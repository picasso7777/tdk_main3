# Data Model: Banner Light Curtain Support

**Feature**: `001-banner-light-curtain` | **Date**: 2026-03-23

## Enums

### LightCurtainIO

用途：識別光幕的 DIO 通道類型，供 `SetLightCurtainDOStatus` / `GetLightCurtainDOStatus` / `GetLightCurtainDIStatus` 方法選擇操作對象。

| 成員 | 值 | I/O 類型 | 說明 |
|------|-----|---------|------|
| OSSD1 | 0 | DI | 安全通道 1（Safety Channel 1）|
| OSSD2 | 1 | DI | 安全通道 2（Safety Channel 2）|
| Reset | 2 | DO | 光幕重置輸出 |
| Test | 3 | DO | 光幕測試輸出 |
| Interlock | 4 | DO | 互鎖輸出 |
| LTCLed | 5 | DO | 光幕狀態 LED 指示輸出 |

### LightCurtainType

用途：定義光幕運作模式，控制何時啟用安全偵測。

| 成員 | 值 | 說明 |
|------|-----|------|
| Disable | 0 | 光幕功能停用 |
| Enable_InTransfer | 1 | 僅在傳送期間啟用 |
| Enable_Always | 2 | 永遠啟用 |

### LightCurtainVoltageMode

用途：定義光幕硬體安裝的電壓極性配置。

| 成員 | 值 | 說明 |
|------|-----|------|
| Voltage24V | 0 | 24V 電壓模式 |
| Voltage0V | 1 | 0V 電壓模式 |

### ErrorCode 擴充（-400..-499 範圍）

以下為 `ErrorCode.cs` 中的 `const int` 定義。

| 成員 | 值 | 說明 |
|------|-----|------|
| LightCurtainError | -400 | 基底錯誤（已存在）|
| LightCurtainNotConfigured | -401 | 模組尚未設定有效組態 |
| LightCurtainDisabled | -402 | 功能已停用 |
| LightCurtainDioReadFailed | -403 | DIO 讀取操作失敗 |
| LightCurtainDioWriteFailed | -404 | DIO 寫入操作失敗 |
| LightCurtainInvalidChannel | -405 | 指定的 IO 通道無效 |
| LightCurtainUnsafeState | -406 | 光幕處於不安全狀態 |

## Structs

### DioChannelConfig

用途：描述單一 DIO 通道的板卡索引、port 索引、bit 索引的映射。

| 欄位 | 型別 | 說明 |
|------|------|------|
| DioDeviceID | int | `IOBoard[]` 陣列索引 |
| PortID | int | Port 索引（零起始）|
| Channel_BitIndex | int | Port 內 bit 索引（零起始）|

**設計決策**：`DioDeviceID` 使用 `-1` 作為 sentinel 表示「未設定」。驗證時若任一 `DioChannelConfig` 的 `DioDeviceID < 0`，則視為必要映射缺漏並拒絕組態。

## Classes

### LightCurtainConfig

用途：定義一個光幕實例的完整 DIO 通道映射、運作模式與電壓模式。

**DO 映射屬性**:

| 屬性 | 型別 | 說明 |
|------|------|------|
| LTC_DO_Reset | DioChannelConfig | Reset 輸出的 DIO 通道映射 |
| LTC_DO_Test | DioChannelConfig | Test 輸出的 DIO 通道映射 |
| LTC_DO_Interlock | DioChannelConfig | Interlock 輸出的 DIO 通道映射 |
| LTC_DO_LTCLed | DioChannelConfig | LTCLed 輸出的 DIO 通道映射 |

**DI 映射屬性**:

| 屬性 | 型別 | 說明 |
|------|------|------|
| LTC_DI_OSSD1 | DioChannelConfig | OSSD1 安全通道的 DIO 通道映射 |
| LTC_DI_OSSD2 | DioChannelConfig | OSSD2 安全通道的 DIO 通道映射 |

**組態屬性**:

| 屬性 | 型別 | 說明 |
|------|------|------|
| LightCurtainType | LightCurtainType | 運作模式 |
| LightCurtainVoltageMode | LightCurtainVoltageMode | 電壓模式 |

