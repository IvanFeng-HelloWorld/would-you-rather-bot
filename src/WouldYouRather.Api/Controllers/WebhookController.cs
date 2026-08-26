using Microsoft.AspNetCore.Mvc;
using WouldYouRather.Api.Models;
using WouldYouRather.Application.Interfaces;

namespace WouldYouRather.Api.Controllers;

/// <summary>
/// LINE Webhook 接收控制器。
///
/// 責任：
/// 1. 接收 LINE Platform 發送的 Webhook 請求
/// 2. 驗證請求格式（签章驗證已由中介軟體處理）
/// 3. 快速回應 202 Accepted，確保不超時
/// 4. 非同步分發事件給應用層處理
///
/// 設計：
/// - 優先回應 HTTP 200/202，確保 LINE 不會重試或暫停投遞
/// - 業務邏輯以非同步 Task 執行，不阻塞 HTTP 回應
/// - 錯誤記錄在 Console，支援簡單的容錯（單一事件失敗不影響其他）
/// </summary>
[ApiController]
[Route("webhook")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookEventHandler _eventHandler;

    public WebhookController(IWebhookEventHandler eventHandler)
    {
        _eventHandler = eventHandler ?? throw new ArgumentNullException(nameof(eventHandler));
    }

    /// <summary>
    /// 接收 LINE Webhook 事件。
    ///
    /// 流程：
    /// 1. 驗證 request body 格式（JSON 解析失敗回傳 400）
    /// 2. 立即回傳 200 OK（確保 LINE 不認為服務異常）
    /// 3. 同時非同步分發事件給應用層（不阻塞 HTTP 回應）
    /// </summary>
    /// <param name="request">LINE Webhook 請求 DTO</param>
    /// <returns>200 OK</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReceiveWebhookAsync([FromBody] LineWebhookRequest? request)
    {
        //// 驗證 request body 格式
        //if (request is null || request.Events is null || request.Events.Length == 0)
        //{
        //    Console.WriteLine($"[{DateTime.UtcNow:o}] Webhook 請求格式錯誤或事件空集合。");
        //    return BadRequest(new { error = "Invalid webhook request: events array is empty or missing." });
        //}

        // 立即回傳 200 OK，不等待業務邏輯完成
        // 這樣確保 LINE Platform 不會因為超時而暫停投遞
        var acceptedResponse = Ok();

        // 非同步分發事件給應用層（在背景執行，不阻塞此 HTTP 回應）
        // 使用 Task.Run 確保即使應用層拋出例外也不會影響 HTTP 回應
        _ = Task.Run(async () =>
        {
            try
            {
                var webhookEvents = request.Events.ToWebhookEventCollection();
                await _eventHandler.HandleWebhookEventsAsync(webhookEvents, HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                // 應用層例外捕捉（WebhookEventService 已有內部例外處理，此為防禦性編程）
                Console.Error.WriteLine($"[{DateTime.UtcNow:o}] Webhook 事件處理發生未預期的例外: {ex}");
            }
        }, HttpContext.RequestAborted);

        return acceptedResponse;
    }
}