using WouldYouRather.Domain.Events;

namespace WouldYouRather.Application.Interfaces;

/// <summary>
/// Webhook 事件處理器介面。
/// 負責解析 LINE Webhook 事件並驅動業務邏輯。
/// </summary>
public interface IWebhookEventHandler
{
    /// <summary>
    /// 非同步處理 Webhook 事件集合。
    /// 此方法在背景執行，不應阻塞 HTTP 回應。
    /// </summary>
    /// <param name="events">Webhook 事件集合。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task HandleWebhookEventsAsync(WebhookEventCollection events, CancellationToken cancellationToken = default);
}
