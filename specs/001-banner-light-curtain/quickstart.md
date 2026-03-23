# Quickstart: Banner Light Curtain Support

**Feature**: `001-banner-light-curtain` | **Date**: 2026-03-23

## 建構 Build

```bash
# 建構 TDKController 專案（包含 LightCurtain 模組）
msbuild TDKController/TDKController.csproj /p:Configuration=Debug

# 建構測試專案
msbuild AutoTest/TDKController.Tests/TDKController.Tests.csproj /p:Configuration=Debug
```

## 測試 Test

```bash
# 執行 LightCurtain 單元測試
nunit3-console.exe AutoTest/TDKController.Tests/bin/Debug/TDKController.Tests.dll --where "class == TDKController.Tests.Unit.LightCurtainTests"
```

## 使用範例 Usage

### 建立實例

```csharp
// 準備 DIO 板陣列
IOBoard[] ioBoards = new IOBoard[] { dio0, dio1, dio2, dio3 };

// 準備組態
var config = new LightCurtainConfig
{
    LTC_DI_OSSD1 = new DioChannelConfig { DioDeviceID = 0, PortID = 0, Channel_BitIndex = 0 },
    LTC_DI_OSSD2 = new DioChannelConfig { DioDeviceID = 0, PortID = 0, Channel_BitIndex = 1 },
    LTC_DO_Reset = new DioChannelConfig { DioDeviceID = 2, PortID = 0, Channel_BitIndex = 0 },
    LTC_DO_Test = new DioChannelConfig { DioDeviceID = 2, PortID = 0, Channel_BitIndex = 1 },
    LTC_DO_Interlock = new DioChannelConfig { DioDeviceID = 2, PortID = 0, Channel_BitIndex = 2 },
    LTC_DO_LTCLed = new DioChannelConfig { DioDeviceID = 2, PortID = 0, Channel_BitIndex = 3 },
    LightCurtainType = LightCurtainType.Enable_Always,
    LightCurtainVoltageMode = LightCurtainVoltageMode.Voltage24V
};

// 建立 LightCurtain 實例
ILightCurtain lightCurtain = new LightCurtain(config, ioBoards, logger);
```

### 訂閱事件

```csharp
lightCurtain.OSSDAlarmTriggered += OnOSSDAlarm;
lightCurtain.StatusChanged += OnStatusChanged;

void OnOSSDAlarm(object sender, LightCurtainAlarmEventArgs e)
{
    // OSSD1 或 OSSD2 進入不安全狀態
    logger.WriteLog("LTC", LogHeadType.Error,
        $"Light curtain alarm: OSSD1={e.OSSD1}, OSSD2={e.OSSD2}");
}

void OnStatusChanged(object sender, LightCurtainStatusChangedEventArgs e)
{
    // 任何訊號或模式變更
    logger.WriteLog("LTC", LogHeadType.Normal,
        $"LTC status: OSSD1={e.OSSD1}, OSSD2={e.OSSD2}, Mode={e.LightCurtainType}");
}
```

### 讀取安全狀態

```csharp
// 外部呼叫者主動讀取 OSSD（模組無內部輪詢）
int result = lightCurtain.ReadLightCurtainOSSD();
if (result == 0)
{
    bool isSafe = lightCurtain.OSSD1 && lightCurtain.OSSD2;
}
```

### 取得完整狀態快照

```csharp
LightCurtainStatusChangedEventArgs status;
int result = lightCurtain.GetLightCurtainStatus(out status);
if (result == 0)
{
    logger.WriteLog("LTC", LogHeadType.Normal,
        $"Snapshot: OSSD1={status.OSSD1}, OSSD2={status.OSSD2}, Reset={status.Reset}, LTCLed={status.LTCLed}");
}
```

### 控制 DO 輸出

```csharp
// 設定 Reset 輸出為 High
int result = lightCurtain.SetLightCurtainDOStatus(LightCurtainIO.Reset, true);

// 讀取 Interlock 輸出狀態
bool interlock;
result = lightCurtain.GetLightCurtainDOStatus(LightCurtainIO.Interlock, out interlock);
```

### 變更模式

```csharp
// 切換為傳送期間啟用
lightCurtain.SetLightCurtainType(LightCurtainType.Enable_InTransfer);

// 查詢目前模式
LightCurtainType currentType;
lightCurtain.GetLightCurtainType(out currentType);
```

## 異動檔案清單

| 檔案 | 操作 |
|------|------|
| `TDKController/Interface/ILightCurtain.cs` | 修改（擴充介面、新增 enum 與 EventArgs）|
| `TDKController/Interface/ErrorCode.cs` | 修改（新增 LightCurtain `const int` 錯誤碼）|
| `TDKController/Config/LightCurtainConfig.cs` | 修改（擴充為完整 Config + DioChannelConfig struct）|
| `TDKController/Module/LightCurtain.cs` | 修改（完整實作）|
| `AutoTest/TDKController.Tests/Unit/LightCurtainTests.cs` | 新增（單元測試）|
