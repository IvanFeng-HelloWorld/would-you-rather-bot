using System.Text.Json;
using System.Text.Json.Serialization;
using WouldYouRather.Domain.Events;

namespace WouldYouRather.Api.Models;

/// <summary>
/// LINE Webhook 事件轉換器。
/// 將 LINE Platform 的 Webhook JSON 轉換為領域物件 (IWebhookEvent)。
/// </summary>
public static class WebhookEventConverter
{
    /// <summary>
    /// 將 LINE Webhook JSON 物件轉換為領域 IWebhookEvent 物件。
    /// </summary>
    public static IWebhookEvent ToDomainEvent(this JsonElement lineEventElement)
    {
        if (lineEventElement.ValueKind == JsonValueKind.Null)
            throw new ArgumentNullException(nameof(lineEventElement));

        return new LineWebhookEventAdapter(lineEventElement);
    }

    /// <summary>
    /// 將 LINE Webhook JSON 事件陣列轉換為 WebhookEventCollection。
    /// </summary>
    public static WebhookEventCollection ToWebhookEventCollection(this JsonElement[]? events)
    {
        var eventList = events?
            .Select(e => e.ToDomainEvent())
            .ToList() ?? new List<IWebhookEvent>();

        return new WebhookEventCollection(eventList.AsReadOnly());
    }

    /// <summary>
    /// LINE Webhook JSON 適配器，實作 IWebhookEvent 介面。
    /// 用於將 LINE Platform 的 Webhook JSON 轉換為領域事件。
    /// </summary>
    private class LineWebhookEventAdapter : IWebhookEvent
    {
        private readonly JsonElement _lineEvent;

        public LineWebhookEventAdapter(JsonElement lineEvent)
        {
            _lineEvent = lineEvent;
        }

        public long Timestamp
        {
            get
            {
                if (_lineEvent.TryGetProperty("timestamp", out var prop) && prop.ValueKind == JsonValueKind.Number)
                {
                    return prop.GetInt64();
                }
                return 0L;
            }
        }

        public string EventType
        {
            get
            {
                if (_lineEvent.TryGetProperty("type", out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    return prop.GetString() ?? string.Empty;
                }
                return string.Empty;
            }
        }

        public string ReplyToken
        {
            get
            {
                if (_lineEvent.TryGetProperty("replyToken", out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    return prop.GetString() ?? string.Empty;
                }
                return string.Empty;
            }
        }
    }
}

/// <summary>
/// LINE Platform 發送的 Webhook 請求 DTO。
/// 此類用於反序列化 JSON 請求體。
/// </summary>
public class LineWebhookRequest
{
    /// <summary>
    /// 事件陣列。
    /// </summary>
    [JsonPropertyName("events")]
    public JsonElement[]? Events { get; set; }
}

