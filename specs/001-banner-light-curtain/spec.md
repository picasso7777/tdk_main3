# Feature Specification: Banner Light Curtain Support

**Feature Branch**: `001-banner-light-curtain`  
**Created**: 2026-03-23  
**Status**: Draft  
**Input**: User description: "請參考此檔案,建立 LightCutrain 模組在 TDKController 底下, 此為Banner Light Curtain"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Detect Unsafe Curtain States (Priority: P1)

As an equipment controller user, I need the system to recognize when the Banner light curtain becomes unsafe so that wafer transfer or motion logic can stop before continuing in an unsafe condition.

**Why this priority**: Unsafe-state detection is the primary business value of the feature. Without it, the module does not provide meaningful safety protection.

**Independent Test**: Can be fully tested by configuring a valid light curtain profile, simulating safe and unsafe input combinations, and verifying that the system reports safe status normally and raises an alarm when protection is violated.

**Acceptance Scenarios**:

1. **Given** a configured light curtain in a safe state, **When** the controller reads the safety inputs, **Then** the system reports both safety channels as safe and does not raise an alarm.
2. **Given** a configured light curtain in a safe state, **When** either safety channel changes to an unsafe state, **Then** the system marks the curtain as unsafe and emits an alarm notification that includes both safety channel states.

---

### User Story 2 - Configure Operating Behavior (Priority: P2)

As a field engineer, I need to configure the Banner light curtain's signal mapping, operating mode, and voltage mode so that the controller behaves correctly for the installed hardware.

**Why this priority**: A valid configuration is required before the module can be trusted in production, but it is secondary to detecting unsafe states once configured.

**Independent Test**: Can be fully tested by entering a complete configuration, retrieving it, changing operating modes, and verifying that invalid or incomplete configurations are rejected.

**Acceptance Scenarios**:

1. **Given** a new controller instance with no accepted light curtain configuration, **When** an engineer provides all required signal mappings and selects an operating mode and voltage mode, **Then** the system accepts the configuration and makes it available for later retrieval.
2. **Given** a light curtain configuration with missing or conflicting signal mappings, **When** an engineer attempts to enable the feature, **Then** the system rejects the configuration and returns a clear failure result.

---

### User Story 3 - Observe And Control Light Curtain Signals (Priority: P3)

As a maintenance or host-side consumer, I need to read the current light curtain status and control supported output signals so that I can diagnose behavior and manage the device during permitted operating states.

**Why this priority**: Status visibility and controllable outputs improve supportability and diagnostics, but they depend on the core safety and configuration behavior being present first.

**Independent Test**: Can be fully tested by reading the full logical state, changing a supported output signal, and confirming that the reported status reflects the new state.

**Acceptance Scenarios**:

1. **Given** a configured light curtain, **When** a caller requests the current status, **Then** the system returns the current values for both safety inputs, all supported output signals, the operating mode, and the voltage mode in a single response.
2. **Given** a configured light curtain in a mode that allows output control, **When** a caller sets a supported output signal, **Then** the system updates the signal state and publishes an updated status notification.

### Edge Cases

