using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WouldYouRather.Api.Middleware;

namespace WouldYouRather.Api.Extensions;

public static class LineSignatureExtensions
{
    /// <summary>
    /// 在管線中加入 Line Webhook 簽章驗證 Middleware（只驗證 POST /webhook）。
    /// 也可改為套用 LineSignatureFilterAttribute 至單一 Controller/Action。
    /// </summary>
    public static IApplicationBuilder UseLineSignatureVerification(this IApplicationBuilder app)
    {
        return app.UseMiddleware<LineSignatureMiddleware>();
    }
}
