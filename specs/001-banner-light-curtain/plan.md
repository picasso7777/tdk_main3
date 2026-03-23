# 實作計畫：Banner 光幕支援

**分支**：001-banner-light-curtain | **日期**：2026-03-23 | **規格**：[specs/001-banner-light-curtain/spec.md](specs/001-banner-light-curtain/spec.md)  
**輸入**：來自 [specs/001-banner-light-curtain/spec.md](specs/001-banner-light-curtain/spec.md) 的功能規格

## 摘要

於 TDKController 專案內實作 Banner Light Curtain 模組，提供兩組安全輸入通道 OSSD1 與 OSSD2 的讀取、四組可控數位輸出 Reset、Test、Interlock、LTCLed 的設定與讀取、完整狀態快照查詢、三種運作模式、兩種電壓模式，以及安全告警與狀態變更事件通知。模組透過 IOBoard[] 陣列與 ILogUtility 進行建構函式注入，並以 DioChannelConfig 結構描述 DIO 通道映射。

## 技術背景

**語言與版本**：C# 7.3、.NET Framework 4.7.2  
**主要相依性**：DIO 專案的 IOBoard 與相關 DIO API、TDKLogUtility 的 ILogUtility  
**資料儲存**：不涉及持久化，僅維護記憶體內狀態  
**測試框架**：NUnit 3.x + Moq，測試專案位於 AutoTest 底下  
**目標平台**：Windows MiniPC 工業設備控制器  
**專案型態**：TDKController 內的 DLL 模組  
**效能目標**：DIO 讀寫延遲以硬體可達範圍內的低延遲回應為目標  
**限制**：模組不得包含內部輪詢執行緒；OSSD 讀取由外部呼叫者主動觸發；模組實作維持單一 .cs 檔案  
**範圍**：單一 LightCurtain 實例，涵蓋 2 個 DI 與 4 個 DO 的映射與控制

## 憲章檢查

進入實作前必須先通過本節檢查；完成 tasks 更新後需再次確認。

| # | 憲章條款 | 狀態 | 說明 |
|---|---------|------|------|
| 1 | 命名慣例 | ✅ PASS | 類別、方法與屬性使用 PascalCase，區域變數使用 camelCase |
| 2 | 組態屬性命名為 Config | ✅ PASS | 光幕組態公開屬性名稱統一為 Config |
| 3 | 單一職責 | ✅ PASS | LightCurtain 模組僅負責光幕安全 I/O 與狀態管理 |
| 4 | 建構函式注入與 null 檢查 | ✅ PASS | IOBoard[]、ILogUtility 與組態由建構函式注入並進行驗證 |
| 5 | 事件相依性訂閱規則 | ✅ N/A | 本功能不涉及 IConnector 注入與替換 |
| 6 | IDisposable 模組生命週期 | ⚠️ N/A | 目前規格未要求實作 IDisposable |
| 7 | 例外處理與日誌 | ✅ PASS | 公開操作須以 try-catch 保護並透過 ILogUtility 記錄 |
| 8 | 錯誤碼範圍 -400..-499 | ✅ PASS | LightCurtain 使用專案預留的 -400..-499 範圍 |
| 9 | 單一 .cs 檔案 | ✅ PASS | 模組實作集中於 [TDKController/Module/LightCurtain.cs](TDKController/Module/LightCurtain.cs) |
| 10 | 禁止未授權類別 | ✅ PASS | 完整狀態快照重用既有狀態資料形狀，不新增額外快照類別 |
| 11 | 介面使用政策 | ⚠️ CONDITIONAL | [specs/001-banner-light-curtain/spec.md](specs/001-banner-light-curtain/spec.md) 已記錄本 feature 對 [TDKController/Interface/ILightCurtain.cs](TDKController/Interface/ILightCurtain.cs) 的核准例外，仍需 [specs/001-banner-light-curtain/tasks.md](specs/001-banner-light-curtain/tasks.md) 同步記錄 |
| 12 | Lambda 使用規則 | ✅ PASS | 設計上不依賴 lambda |
| 13 | #region 分區 | ✅ PASS | 最終模組將依職責建立語意化分區 |
| 14 | 核心邏輯覆蓋率至少 90% | ⚠️ CONDITIONAL | 必須於 [specs/001-banner-light-curtain/tasks.md](specs/001-banner-light-curtain/tasks.md) 與最終驗證中明確列為通過條件 |
| 15 | 檔案建立政策 | ⚠️ CONDITIONAL | [specs/001-banner-light-curtain/spec.md](specs/001-banner-light-curtain/spec.md) 已記錄單一測試檔核准，仍需 [specs/001-banner-light-curtain/tasks.md](specs/001-banner-light-curtain/tasks.md) 同步記錄 |
| 16 | 分層規則 | ✅ PASS | 模組保持自含，不依賴 LoadportActor 或 loadport workflow 狀態 |
| 17 | YAGNI | ✅ PASS | 僅實作規格明確要求的能力 |
| 18 | 文件語言要求 | ✅ PASS | 本計畫文件以繁體中文撰寫 |

**Gate 結果**：⚠️ 有條件通過。  
本文件已與 [specs/001-banner-light-curtain/spec.md](specs/001-banner-light-curtain/spec.md) 對齊，但仍需 [specs/001-banner-light-curtain/tasks.md](specs/001-banner-light-curtain/tasks.md) 同步補齊核准例外紀錄與 90% 覆蓋率驗證條件，才可視為完全通過。

## 專案結構

### 本功能文件

- [specs/001-banner-light-curtain/spec.md](specs/001-banner-light-curtain/spec.md)：功能規格
- [specs/001-banner-light-curtain/research.md](specs/001-banner-light-curtain/research.md)：研究輸出
- [specs/001-banner-light-curtain/data-model.md](specs/001-banner-light-curtain/data-model.md)：資料模型
- [specs/001-banner-light-curtain/quickstart.md](specs/001-banner-light-curtain/quickstart.md)：驗證步驟
- [specs/001-banner-light-curtain/tasks.md](specs/001-banner-light-curtain/tasks.md)：任務清單

### 主要實作位置

- [TDKController/Interface/ILightCurtain.cs](TDKController/Interface/ILightCurtain.cs)：光幕介面定義，允許本 feature 進行最小必要擴充
- [TDKController/Interface/ErrorCode.cs](TDKController/Interface/ErrorCode.cs)：LightCurtain 錯誤碼常數
- [TDKController/Config/LightCurtainConfig.cs](TDKController/Config/LightCurtainConfig.cs)：光幕組態與通道映射
- [TDKController/Module/LightCurtain.cs](TDKController/Module/LightCurtain.cs)：光幕模組實作

### 結構決策

所有實作程式碼均放置於既有的 TDKController 專案結構中，不新增專案。完整狀態快照由 GetLightCurtainStatus 方法提供，並與狀態變更通知共用相同資料形狀，以避免為本 feature 額外新增獨立快照類別。單元測試維持單一測試檔策略，放置於既有 AutoTest 單元測試目錄中。任何對介面擴充與新測試檔建立的例外記錄，必須與 [specs/001-banner-light-curtain/spec.md](specs/001-banner-light-curtain/spec.md) 及 [specs/001-banner-light-curtain/tasks.md](specs/001-banner-light-curtain/tasks.md) 保持一致。

## 複雜度追蹤

目前無需額外複雜度豁免。  
若後續因測試覆蓋率、檔案建立政策或介面例外同步不足而產生偏離，必須先更新 [specs/001-banner-light-curtain/tasks.md](specs/001-banner-light-curtain/tasks.md) 後再進入實作。
