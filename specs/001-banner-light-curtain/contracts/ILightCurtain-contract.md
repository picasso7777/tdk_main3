# Contract: ILightCurtain Interface

**Feature**: `001-banner-light-curtain` | **Date**: 2026-03-23
**定義位置**: `TDKController/Interface/ILightCurtain.cs`

## 介面簽章

```csharp
namespace TDKController.Interface
{
    public interface ILightCurtain
    {
        #region Logical IO Status (read-only properties)

        /// <summary>Safety channel array. Index 0 = OSSD1, index 1 = OSSD2.</summary>
        bool[] OSSD { get; }

        /// <summary>Safety channel 1 status. True = safe.</summary>
        bool OSSD1 { get; }

        /// <summary>Safety channel 2 status. True = safe.</summary>
        bool OSSD2 { get; }

        /// <summary>Reset output status.</summary>
        bool Reset { get; }

        /// <summary>Test output status.</summary>
        bool Test { get; }

        /// <summary>Interlock output status.</summary>
        bool Interlock { get; }

        /// <summary>LTC LED output status.</summary>
        bool LTCLed { get; }

        #endregion

        #region Configuration Properties

        /// <summary>Light curtain configuration with DIO channel mappings.</summary>
        LightCurtainConfig Config { get; set; }

        /// <summary>Current voltage mode (read-only; use SetVoltageMode to modify).</summary>
        LightCurtainVoltageMode LightCurtainVoltageMode { get; }

        /// <summary>Current operating mode (read-only; use SetLightCurtainType to modify).</summary>
        LightCurtainType LightCurtainType { get; }

        #endregion

        #region Methods

        /// <summary>Sets the operating mode.</summary>
        ErrorCode SetLightCurtainType(LightCurtainType lightCurtainType);

        /// <summary>Gets the current operating mode.</summary>
        ErrorCode GetLightCurtainType(out LightCurtainType lightCurtainType);

        /// <summary>Sets the voltage mode.</summary>
        ErrorCode SetVoltageMode(LightCurtainVoltageMode lightCurtainVoltageMode);

        /// <summary>Gets the current voltage mode.</summary>
        ErrorCode GetVoltageMode(out LightCurtainVoltageMode lightCurtainVoltageMode);

        /// <summary>
        /// Reads OSSD safety inputs from DIO hardware.
        /// Updates OSSD1/OSSD2 properties and triggers alarm/status events if changed.
        /// </summary>
        ErrorCode ReadLightCurtainOSSD(out bool lTCTriggered);

        /// <summary>Manually raises the current OSSD alarm event using cached values.</summary>
        ErrorCode TriggerLightCurtainAlarm();

        /// <summary>Gets a single-response snapshot of the current light curtain state.</summary>
        ErrorCode GetLightCurtainStatus(out LightCurtainStatusChangedEventArgs status);

        /// <summary>Sets a DO channel to specified state.</summary>
        ErrorCode SetLightCurtainDOStatus(LightCurtainIO io, bool turnOn);

        /// <summary>Gets the current state of a DO channel.</summary>
        ErrorCode GetLightCurtainDOStatus(LightCurtainIO io, out bool turnOn);

        /// <summary>Gets the current state of a DI channel.</summary>
        ErrorCode GetLightCurtainDIStatus(LightCurtainIO io, out bool value);

        #endregion

        #region Events

        /// <summary>Raised when light curtain transitions from safe to unsafe state.</summary>
        event EventHandler<LightCurtainAlarmEventArgs> OSSDAlarmTriggered;

        /// <summary>Raised when any signal, mode, or voltage setting changes.</summary>
        event EventHandler<LightCurtainStatusChangedEventArgs> StatusChanged;

        #endregion
    }
}
```

## 屬性行為契約

### Config (setter)

| 輸入 | 條件 | 行為 |
|------|------|------|
| 有效 `LightCurtainConfig` | 所有 DioChannelConfig 映射完整、DeviceID 在範圍內、無重複映射 | 接受並儲存組態；同步更新模組級 `LightCurtainType` 與 `LightCurtainVoltageMode` 屬性；若任一模式值與先前不同，觸發 `StatusChanged` |
| `null` | — | 拋出 `ArgumentNullException` |
| 無效映射（缺欄位、DeviceID 超出範圍、重複通道） | FR-003 驗證失敗 | 拋出 `ArgumentException`，附帶失敗原因描述；保留原有組態 |

### OSSD / Dio Mapping

- `OSSD[0]` 對應 `OSSD1`
- `OSSD[1]` 對應 `OSSD2`
- `LTC_DI_OSSD` 必須存在且固定包含 2 個 `DioChannelConfig`

## 方法行為契約

### SetLightCurtainDOStatus

