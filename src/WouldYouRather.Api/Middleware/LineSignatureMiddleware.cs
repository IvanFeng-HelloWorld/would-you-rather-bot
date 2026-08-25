using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using WouldYouRather.Api.Models;

namespace WouldYouRather.Api.Middleware;

public class LineSignatureMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptions<LineBotSetting> _options;
    private const string HeaderName = "X-Line-Signature";

    public LineSignatureMiddleware(RequestDelegate next, IOptions<LineBotSetting> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate POST /webhook to avoid interfering other endpoints (eg. /health)
        if (!HttpMethods.IsPost(context.Request.Method) ||
            !context.Request.Path.StartsWithSegments("/webhook", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var signatureValues))
        {
            Console.WriteLine($"[{DateTime.UtcNow:o}] Webhook signature missing.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var signature = signatureValues.FirstOrDefault() ?? string.Empty;
        var secret = _options.Value?.ChannelSecret ?? string.Empty;

        context.Request.EnableBuffering();
        using var ms = new MemoryStream();
        await context.Request.Body.CopyToAsync(ms);
        var body = ms.ToArray();
        context.Request.Body.Position = 0;

        var computed = ComputeHmacSha256(secret, body);

        try
        {
            var incoming = Convert.FromBase64String(signature);
            if (!CryptographicOperations.FixedTimeEquals(computed, incoming))
            {
                Console.WriteLine($"[{DateTime.UtcNow:o}] Webhook signature mismatch.");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }
        catch (FormatException)
        {
            Console.WriteLine($"[{DateTime.UtcNow:o}] Webhook signature malformed.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await _next(context);
    }

    private static byte[] ComputeHmacSha256(string key, byte[] body)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        using var hmac = new HMACSHA256(keyBytes);
        return hmac.ComputeHash(body);
    }
}
