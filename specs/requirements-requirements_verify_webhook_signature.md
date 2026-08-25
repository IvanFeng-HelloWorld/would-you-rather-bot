# 需求文件：Webhook 安全驗證

> 本文件為 SDD（Spec-Driven Development）風格範本。驗收標準採 EARS 語法：
> `WHEN <觸發條件> THEN <系統> SHALL <行為>` / `IF <狀態> THEN <系統> SHALL <行為>`

## 1. Overview

LINE Platform 會將所有事件以 POST 方式送到 Webhook URL。系統必須先確認請求確實來自 LINE，才能進入後續業務邏輯，避免偽造請求觸發遊戲邏輯或消耗 Messaging API 額度。
- 請先讀取網站內容 https://developers.line.biz/en/docs/messaging-api/verify-webhook-signature/

## 2. Requirements

### Requirement 1：簽章驗證

**User Story:** 身為系統維運者，我希望所有進入 Webhook 的請求都經過簽章驗證，才能確保事件來源可信。

**Acceptance Criteria**
1. WHEN 收到 POST /webhook 請求 THEN 系統 SHALL 使用 Channel Secret 對 Request Body 計算 HMAC-SHA256(存放於IOptions<LineBotSetting> 中)，並與 `X-Line-Signature` Header 做 Base64 比對
2. IF 簽章比對失敗 THEN 系統 SHALL 回傳 401，不執行任何後續邏輯
3. IF `X-Line-Signature` Header 不存在 THEN 系統 SHALL 視同驗證失敗，回傳 401
4. WHEN 簽章驗證通過 THEN 系統 SHALL 於時限內回傳 200，避免 LINE Platform 判定逾時重送

### Requirement 2：簽章驗證使用

**User Story:** 身為系統維運者，我希望簽章驗證可以容易被重複使用。

**Acceptance Criteria**
1. WHEN 新增 controller THEN 系統 SHALL 很容易的使用簽章驗證，使用 Filter 或其他全域設定

### Requirement 3：失敗處理與記錄

**Acceptance Criteria**
1. WHEN 簽章驗證失敗 THEN 系統 SHALL console 記錄請求時間、失敗原因，不得記錄 Channel Secret 明文
2. WHEN 業務邏輯處理發生例外 THEN 系統 SHALL 仍回應合理的 HTTP 狀態碼，不得讓例外導致連線中斷無回應

## 3. Non-Functional Requirements

- **可觀測性**：驗證失敗需可被console 記錄

## 4. Constraints

- 平台：ASP.NET Core Web API
- 部署環境：Render（金鑰以環境變數注入）

## 5. Verification

- 單元測試：正確簽章 / 錯誤簽章 / 缺 Header 三種情境

## 6.AI 交談紀錄
**以下為與AI討論區塊**

- 2026-08-25 (AI): 已完成實作與測試，變更清單如下：
  1. 新增可重用的驗證元件於 Api 專案：
     - src/WouldYouRather.Api/Filters/LineSignatureFilterAttribute.cs  (可套用於 Controller 或 Action 的 Attribute)
     - src/WouldYouRather.Api/Middleware/LineSignatureMiddleware.cs  (建議用於全域或 webhook 專用的 Middleware，僅驗證 POST /webhook)
     - src/WouldYouRather.Api/Extensions/LineSignatureExtensions.cs  (註冊中介軟體的擴充方法：UseLineSignatureVerification)
     - 已在 src/WouldYouRather.Api/Program.cs 註冊 UseLineSignatureVerification()，中介軟體僅對 POST /webhook 生效，避免影響 /health 等其他端點。

  2. 新增單元測試專案與測試案例：
     - tests/WouldYouRather.Api.Tests/WouldYouRather.Api.Tests.csproj
     - tests/WouldYouRather.Api.Tests/LineSignatureMiddlewareTests.cs
       - 測試情境：正確簽章 (允許)、錯誤簽章 (401)、缺 Header (401)

  3. 測試結果：
     - dotnet test 執行結果：3/3 測試通過。執行期間於 Console 出現兩筆驗證失敗的日誌（預期行為，用於可觀測性）。

  驗證行為摘要：
  - 從 IOptions<LineBotSetting> 讀取 ChannelSecret（Program.cs 已以解密後值綁定至 IOptions）。
  - 僅針對 POST /webhook 執行 HMAC-SHA256 驗證（使用 UTF8(secret)），並比對 X-Line-Signature（Base64）。
  - 驗證失敗（缺 Header / 簽章錯誤 / 格式錯誤）回應 401，並以 Console.WriteLine 列印 UTC 時間與失敗原因（絕不列印 ChannelSecret）。
  - 驗證成功會將 Request.Body rewind，確保後續處理可讀取 Body。

  注意事項 / 建議：
  - 若僅需在單一 Controller/Action 使用，請使用 [LineSignatureFilter]；若欲全站或集中管理，使用中介軟體（目前已註冊）。
  - 建議將 ChannelAccessToken / ChannelSecret 以環境變數注入（目前 Program.cs 已示範使用環境變數並解密後綁定）。

- README 更新建議（需您確認是否同步更新）：
  建議更新 README.md（是）：新增「Webhook 簽章驗證」章節，包含以下要點：
  1. 功能簡述：說明為何要驗證 LINE Webhook 簽章（防止偽造請求）。
  2. 使用方式：如何在程式中啟用中介軟體（Program.cs 範例）或在 Controller 加上 [LineSignatureFilter]。
  3. 設定項目：需設定的環境變數 ChannelAccessToken、ChannelSecret（注意：Secret 請以密文注入並解密，示範目前採用的方法）。
  4. 測試：說明已加入 NUnit 測試的三種情境及如何執行 dotnet test。
  5. 可觀測性：驗證失敗會在 Console 記錄 UTC 時間與失敗原因，並強調不會記錄明文 Secret。

  請回覆是否要我同步在 README.md 新增上述章節（回覆：要 / 不要 / 先草稿供審閱）。

  使用者回覆: 先草稿供審閱

- 2026-08-25 (AI): 已建立 README 草稿檔案 README_UPDATE.md，包含使用說明、設定、測試與可觀測性說明。請審閱草稿並指示是否合併或修改.