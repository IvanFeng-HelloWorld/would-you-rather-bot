# 需求文件：站台啟動測試（Health Check）

> SDD 風格範本。驗收標準採 EARS 語法：
> `WHEN <觸發條件> THEN <系統> SHALL <行為>` / `IF <狀態> THEN <系統> SHALL <行為>`

## 1. Overview

服務部署到 Render 後，需要一個不依賴任何外部服務（LINE、檔案、DB）的端點，用來確認站台本身有正常啟動並能回應請求，作為 Render Health Check 與人工驗證的依據。

## 2. Requirements

### Requirement 1：健康檢查端點

**User Story:** 身為維運者，我希望能用一個固定端點確認站台是否存活，才能判斷部署是否成功。

**Acceptance Criteria**
1. WHEN 對 `GET /health` 發出請求 THEN 系統 SHALL 回傳 HTTP 200
2. WHEN `/health` 回應成功 THEN 系統 SHALL 回傳內容包含狀態欄位與 UTC 時間戳，格式為 JSON
3. 系統 SHALL 確保 `/health` 不依賴題庫 JSON、投票紀錄或任何外部 API，僅反映站台自身是否存活

### Requirement 2：埠號綁定

**User Story:** 身為維運者，我希望站台能正確依部署環境指定的埠號啟動，才能在 Render 上被正確路由。

**Acceptance Criteria**
1. IF 環境變數 `PORT` 存在 THEN 系統 SHALL 監聽該埠號
2. IF 環境變數 `PORT` 不存在 THEN 系統 SHALL 使用預設埠號（本機開發情境）

### Requirement 3：容器化建置

**User Story:** 身為維運者，我希望站台能被建置成 Docker image，才能部署到 Render。

**Acceptance Criteria**
1. 系統 SHALL 提供 Dockerfile，能在無需額外手動安裝相依套件的情況下完成 build
2. WHEN 執行 `docker build` THEN 系統 SHALL 產出可直接 `docker run` 啟動的 image
3. 系統 SHALL 採多階段建置（build stage 與 runtime stage 分離），避免 runtime image 包含 SDK 與原始碼

## 3. Non-Functional Requirements

- **啟動速度**：容器啟動到 `/health` 可回應應在合理時間內完成（建議 < 10 秒），避免 Render Health Check 誤判失敗
- **無外部依賴**：`/health` 邏輯不得有可能因外部服務逾時而卡住的呼叫

## 4. Constraints

- 平台：ASP.NET Core（.NET 10）
- 部署環境：Render，Environment 設定為 Docker

## 5. Verification

- 本機：`dotnet run` 後打 `/health` 確認 200
- 容器：`docker build` + `docker run` 後打 `/health` 確認 200
- Render：部署後於 Render Dashboard 確認 Health Check 狀態為 Healthy

## 6.AI 交談紀錄
**以下為與AI討論區塊**

```
2026-08-23T00:00:00Z - 動作紀錄:
- 已讀取: agent.md
- 已讀取: instruction.md
- 已讀取: requirements-startup-healthcheck.md

2026-08-23T00:00:00Z - 確認事項:
- 依使用者要求，所有後續交互內容將記錄於本區塊下。
- 下一步: 等待使用者指示是否開始實作 health endpoint、Dockerfile 或其他驗證步驟。

使用者回覆: 開始實作 health endpoint、Dockerfile 與驗證步驟，需確認是一次做完還是分批完成。

2026-08-23T00:01:00Z - 使用者指示:
- 所有交互內容保持記錄於本檔的 6.AI 交談紀錄 區塊下。

使用者回覆: 一次做完

2026-08-23T00:02:00Z - AI 操作紀錄:
- 建立專案: src/WouldYouRather.Api (WouldYouRather.Api.csproj)
- 新增 Program.cs，實作 GET /health，回傳 JSON { status: "OK", utc: "<ISO8601 UTC>" }
- 新增 Dockerfile（multi-stage）於 src/WouldYouRather.Api/Dockerfile
- 新增 .dockerignore

2026-08-23T00:02:30Z - 本機建置驗證:
- 執行: dotnet build src/WouldYouRather.Api -c Release
- 結果: Build 成功，輸出: src\WouldYouRather.Api\bin\Release\net10.0\WouldYouRather.Api.dll

2026-08-23T00:02:40Z - 下一步建議與驗證指令 (請在使用者機器上執行):
1) 本機啟動並驗證 /health:
   - dotnet run --project src/WouldYouRather.Api
   - 在瀏覽器或 curl 中訪問: http://localhost:5000/health (若未設定 PORT, 預設 Kestrel 將使用 5000/5001)
   - 預期回應: HTTP 200, JSON { "status": "OK", "utc": "2026-08-23T...Z" }

2) 使用 PORT 環境變數啟動 (Render 相容):
   - $env:PORT=8080; dotnet run --project src/WouldYouRather.Api
   - 或 Linux/macOS: PORT=8080 dotnet run --project src/WouldYouRather.Api
   - 訪問: http://localhost:8080/health

3) 建置 Docker image 並執行:
   - docker build -t wouldyourrather/api:healthcheck -f src/WouldYouRather.Api/Dockerfile .
   - docker run -e PORT=8080 -p 8080:8080 wouldyourrather/api:healthcheck
   - 訪問: http://localhost:8080/health

4) 預期容器回應: HTTP 200 與 JSON (status, utc), 並在 10 秒內可回應。

```
