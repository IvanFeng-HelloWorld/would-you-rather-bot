using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using WouldYouRather.Api.Models;

namespace WouldYouRather.Api.Filters;

/// <summary>
/// 可套用於 Controller 或 Action 的簽章驗證 Attribute。
/// 若要全站驗證，建議使用 Middleware。此 Attribute 以 IOptions&lt;LineBotSetting&gt; 讀取 ChannelSecret。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class LineSignatureFilterAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _headerName = "X-Line-Signature";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;

        if (!http.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
            !http.Request.Path.StartsWithSegments("/webhook", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        var options = http.RequestServices.GetService(typeof(IOptions<LineBotSetting>)) as IOptions<LineBotSetting>;
        var secret = options?.Value?.ChannelSecret ?? string.Empty;

        if (!http.Request.Headers.TryGetValue(_headerName, out var signatureValues))
        {
            Console.WriteLine($"[{DateTime.UtcNow:o}] Webhook signature missing.");
            context.Result = new UnauthorizedResult();
            return;
        }

        var signature = signatureValues.FirstOrDefault() ?? string.Empty;

        http.Request.EnableBuffering();
        using var ms = new MemoryStream();
        await http.Request.Body.CopyToAsync(ms);
        var body = ms.ToArray();
        http.Request.Body.Position = 0;

        var computed = ComputeHmacSha256(secret, body);

        try
        {
            var incoming = Convert.FromBase64String(signature);
            if (!CryptographicOperations.FixedTimeEquals(computed, incoming))
            {
                Console.WriteLine($"[{DateTime.UtcNow:o}] Webhook signature mismatch.");
                context.Result = new UnauthorizedResult();
                return;
            }
        }
        catch (FormatException)
        {
            Console.WriteLine($"[{DateTime.UtcNow:o}] Webhook signature malformed.");
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }

    private static byte[] ComputeHmacSha256(string key, byte[] body)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        using var hmac = new HMACSHA256(keyBytes);
        return hmac.ComputeHash(body);
    }
}
