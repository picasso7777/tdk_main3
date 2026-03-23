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

        /// <summary>Current voltage mode.</summary>
        LightCurtainVoltageMode LightCurtainVoltageMode { get; set; }

        /// <summary>Current operating mode.</summary>
        LightCurtainType LightCurtainType { get; set; }

        #endregion

        #region Methods

        /// <summary>Sets the operating mode.</summary>
        int SetLightCurtainType(LightCurtainType lightCurtainType);

        /// <summary>Gets the current operating mode.</summary>
        int GetLightCurtainType(out LightCurtainType lightCurtainType);

        /// <summary>Sets the voltage mode.</summary>
        int SetVoltageMode(LightCurtainVoltageMode lightCurtainVoltageMode);

        /// <summary>Gets the current voltage mode.</summary>
        int GetVoltageMode(out LightCurtainVoltageMode lightCurtainVoltageMode);

        /// <summary>
        /// Reads OSSD safety inputs from DIO hardware.
        /// Updates OSSD1/OSSD2 properties and triggers alarm/status events if changed.
        /// </summary>
        int ReadLightCurtainOSSD();

        /// <summary>Gets a single-response snapshot of the current light curtain state.</summary>
        int GetLightCurtainStatus(out LightCurtainStatusChangedEventArgs status);

        /// <summary>Sets a DO channel to specified state.</summary>
        int SetLightCurtainDOStatus(LightCurtainIO io, bool turnOn);

        /// <summary>Gets the current state of a DO channel.</summary>
        int GetLightCurtainDOStatus(LightCurtainIO io, out bool turnOn);

        /// <summary>Gets the current state of a DI channel.</summary>
        int GetLightCurtainDIStatus(LightCurtainIO io, out bool value);

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
| 有效 `LightCurtainConfig` | 所有 DioChannelConfig 映射完整、DeviceID 在範圍內、無重複映射 | 接受並儲存組態 |
| `null` | — | 拋出 `ArgumentNullException` |
| 無效映射（缺欄位、DeviceID 超出範圍、重複通道） | FR-003 驗證失敗 | 拋出 `ArgumentException`，附帶失敗原因描述；保留原有組態 |

## 方法行為契約

### SetLightCurtainDOStatus

| 輸入 | 條件 | 回傳 |
|------|------|------|
| io = Reset/Test/Interlock/LTCLed | 組態有效 且 非 Disable | `0` |
| io = OSSD1 或 OSSD2 | DI 通道不支援 DO 寫入 | `ErrorCode.LightCurtainInvalidChannel` |
| 任意 | 尚未設定有效組態 | `ErrorCode.LightCurtainNotConfigured` |
| 任意 | 功能停用 (Disable) | `ErrorCode.LightCurtainDisabled` |
| 任意 | 光幕目前不安全 | `ErrorCode.LightCurtainUnsafeState` |
| 任意 | DIO 板寫入失敗 | `ErrorCode.LightCurtainDioWriteFailed` |

### GetLightCurtainDOStatus

| 輸入 | 條件 | 回傳 |
|------|------|------|
| io = Reset/Test/Interlock/LTCLed | 組態有效 | `0` + out turnOn |
| io = OSSD1 或 OSSD2 | DI 通道不支援 DO 讀取 | `ErrorCode.LightCurtainInvalidChannel` |
| 任意 | 尚未設定有效組態 | `ErrorCode.LightCurtainNotConfigured` |
| 任意 | DIO 板讀取失敗 | `ErrorCode.LightCurtainDioReadFailed` |

### GetLightCurtainDIStatus

| 輸入 | 條件 | 回傳 |
|------|------|------|
| io = OSSD1 或 OSSD2 | 組態有效 | `0` + out value |
| io = Reset/Test/Interlock/LTCLed | DO 通道不支援 DI 讀取 | `ErrorCode.LightCurtainInvalidChannel` |
| 任意 | 尚未設定有效組態 | `ErrorCode.LightCurtainNotConfigured` |
| 任意 | DIO 板讀取失敗 | `ErrorCode.LightCurtainDioReadFailed` |

### GetLightCurtainStatus

| 條件 | 回傳 | 副作用 |
|------|------|--------|
| 組態有效 | `0` + out status | 回傳目前 OSSD1、OSSD2、Reset、Test、Interlock、LTCLed、LightCurtainType、LightCurtainVoltageMode |
| 尚未設定有效組態 | `ErrorCode.LightCurtainNotConfigured` | 不回傳快照 |

### ReadLightCurtainOSSD

| 條件 | 回傳 | 副作用 |
|------|------|--------|
| 組態有效，兩通道讀取成功 | `0` | 更新 OSSD1/OSSD2 屬性；若狀態變更則觸發 StatusChanged；若從安全→不安全則觸發 OSSDAlarmTriggered |
| 尚未設定有效組態 | `ErrorCode.LightCurtainNotConfigured` | 不更新狀態 |
| DIO 讀取失敗 | `ErrorCode.LightCurtainDioReadFailed` | 不更新狀態 |

### SetLightCurtainType / SetVoltageMode

| 條件 | 回傳 | 副作用 |
|------|------|--------|
| 有效 enum 值 | `0` | 更新對應屬性；若值變更則觸發 StatusChanged |

## 事件契約

### OSSDAlarmTriggered

- **觸發時機**: `ReadLightCurtainOSSD()` 偵測到從安全狀態轉為不安全狀態
- **資料**: `LightCurtainAlarmEventArgs { OSSD1, OSSD2 }`
- **清除**: 自動清除（下次 ReadLightCurtainOSSD 偵測到安全狀態時不觸發事件）

### StatusChanged

- **觸發時機**: 任何訊號值、運行模式或電壓模式變更
- **資料**: `LightCurtainStatusChangedEventArgs { OSSD1, OSSD2, Reset, Test, Interlock, LTCLed, LightCurtainVoltageMode, LightCurtainType }`
