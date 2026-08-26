# 需求文件：接收 Webhook 訊息（Receive messages）

> SDD 風格範本。驗收標準採 EARS 語法：
> `WHEN <觸發條件> THEN <系統> SHALL <行為>` / `IF <狀態> THEN <系統> SHALL <行為>`

## 1. Overview

使用者對 LINE 官方帳號加好友或傳送訊息時，LINE Platform 會以 HTTP POST 將 webhook event object 送到已註冊的 Webhook URL。系統需正確接收並處理這些事件，且需即時回應，避免因長時間無法正確回應而被 LINE Platform 暫停投遞。

## 2. Requirements

### Requirement 1：接收 Webhook 事件

**User Story:** 身為系統，我需要接收 LINE Platform 送來的 webhook event object，才能驅動後續的邏輯。

**Acceptance Criteria**
1. WHEN LINE Platform 對 Webhook URL 發出 POST 請求 THEN 系統 SHALL 使用 WebhookController 的 [POST] 方法做接收
2. WHEN LINE Platform 對 Webhook URL 發出 POST 請求 THEN 系統 SHALL 接收並解析 request body 中的 event object
3. IF request body 無法解析為合法的 event object 結構 THEN 系統 SHALL 回傳 400，不得拋出未處理例外

### Requirement 2：即時回應避免被停權

**User Story:** 身為維運者，我希望系統能穩定且快速回應 webhook 請求，避免 LINE Platform 判定服務異常而暫停投遞。

**Acceptance Criteria**
1. WHEN 系統成功接收 webhook 請求（不論後續業務邏輯是否完成）THEN 系統 SHALL 於 LINE 逾時限制內回傳 200
2. 系統 SHALL 確保回應時間不受後續業務邏輯（讀檔、計算、呼叫 Messaging API）執行時間影響

### Requirement 3：非同步處理事件

**User Story:** 身為系統，我需要非同步處理事件內容，避免同一時間的多個請求互相阻塞等待。

**Acceptance Criteria**
1. WHEN 系統回傳 200 給 LINE Platform THEN 系統 SHALL 以非同步方式繼續執行對應的業務邏輯（不得同步阻塞 HTTP 回應）
2. 系統 SHALL 確保單一事件處理發生例外時，不影響其他並行事件的處理

### Requirement 4：簽章驗證

**User Story:** 身為系統，我需要先驗證請求來源合法，才能處理事件內容。

**Acceptance Criteria**
1. WHEN 系統接收到 POST 請求 THEN 系統 SHALL 先完成簽章驗證，驗證失敗者不得進入 Requirement 1～3 的流程
2. 詳細驗證規則見另一份需求文件《requirements-startup-healthcheck.md》

## 3. Non-Functional Requirements

- **可靠度**：長時間無法正確回應會導致 LINE Platform 暫停投遞，需監控回應成功率與延遲
- **容錯**：單一事件處理失敗不得影響其他事件或導致服務中斷

## 4. Constraints

- 平台：ASP.NET Core（.NET 8），部署於 Render
- 依賴既有簽章驗證機制（見《requirements-startup-healthcheck.md》需求文件）

## 5. Out of Scope

- 簽章驗證的實作細節（《requirements-startup-healthcheck.md》）
- 各類事件（訊息、加好友、Postback）的業務邏輯處理內容

## 6. Verification

- 整合測試：送出合法 event，確認 200 回應時間不受業務邏輯耗時影響
- 整合測試：送出格式錯誤的 body，確認回傳 400 且無未處理例外

## 7. AI 交談紀錄
**以下為與AI討論區塊**

### 初始澄清 (2025-01-09)

**AI 提出的澄清問題**

在開始設計與實作前，需確認以下細節：

#### 問題 1：背景工作方案
事件非同步處理方式？
- A) 簡單的內部 `BackgroundJobQueue`（不持久化，服務重啟遺失）
- B) 使用 Hangfire（持久化、可監控）
- C) 使用 `Channel<T>` + HostedService（輕量級，無持久化）

