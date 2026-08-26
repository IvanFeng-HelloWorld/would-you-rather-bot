namespace WouldYouRather.Domain.Events;

/// <summary>
/// LINE Webhook 事件集合的領域表示。
/// 一次 Webhook 請求可能包含多個事件，此類代表整個事件集合。
/// </summary>
public class WebhookEventCollection
{
    /// <summary>
    /// 事件清單。
    /// </summary>
    public IReadOnlyList<IWebhookEvent> Events { get; }

    public WebhookEventCollection(IReadOnlyList<IWebhookEvent> events)
    {
        Events = events ?? throw new ArgumentNullException(nameof(events));
    }

    /// <summary>
    /// 取得事件數量。
    /// </summary>
    public int Count => Events.Count;
}