**驗證規則** (FR-003):
- 所有 6 個 DioChannelConfig 的 `DioDeviceID` 必須在 `IOBoard[]` 陣列索引範圍內
- 所有 required mappings 必須完整提供，不得保留未設定的預設值（`DioDeviceID < 0` 視為未設定）
- 任兩個 required signals 不得映射到相同的 `(DioDeviceID, PortID, Channel_BitIndex)` 組合，不區分 DI/DO 類型

### LightCurtainAlarmEventArgs

用途：安全告警事件資料，當光幕從安全狀態轉為不安全狀態時觸發。

| 屬性 | 型別 | 說明 |
|------|------|------|
| OSSD1 | bool | 安全通道 1 狀態（true = 安全）|
| OSSD2 | bool | 安全通道 2 狀態（true = 安全）|

繼承自 `EventArgs`。

### LightCurtainStatusChangedEventArgs

用途：狀態變更事件資料，任何邏輯訊號、運作模式或電壓模式變更時觸發。此型別同時作為 `GetLightCurtainStatus(...)` 的完整狀態快照資料模型。

| 屬性 | 型別 | 說明 |
|------|------|------|
| OSSD1 | bool | 安全通道 1 狀態 |
| OSSD2 | bool | 安全通道 2 狀態 |
| Reset | bool | Reset 輸出狀態 |
| Test | bool | Test 輸出狀態 |
| Interlock | bool | Interlock 輸出狀態 |
| LTCLed | bool | LTCLed 輸出狀態 |
| LightCurtainVoltageMode | LightCurtainVoltageMode | 電壓模式 |
| LightCurtainType | LightCurtainType | 運作模式 |

繼承自 `EventArgs`。

## State & Relationships

### 安全狀態判定（FR-007）

```
安全 (Safe):   OSSD1 == true AND OSSD2 == true
不安全 (Unsafe): OSSD1 == false OR OSSD2 == false
```

- 不安全狀態在兩個 OSSD 通道恢復安全值時自動清除（無鎖定）
- 安全狀態由 `ReadLightCurtainOSSD()` 呼叫時被動更新

### 告警觸發條件（FR-008）

```
前次安全 AND 本次不安全 → 觸發 OSSDAlarmTriggered 事件
前次不安全 AND 本次安全 → 告警自動清除（無事件）
```

### 狀態變更觸發條件（FR-009）

任何以下值變更時觸發 `StatusChanged` 事件：
- OSSD1, OSSD2（經由 ReadLightCurtainOSSD）
- Reset, Test, Interlock, LTCLed（經由 SetLightCurtainDOStatus 寫入後，或 GetLightCurtainDOStatus 讀取硬體值與本地快取不同時）
- LightCurtainType（經由 SetLightCurtainType 或 Config setter）
- LightCurtainVoltageMode（經由 SetVoltageMode 或 Config setter）

### 實體關係

```
LightCurtain ─implements─> ILightCurtain
LightCurtain ─has─> LightCurtainConfig (1:1, via Config property)
LightCurtain ─uses─> IOBoard[] (injected, indexed by DioChannelConfig.DioDeviceID)
LightCurtain ─uses─> ILogUtility (injected, for logging)
LightCurtain ─emits─> LightCurtainAlarmEventArgs
LightCurtain ─emits─> LightCurtainStatusChangedEventArgs
LightCurtain ─returns─> LightCurtainStatusChangedEventArgs (status snapshot)
LightCurtainConfig ─contains─> DioChannelConfig (6 instances: 4 DO + 2 DI)
```

### 檔案分佈

| 型別 | 定義位置 |
|------|---------|
| LightCurtainIO, LightCurtainType, LightCurtainVoltageMode | `TDKController/Interface/ILightCurtain.cs` |
| LightCurtainAlarmEventArgs, LightCurtainStatusChangedEventArgs | `TDKController/Interface/ILightCurtain.cs` |
| ILightCurtain | `TDKController/Interface/ILightCurtain.cs` |
| DioChannelConfig | `TDKController/Config/LightCurtainConfig.cs` |
| LightCurtainConfig | `TDKController/Config/LightCurtainConfig.cs` |
| ErrorCode 擴充 | `TDKController/Interface/ErrorCode.cs` |
| LightCurtain (實作) | `TDKController/Module/LightCurtain.cs` |
| LightCurtainTests | `AutoTest/TDKController.Tests/Unit/LightCurtainTests.cs` |
