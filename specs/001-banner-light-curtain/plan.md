# Implementation Plan: Banner Light Curtain Support

**Branch**: `001-banner-light-curtain` | **Date**: 2026-03-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-banner-light-curtain/spec.md`

## Summary

實作 Banner Light Curtain 模組於 TDKController 專案內，提供兩組安全輸入通道 (OSSD1/OSSD2) 的讀取、四組可控數位輸出 (Reset/Test/Interlock/LTCLed) 的設定與讀取、完整狀態快照查詢、三種運行模式 (Disable/Enable_InTransfer/Enable_Always)、兩種電壓模式 (24V/0V)、以及安全告警與狀態變更事件通知。模組透過 `IOBoard[]` 陣列與 `ILogUtility` 進行建構函式注入，使用 `DioChannelConfig` 結構映射 DIO 通道。

## Technical Context

**Language/Version**: C# 7.3 (.NET Framework 4.7.2)
**Primary Dependencies**: DIO 專案 (`IOBoard`, `IDI`, `IDO` API)、TDKLogUtility (`ILogUtility`)
**Storage**: N/A（記憶體內狀態，無持久化）
**Testing**: NUnit 3.x + Moq（測試專案位於 `AutoTest/TDKController.Tests/`）
**Target Platform**: Windows (MiniPC 工業設備控制器)
**Project Type**: DLL 模組（TDKController 函式庫內的設備控制模組）
**Performance Goals**: DIO 讀寫延遲 < 10ms（受限於硬體 DIO 板效能）
**Constraints**: 無內部輪詢執行緒；由外部呼叫者觸發 OSSD 讀取。單一 .cs 檔案模組。
**Scale/Scope**: 單一 LightCurtain 實例，6 個 DIO 通道映射（2 DI + 4 DO）

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | 憲章條款 | 狀態 | 說明 |
|---|---------|------|------|
| 1 | 命名慣例 (PascalCase/camelCase) | ✅ PASS | 類別/方法/屬性遵循 PascalCase，區域變數 camelCase |
| 2 | 組態屬性命名 `Config` | ✅ PASS | `LightCurtainConfig` 透過建構函式注入，公開屬性名稱為 `Config` |
| 3 | 單一職責 | ✅ PASS | LightCurtain 模組專注於光幕安全 I/O 控制 |
| 4 | 建構函式注入 + null-check | ✅ PASS | `ILogUtility` 與 `IOBoard[]` 注入並驗證 |
| 5 | 事件相依性訂閱規則 | ✅ PASS | LightCurtain 不注入 IConnector，無需 setter 訂閱模式 |
| 6 | IDisposable 模組生命週期 | ⚠️ N/A | 規格未要求 IDisposable；模組無非託管資源或背景執行緒 |
| 7 | 例外處理與日誌 | ✅ PASS | 公開方法以 try-catch 保護，使用 ILogUtility 記錄 |
| 8 | 錯誤碼範圍 -400..-499 | ✅ PASS | 於 `ErrorCode.cs` 以 `const int` 定義 LightCurtain 模組錯誤碼 |
| 9 | 單一 .cs 檔案 | ✅ PASS | 模組實作於 `TDKController/Module/LightCurtain.cs` |
| 10 | 禁止未授權類別 | ✅ PASS | 使用既有 `DioChannelConfig` 結構與 `ErrorCode` 常數定義；完整狀態查詢重用 `LightCurtainStatusChangedEventArgs` 作為回傳資料模型 |
| 11 | 介面優先 | ✅ PASS | 透過 `ILightCurtain` 介面暴露 |
| 12 | Lambda 使用規則 | ✅ PASS | 無 lambda 需求 |
| 13 | #region 分區 | ✅ PASS | 將按職責分區：Constants / Construction / IO Status Properties / Configuration & Mode / DI Operations / DO Operations / Status Snapshot / OSSD Safety Detection / Event Helpers |
| 14 | 測試覆蓋率 ≥ 90% | ✅ PLAN | 測試檔案位於 `AutoTest/TDKController.Tests/Unit/LightCurtainTests.cs`，並於最終驗證階段收集 coverage |
| 15 | 介面使用政策 | ✅ PASS | 不修改 IConnector、ILogUtility、HRESULT；僅擴充 ILightCurtain |
| 16 | 檔案建立政策 | ✅ PASS | 使用者 feature 請求隱含核准新增 EventArgs、DioChannelConfig 與測試檔 |
| 17 | 分層規則 | ✅ PASS | Module 層不引用 Service 層或 Controller 層 |
| 18 | YAGNI | ✅ PASS | 僅實作規格要求的功能 |

**Gate 結果**: ✅ PASS — T027、T029 已涵蓋 XML 文件、English 註解與 coverage 收集；#16 已經使用者核准

## Project Structure

### Documentation (this feature)

```text
specs/001-banner-light-curtain/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
TDKController/
├── Interface/
│   ├── ILightCurtain.cs         # 介面定義（已存在，需擴充）
│   └── ErrorCode.cs             # 錯誤碼常數定義（已存在，需新增 LightCurtain 範圍碼）
├── Config/
│   └── LightCurtainConfig.cs    # 組態類別（已存在，需擴充）
├── Module/
│   └── LightCurtain.cs          # 模組實作（已存在，需擴充）

DIO/
├── Interface/
│   ├── IIOBoard.cs              # DIO 板介面（唯讀）
│   ├── IDI.cs                   # 數位輸入介面（唯讀）
│   └── IDO.cs                   # 數位輸出介面（唯讀）

AutoTest/
├── TDKController.Tests/
│   └── Unit/
│       └── LightCurtainTests.cs # 單元測試（需建立）
```

**Structure Decision**: 所有實作程式碼置於已有的 TDKController 專案結構中。不新增專案。完整狀態快照以 `GetLightCurtainStatus(...)` 方法提供，並重用 `LightCurtainStatusChangedEventArgs` 作為單次回傳資料模型，以避免額外新增資料類別。測試檔案置於已有的 `AutoTest/TDKController.Tests/Unit/` 目錄。

## Complexity Tracking

> 無需記錄 — Constitution Check 無違規需要正當化。
