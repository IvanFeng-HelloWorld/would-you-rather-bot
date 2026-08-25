# would-you-rather-bot

簡短說明
- 目的：建立一個 Line Bot 使用的 Message API 站台，實現 WouldYouRather 功能
- 目標平台：.NET 10

專案結構（重要專案）
- src/WouldYouRather.Domain — 領域層（Entity、ValueObject、Domain 介面）
- src/WouldYouRather.Application — 應用層（DTO、UseCase 介面）
- src/WouldYouRather.Infrastructure — 基礎設施層（Repository 實作，範例採 In-Memory）
- src/WouldYouRather.Api — 展示層（Minimal Web API）

快速開始
1. 安裝需求：.NET 10 SDK
2. 從專案根目錄建置解決方案：

   dotnet build would-you-rather-bot.slnx

3. 啟動 API（預設會在 localhost:5000 / 以及 HTTPS 另一個端口）：

   dotnet run --project src/WouldYouRather.Api

API 範例
- GET /questions — 取得內建的問答清單（回傳 JSON 陣列）

開發說明
- 此骨架遵循 DDD 分層原則：Domain 不依賴其他層；Application 定義介面；Infrastructure 提供實作；Api 注入 Application/Infrastructure 提供的服務。
- 目前 Infrastructure 提供一個簡單的 InMemory repository 作為範例，後續可替換為 EF Core 或其他持久層實作。
