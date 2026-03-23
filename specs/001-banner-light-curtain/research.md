# Research: Banner Light Curtain Support

**Feature**: `001-banner-light-curtain` | **Date**: 2026-03-23

## R1: DIO 存取模式

**Decision**: 透過 `IOBoard[]` 陣列索引存取 DIO 板，使用 `SetOutputBit`/`GetOutputBit`/`GetInputBit` 方法，搭配 `DioChannelConfig` 結構定位通道。

**Rationale**:
- `IDO.SetOutputBit(portIndex, bitIndex, byte value)` — value 為 `byte` (0/1)，非 `bool`
- `IDO.GetOutputBit(portIndex, bitIndex, out byte value)` — out 為 `byte`
- `IDI.GetInputBit(portIndex, bitIndex, out byte value)` — out 為 `byte`
- 所有方法回傳 `int` 錯誤碼（0 = 成功）
- PlantUML 規格使用 `bool` turnOn 參數，需在模組內做 `bool ↔ byte` 轉換
- 使用者已確認建構函式注入型別以 `IOBoard[]` 為準

**Alternatives considered**:
- 使用 `SetOutput`/`GetInput` 整 port 讀寫 — 不適合，因為每個光幕訊號映射至獨立 bit，且整 port 寫入可能影響其他 bit

## R2: DioChannelConfig 結構

**Decision**: 需新建 `DioChannelConfig` struct，定義於 `TDKController/Config/LightCurtainConfig.cs` 同檔。

**Rationale**:
- 目前專案中不存在此結構
- PlantUML 規格定義為 struct，含三個 int 欄位：`DioDeviceID`, `PortID`, `Channel_BitIndex`
- 放在 LightCurtainConfig.cs 同檔以遵循單一模組單一檔案精神
- 宣告為 `public struct` 以供介面使用

**Alternatives considered**:
- 獨立新檔案 — 違反檔案建立政策
- 定義在 DIO 專案 — DIO 為基礎設施層，不應承載 TDKController 的組態結構

## R3: EventArgs 模式

**Decision**: 使用 `EventHandler<T>` 泛型事件模式，EventArgs 類別定義於 `ILightCurtain.cs` 介面檔案中。

**Rationale**:
- PlantUML 規格明確指定 `EventHandler<LightCurtainAlarmEventArgs>` 和 `EventHandler<LightCurtainStatusChangedEventArgs>`
- 既有模組用自訂 delegate（如 `StatusChangedEventHandler`），但 PlantUML 設計已指定泛型模式
- EventArgs 放在介面檔以便外部消費者可引用型別

**Alternatives considered**:
- 自訂 delegate 模式（如 LoadportActor）— 可行但 PlantUML 已指定泛型模式
- EventArgs 放在模組檔 — 會使介面消費者無法獨立取得型別定義

## R4: Enum 定義

**Decision**: 三個 enum（`LightCurtainIO`, `LightCurtainType`, `LightCurtainVoltageMode`）定義於 `ILightCurtain.cs` 介面檔案中。

**Rationale**:
- 目前 C# 程式碼中不存在這三個 enum
- 放在介面檔與介面消費者同一命名空間，方便引用
- 遵循介面相關共用型別集中於 Interface 目錄的既有模式

**Alternatives considered**:
- 放在 Config 目錄 — 不符合介面導向設計，enum 主要由介面方法簽章使用
- 獨立檔案 — 不必要的檔案擴增

## R5: Config 類別模式

**Decision**: `LightCurtainConfig` 改為 `public class`，使用 auto-properties，包含 6 個 `DioChannelConfig` 屬性與 2 個 enum 屬性。

**Rationale**:
- 既有已實作的 config（`LoadportActorConfig`）使用 `public class` + auto-properties
- 目前 `LightCurtainConfig` 是 `internal class` 空殼，需改為 `public`
- PlantUML 定義了完整屬性清單

**Alternatives considered**:
- 保持 internal — 會使介面無法暴露 Config 屬性型別

## R6: ErrorCode 擴充

**Decision**: 在 `ErrorCode.cs` 中以 `const int` 定義以下 LightCurtain 範圍錯誤碼：