**使用者回覆:** 不須特別做背景工作，僅使用 ASP.NET Core 內建的非同步 Task 即可。

#### 問題 2：事件結構
預期的 webhook event 結構是否已定義？
- A) 使用 LINE 官方 SDK（如 Line.Messaging）
- B) 手動定義 DTO
- C) 查看既有程式碼中是否已有實作

**使用者回覆:** A

#### 問題 3：錯誤處理與重試
業務邏輯失敗時的重試策略？
- A) 不重試（失敗則記錄，不再處理）
- B) 簡單重試次數（預設 3 次）
- C) 指數退避重試（遞增延遲間隔）

**使用者回覆:** A，僅列印錯誤訊息在 Console 中

#### 問題 4：實作範圍
您想一口氣實作所有四個 Requirement？
- A) 全部實作（Req 1～4 完整）
- B) 分批實作（先做 Req 1～3，Req 4 另外處理）

**使用者回覆:** B

### 現狀分析 (2025-01-09)

**已有基礎設施**

專案已具備：
- DDD 分層架構（Domain / Application / Infrastructure / Api）
- LINE 簽章驗證中介軟體（LineSignatureMiddleware）
- 加密配置（Aes256CryptoLib）
- .NET 10 環境

**缺失項目**

需新增：
- LINE 官方 SDK（Line.Messaging）NuGet 套件
- Webhook 接收端點（WebhookController 或 Minimal API）
- Webhook Event DTO 與解析邏輯（Application 層）
- Webhook 事件處理 Service（Application 層）
- 非同步事件發布機制（簡單的 Task-based 設計）

---

### 實作規劃與執行 (2025-01-09)

**已完成的步驟**

1. ✅ **新增 Line.Messaging NuGet 套件（v1.4.5）**
   - 添加至 Api 與 Application 專案

2. ✅ **在 Domain 層建立 Webhook 領域物件**
   - `IWebhookEvent` 介面：定義事件的核心屬性（Timestamp、EventType、ReplyToken）
   - `WebhookEventCollection` 類別：代表一組事件集合

3. ✅ **在 Application 層建立事件處理服務**
   - `IWebhookEventHandler` 介面
   - `WebhookEventService` 實作：
     - 支援多種事件類型處理（message、follow、unfollow、postback）
     - 單一事件失敗不影響其他事件（容錯設計）
     - 所有錯誤記錄在 Console

4. ✅ **在 Api 層建立 WebhookController**
   - `WebhookController` [POST] /webhook 端點
   - 驗證請求格式（format error 回傳 400）
   - 立即回傳 202 Accepted，後續非同步處理
   - 使用 `Task.Run()` 進行非同步事件分發

5. ✅ **WebhookEventConverter 轉換器**
   - 無依賴 SDK 型別，使用協商 `JsonElement` 進行 JSON 轉換
   - 適配 LINE Webhook 事件為領域物件
   - 支援序列化反序列化

6. ✅ **在 Program.cs 中註冊 DI**
   - 註冊 `IWebhookEventHandler` → `WebhookEventService`
   - 註冊控制器與 JSON 序列化選項
   - 正確順序：簽章驗證中介軟體 → 控制器路由

7. ✅ **編譯驗證**
   - dotnet build 成功，無編譯錯誤

---

### 技術設計決策

**1. 事件轉換架構**
- 使用 `JsonElement` 而非 Line.Messaging SDK 型別
- 理由：降低相依、提高靈活性、支援 .NET 10 System.Text.Json
- 優勢：易於擴展、無版本限制

**2. 非同步設計**
- 使用簡單 `Task.Run()` 搭配 `CancellationToken`
- 立即回傳 202，業務邏輯後台執行
- 不使用 BackgroundService 或 Channel，保持輕量

**3. 錯誤處理**
- 單層捕捉：Application 層內部例外處理
- Controller 層防禦性 Try-Catch
- 所有錯誤記錄到 Console（符合要求）
- 失敗事件不重試，直接繼續下一個

---



