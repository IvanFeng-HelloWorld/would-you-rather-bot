namespace WouldYouRather.Domain.Events;

/// <summary>
/// LINE Webhook 事件的抽象介面。
/// 所有 Webhook 事件都應實作此介面，以便統一處理。
/// </summary>
public interface IWebhookEvent
{
    /// <summary>
    /// 事件時戳（UTC）。
    /// </summary>
    long Timestamp { get; }

    /// <summary>
    /// 事件類型（例："message"、"follow"、"postback" 等）。
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// 使用者或群組 ID（來源端點識別符）。
    /// </summary>
    string ReplyToken { get; }
}