| 值 | 名稱 | 說明 |
|----|------|------|
| -400 | LightCurtainError | 基底錯誤（已存在）|
| -401 | LightCurtainNotConfigured | 模組尚未設定有效組態 |
| -402 | LightCurtainDisabled | 功能已停用 |
| -403 | LightCurtainDioReadFailed | DIO 讀取操作失敗 |
| -404 | LightCurtainDioWriteFailed | DIO 寫入操作失敗 |
| -405 | LightCurtainInvalidChannel | 指定的 IO 通道無效 |
| -406 | LightCurtainUnsafeState | 光幕處於不安全狀態，拒絕操作 |

**Rationale**:
- 對應規格 FR-011 要求的不同失敗情境，每個情境一個明確錯誤碼
- 使用者已確認採用 `const int`，以符合 spec 與 constitution

## R7: bool ↔ byte 轉換策略

**Decision**: 模組公開介面使用 `bool`（如規格），內部呼叫 DIO 時轉換為 `byte`。

**Rationale**:
- DIO 介面方法只接受/回傳 `byte`（0 或 1）
- 光幕模組的業務語意是 bool（安全/不安全、開/關）
- 轉換邏輯封裝在模組內部，不暴露 byte 細節給消費者

**轉換方式**: `(byte)(turnOn ? 1 : 0)` 和 `value != 0`

## R8: 建構函式簽章

**Decision**: `LightCurtain(LightCurtainConfig config, IOBoard[] ioBoards, ILogUtility logger)`

**Rationale**:
- PlantUML 指定 `LightCurtain(config : LightCurtainConfig, iIOBoard : IOBoard[])`
- 規格 FR-001 要求注入 `IOBoard[]` 陣列與 `ILogUtility`
- 憲章要求 ILogUtility 透過建構函式注入
- config 在前（與 LoadportActor 模式一致），ioBoards 次之，logger 最後

## R9: Full Status Snapshot

**Decision**: 提供 dedicated `GetLightCurtainStatus(...)` 方法，並重用 `LightCurtainStatusChangedEventArgs` 作為單次回傳的完整狀態資料模型。

**Rationale**:
- 使用者已確認 User Story 3 需要 single-response status snapshot API
- 重用既有 `LightCurtainStatusChangedEventArgs` 可避免再新增未授權資料類別
- 這個資料模型已完整涵蓋 OSSD1、OSSD2、四個 DO、運行模式與電壓模式

## R10: Interface Mode/Voltage Properties Read-Only

**Decision**: `ILightCurtain` 介面上的 `LightCurtainType` 與 `LightCurtainVoltageMode` 屬性宣告為 `{ get; }`（唯讀），修改僅透過 `SetLightCurtainType()` 與 `SetVoltageMode()` 方法進行。

**Rationale**:
- Property setter 無法回傳 `int` 錯誤碼，呼叫者無法得知操作是否成功
- Property setter 無法觸發 `StatusChanged` 事件（除非以隱式副作用實作，但違反最小驚訝原則）
- 保留可寫 setter 會使呼叫者繞過驗證與事件機制
- Set 方法已提供完整的錯誤回傳與事件觸發行為，setter 為冗餘入口

**Alternatives considered**:
- 保留 setter 並內部轉呼叫 Set 方法（忽略回傳值）— 隱式副作用難以除錯
- 移除 Set 方法、保留 setter — 無法回傳錯誤碼，違反統一錯誤碼政策

## R11: GetDOStatus Triggers StatusChanged on Mismatch

**Decision**: `GetLightCurtainDOStatus()` 讀取硬體 DO 狀態後，若與本地快取值不同，則更新屬性並觸發 `StatusChanged` 事件。

**Rationale**:
- DO 輸出值可能被外部因素改變（例如 DIO 板硬體重置、其他控制器寫入同一 port）
- Get 操作讀取實際硬體值，若與模組快取不一致，需同步內部狀態並通知消費者
- 確保 `StatusChanged` 事件完整反映所有可偵測的狀態變化，而非僅限於本模組的寫入操作

**Alternatives considered**:
- Get 操作不觸發事件（純讀取）— 可能導致模組內部狀態與硬體不同步，消費者感知不到外部變更