- The configuration references unavailable channels or assigns the same channel to multiple required signals.
- One safety channel reports safe while the other reports unsafe, creating a mismatched safety state.
- A caller requests status or output control before a valid configuration has been accepted.
- The operating mode changes while the light curtain is already in an alarmed or unsafe state.
- A caller attempts to control outputs while the feature is disabled or while the current state is not safe for continued operation.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST support a Banner light curtain profile with two safety input channels (DI) and four controllable output signals (DO) plus one status indicator output. The module receives an `IOBoard[]` array and an `ILogUtility` instance via constructor injection, and accesses each channel through `DioChannelConfig.DioDeviceID` as the array index.
- **FR-002**: The system MUST allow callers to define, update, and retrieve the light curtain configuration for each controller instance.
- **FR-003**: The system MUST validate that all required signal mappings are complete, non-conflicting, and usable before the light curtain can be enabled.
- **FR-004**: The system MUST support three operating modes: disabled, enabled during transfer only, and enabled at all times.
- **FR-005**: The system MUST support two voltage modes representing the installed light curtain polarity options and report the currently selected mode.
- **FR-006**: The system MUST allow callers to read the current logical state of both safety input channels and all supported output signals on demand via explicit method calls (e.g. `ReadLightCurtainOSSD()`). The module does not contain an internal polling thread; polling responsibility belongs to the caller.
- **FR-007**: The system MUST treat the light curtain as unsafe when either safety input indicates interruption, fault, or a mismatched safety condition. The unsafe state MUST auto-clear when both safety inputs return to safe values; no explicit reset action is required.
- **FR-008**: The system MUST generate an alarm notification whenever the light curtain transitions from a safe state to an unsafe state, including the current values of both safety inputs. The alarm condition clears automatically when both OSSD channels report safe.
- **FR-009**: The system MUST generate a status change notification whenever any reported logical signal, operating mode, or voltage mode changes.
- **FR-010**: The system MUST allow callers to set and retrieve the supported output signals only when configuration and operating conditions permit the action.
- **FR-011**: The system MUST return a clear failure result (using `const int` error codes in the `-400..-499` range) when a caller requests an operation that is not allowed because the feature is disabled, unconfigured, or currently unsafe.
- **FR-012**: The system MUST keep the latest accepted configuration and operating selections available for later readback during the active controller lifecycle.
- **FR-013**: The system MUST operate as a self-contained module with no dependency on LoadportActor or any loadport workflow state.

### Key Entities *(include if feature involves data)*

- **Light Curtain Configuration**: Defines the required signal mappings, selected operating mode, and selected voltage mode for one standalone Banner light curtain instance.
- **Light Curtain State**: Represents the current logical values of the two safety inputs, the supported outputs, and the active mode selections used by callers to determine whether the curtain is safe.
- **Alarm Notification**: Represents a safety event raised when the light curtain transitions into an unsafe state and includes the safety input values that caused the alarm.
- **Status Change Notification**: Represents the latest reported operating state after any signal, operating mode, or voltage mode change.

### Assumptions

- Each Banner light curtain instance is managed independently and has no coupling to LoadportActor or loadport workflows.
- The transfer-only operating mode is activated and deactivated by an external caller; the module itself does not monitor or depend on loadport status, load/unload commands, or transfer workflow state.
- Alarm and status notifications are consumed through existing controller-side notification patterns rather than requiring a new standalone user interface.

## Clarifications

### Session 2026-03-23

- Q: When both OSSD safety channels return to safe values, how should the light curtain recover from unsafe state? → A: Auto-clear (no latch). LTCIN reads OSSD state directly; alarm clears when inputs recover.
- Q: LightCurtain 模組透過什麼方式存取 DIO 訊號？ → A: 注入 IOBoard[] 陣列，透過 DioChannelConfig.DioDeviceID 索引存取對應裝置。
- Q: OSSD 安全輸入應該如何被讀取？ → A: 被動式，外部呼叫者主動呼叫 ReadLightCurtainOSSD() 讀取，模組不含內部輪詢執行緒。
- Q: LightCurtain 錯誤碼範圍？ → A: -400..-499（專案規範已預分配）。
- Q: LightCurtain 是否需要注入 ILogUtility？ → A: 是，構造函式注入 ILogUtility。

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Engineers can complete initial Banner light curtain configuration, validation, and readback in under 5 minutes per light curtain instance.
- **SC-002**: In acceptance testing, 100% of unsafe conditions caused by a safety-channel interruption or safety-channel mismatch are reported before the next transfer step is allowed to continue.
- **SC-003**: In scenario-based testing, users can correctly distinguish disabled, transfer-only, and always-enabled behavior in 100% of defined operating-mode scenarios.
- **SC-004**: In monitored verification runs, users can retrieve the full current light curtain status in a single request for at least 95% of checks.
