# 需求文件：LINE Bot 設定讀取與解密（啟動階段）

> SDD 風格範本。驗收標準採 EARS 語法：
> `WHEN <觸發條件> THEN <系統> SHALL <行為>` / `IF <狀態> THEN <系統> SHALL <行為>`

## 1. Overview

系統部署於 Render，`ChannelAccessToken`／`ChannelSecret` 以加密後的密文存放在環境變數中。系統啟動時需讀取這兩個環境變數、以 `Aes256CryptoService` 解密，並將解密後的明文設定進系統設定物件，供各服務透過 `IOptions<LineBotSetting>` 注入使用。

## 2. Requirements

### Requirement 1：讀取加密環境變數

**User Story:** 身為維運者，我希望敏感金鑰以密文存放在環境變數，才能避免明文外洩於部署平台設定畫面或紀錄中。

**Acceptance Criteria**
1. WHEN 系統啟動 THEN 系統 SHALL 讀取環境變數 `ChannelAccessToKen`、`ChannelSecret`
2. IF 任一環境變數不存在或為空字串 THEN 系統 SHALL 啟動失敗並輸出明確錯誤訊息（不得以空值或預設值靜默啟動）

### Requirement 2：解密

**User Story:** 身為維運者，我希望密文能在啟動階段被正確解密成可用的金鑰，才能供後續呼叫 LINE Messaging API。

**Acceptance Criteria**
1. WHEN 取得環境變數密文 THEN 系統 SHALL 使用 `Aes256CryptoService.Decrypt` 解密取得明文
2. IF 解密過程拋出例外（如密文格式錯誤、金鑰不符） THEN 系統 SHALL 啟動失敗並輸出錯誤訊息，且不得將密文或解密例外中可能包含的明文片段輸出至一般 log

### Requirement 3：設定綁定與注入

**User Story:** 身為開發者，我希望解密後的設定能透過 `IOptions<LineBotSetting>` 注入到各服務，才能統一管理設定來源、方便單元測試替換。

**Acceptance Criteria**
1. WHEN 解密完成 THEN 系統 SHALL 將明文寫入 `LineBotSetting`（包含 `ChannelAccessToken`、`ChannelSecret` 屬性）並註冊進 DI 容器
2. 系統 SHALL 讓其他服務僅能透過建構子注入 `IOptions<LineBotSetting>` 取得設定，不得各自重複讀取環境變數或呼叫解密邏輯
3. WHEN 服務透過建構子注入 `IOptions<LineBotSetting>` THEN 系統 SHALL 提供已解密完成的明文，服務端不得再處理解密邏輯

## 3. Non-Functional Requirements

- **安全性**：明文金鑰僅存在於記憶體中的設定物件，不得輸出於一般 log、例外訊息、或任何回應內容
- **啟動即失敗（Fail Fast）**：任何讀取或解密失敗都必須讓服務啟動失敗，不得讓服務帶著無效金鑰啟動並在執行期間才報錯

## 4. Constraints

- 平台：ASP.NET Core（.NET 10），部署於 Render
- 解密邏輯固定使用既有 `Aes256CryptoService`（本文件不涉及該服務內部實作）
- 設定綁定方式固定採 `IOptions<LineBotSetting>` 模式

## 5. Verification
- 本機測試：使用 .appsettings.json 檔模擬環境變數，確認解密後的明文能正確注入服務
- 單元測試：合法密文可正確解密還原為預期明文
- 單元測試：環境變數缺漏 / 密文格式錯誤時，啟動流程會拋出例外並中止
- 整合測試：以測試用密文啟動服務，確認任一注入 `IOptions<LineBotSetting>` 的服務能取得正確明文
- 人工檢查：確認一般 log 輸出中不含明文或密文

## 6.AI 交談紀錄
**以下為與AI討論區塊**

```
2026-08-23T00:00:00Z - 動作紀錄:
- 已讀取: agent.md
- 已讀取: .github\instruction.md
- 已讀取: requirements-linebot-settings-decrypt.md

2026-08-23T00:00:00Z - 確認事項:
- 依使用者要求，後續任何互動內容將記錄於本檔的 6.AI 交談紀錄 區塊下。

下一步: 請確認是否要我一次完成實作（讀取環境變數、Aes256 解密、IOptions 註冊、啟動失敗處理與測試）或分批執行。

使用者回覆: 一次完成實作

2026-08-23T00:05:00Z - AI 操作紀錄:
- 已更新: src/WouldYouRather.Api/Program.cs（在啟動時讀取 ChannelAccessToKen 與 ChannelSecret，使用 Aes256CryptoService 解密，並將明文註冊至 IOptions<LineBotSetting>；缺少或解密錯誤時以非零狀態退出以達到 fail-fast）
- 執行: dotnet build src/WouldYouRather.Api -c Release -> Build 成功