| 輸入 | 條件 | 回傳 |
|------|------|------|
| io = Reset/Test/Interlock/LTCLed | 組態有效 且 非 Disable | `ErrorCode.Success` |
| io = OSSD1 或 OSSD2 | DI 通道不支援 DO 寫入 | `ErrorCode.LightCurtainInvalidChannel` |
| 任意 | 尚未設定有效組態 | `ErrorCode.LightCurtainNotConfigured` |
| 任意 | 功能停用 (Disable) | `ErrorCode.LightCurtainDisabled` |
| 任意 | 光幕目前不安全 | `ErrorCode.LightCurtainUnsafeState` |
| 任意 | DIO 板寫入失敗 | `ErrorCode.LightCurtainDioWriteFailed` |

### GetLightCurtainDOStatus

| 輸入 | 條件 | 回傳 | 副作用 |
|------|------|------|--------|
| io = Reset/Test/Interlock/LTCLed | 組態有效 | `ErrorCode.Success` + out turnOn | 若讀取硬體值與本地快取不同，更新屬性並觸發 `StatusChanged` |
| io = OSSD1 或 OSSD2 | DI 通道不支援 DO 讀取 | `ErrorCode.LightCurtainInvalidChannel` | — |
| 任意 | 尚未設定有效組態 | `ErrorCode.LightCurtainNotConfigured` | — |
| 任意 | DIO 板讀取失敗 | `ErrorCode.LightCurtainDioReadFailed` | — |

### GetLightCurtainDIStatus

| 輸入 | 條件 | 回傳 |
|------|------|------|
| io = OSSD1 或 OSSD2 | 組態有效 | `ErrorCode.Success` + out value |
| io = Reset/Test/Interlock/LTCLed | DO 通道不支援 DI 讀取 | `ErrorCode.LightCurtainInvalidChannel` |
| 任意 | 尚未設定有效組態 | `ErrorCode.LightCurtainNotConfigured` |
| 任意 | DIO 板讀取失敗 | `ErrorCode.LightCurtainDioReadFailed` |

### GetLightCurtainStatus

| 條件 | 回傳 | 副作用 |
|------|------|--------|
| 組態有效 | `ErrorCode.Success` + out status | 回傳模組內部快取值（OSSD1、OSSD2、Reset、Test、Interlock、LTCLed、LightCurtainType、LightCurtainVoltageMode），不觸發硬體 I/O |
| 尚未設定有效組態 | `ErrorCode.LightCurtainNotConfigured` | 不回傳快照 |

### ReadLightCurtainOSSD

| 條件 | 回傳 | 副作用 |
|------|------|--------|
| 組態有效，兩通道讀取成功 | `ErrorCode.Success` + `out lTCTriggered` | 更新 `OSSD`、`OSSD1`、`OSSD2` 屬性；若狀態變更則觸發 StatusChanged；若從安全→不安全則觸發 OSSDAlarmTriggered；當前狀態為 unsafe 時 `lTCTriggered = true` |
| 尚未設定有效組態 | `ErrorCode.LightCurtainNotConfigured` + `out false` | 不更新狀態 |
| DIO 讀取失敗 | `ErrorCode.LightCurtainDioReadFailed` + `out false` | 不更新狀態 |

### TriggerLightCurtainAlarm

| 條件 | 回傳 | 副作用 |
|------|------|--------|
| 組態有效 | `ErrorCode.Success` | 以目前快取的 `OSSD1` / `OSSD2` 值觸發 `OSSDAlarmTriggered` |
| 尚未設定有效組態 | `ErrorCode.LightCurtainNotConfigured` | 不觸發事件 |

### SetLightCurtainType / SetVoltageMode

| 條件 | 回傳 | 副作用 |
|------|------|--------|
| 有效 enum 值 | `ErrorCode.Success` | 更新對應屬性；若值變更則觸發 StatusChanged |

**同步規則**：`SetLightCurtainType` / `SetVoltageMode` 僅修改模組級 mode 屬性，不回寫 `LightCurtainConfig` 物件。Config setter 則同步更新模組級 mode 屬性（見上方 Config setter 行為表）。

## 事件契約

### OSSDAlarmTriggered

- **觸發時機**: `ReadLightCurtainOSSD()` 偵測到從安全狀態轉為不安全狀態
- **資料**: `LightCurtainAlarmEventArgs { OSSD1, OSSD2 }`
- **清除**: 自動清除（下次 ReadLightCurtainOSSD 偵測到安全狀態時不觸發事件）

### StatusChanged

- **觸發時機**: 任何訊號值、運作模式或電壓模式變更
- **資料**: `LightCurtainStatusChangedEventArgs { OSSD1, OSSD2, Reset, Test, Interlock, LTCLed, LightCurtainVoltageMode, LightCurtainType }`
