using WouldYouRather.Application.Interfaces;
using WouldYouRather.Domain.Events;

namespace WouldYouRather.Application.Services;

/// <summary>
/// Webhook 事件解析與分發服務。
/// 將 LINE SDK 的 Event 物件轉換為領域物件，並驅動業務邏輯。
/// 
/// 當前實作：簡單轉換並列印事件，後續可擴展為複雜業務邏輯。
/// </summary>
public class WebhookEventService : IWebhookEventHandler
{
    /// <summary>
    /// 處理 Webhook 事件集合。
    /// 此方法安全地捕捉所有例外，確保單一事件失敗不影響其他事件。
    /// </summary>
    public async Task HandleWebhookEventsAsync(WebhookEventCollection events, CancellationToken cancellationToken = default)
    {
        if (events.Count == 0)
        {
            Console.WriteLine($"[{DateTime.UtcNow:o}] Webhook 事件集合為空，無需處理。");
            return;
        }

        Console.WriteLine($"[{DateTime.UtcNow:o}] 開始處理 {events.Count} 個 Webhook 事件。");

        // 逐一處理每個事件
        // 若某個事件失敗，記錄並繼續處理下一個（不中斷）
        foreach (var webhookEvent in events.Events)
        {
            try
            {
                await ProcessSingleEventAsync(webhookEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[{DateTime.UtcNow:o}] 處理事件失敗 (EventType={webhookEvent.EventType}): {ex.Message}");
                // 不重試，直接繼續下一個事件
            }
        }

        Console.WriteLine($"[{DateTime.UtcNow:o}] 完成處理 {events.Count} 個 Webhook 事件。");
    }

    /// <summary>
    /// 處理單一事件。
    /// </summary>
    private async Task ProcessSingleEventAsync(IWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[{DateTime.UtcNow:o}] 處理事件: EventType={webhookEvent.EventType}, Timestamp={webhookEvent.Timestamp}");

        // 根據事件類型分配業務邏輯
        // 當前暫時全部統一記錄，後續按需分類處理
        switch (webhookEvent.EventType.ToLowerInvariant())
        {
            case "message":
                await HandleMessageEventAsync(webhookEvent, cancellationToken);
                break;

            case "follow":
                await HandleFollowEventAsync(webhookEvent, cancellationToken);
                break;

            case "unfollow":
                await HandleUnfollowEventAsync(webhookEvent, cancellationToken);
                break;

            case "postback":
                await HandlePostbackEventAsync(webhookEvent, cancellationToken);
                break;

            default:
                Console.WriteLine($"[{DateTime.UtcNow:o}] 未知事件類型: {webhookEvent.EventType}");
                break;
        }

        // 使用 cancellationToken，確保支援優雅關閉
        await Task.CompletedTask;
    }

    private async Task HandleMessageEventAsync(IWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[{DateTime.UtcNow:o}] 處理訊息事件 (ReplyToken={webhookEvent.ReplyToken})");
        // TODO: 實作訊息事件業務邏輯（例：解析訊息內容、回覆、存儲等）
        await Task.CompletedTask;
    }

    private async Task HandleFollowEventAsync(IWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[{DateTime.UtcNow:o}] 處理加好友事件 (ReplyToken={webhookEvent.ReplyToken})");
        // TODO: 實作加好友事件業務邏輯（例：記錄新用戶）
        await Task.CompletedTask;
    }

    private async Task HandleUnfollowEventAsync(IWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[{DateTime.UtcNow:o}] 處理取消好友事件 (ReplyToken={webhookEvent.ReplyToken})");
        // TODO: 實作取消好友事件業務邏輯（例：清理用戶數據）
        await Task.CompletedTask;
    }

    private async Task HandlePostbackEventAsync(IWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[{DateTime.UtcNow:o}] 處理 PostBack 事件 (ReplyToken={webhookEvent.ReplyToken})");
        // TODO: 實作 PostBack 事件業務邏輯（例：處理按鈕點擊等）
        await Task.CompletedTask;
    }
}
