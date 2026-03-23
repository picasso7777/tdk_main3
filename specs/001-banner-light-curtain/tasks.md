# Tasks: Banner Light Curtain Support

**Input**: Design documents from `/specs/001-banner-light-curtain/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Included — spec FR-011 implies comprehensive validation; constitution mandates XML docs, English implementation comments, and ≥ 90% coverage for core logic.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Enums, structs, EventArgs, const int 錯誤碼定義 — 所有 user story 共享的型別基礎

- [ ] T001 [P] Define enums `LightCurtainIO`, `LightCurtainType`, `LightCurtainVoltageMode` and EventArgs classes (`LightCurtainAlarmEventArgs`, `LightCurtainStatusChangedEventArgs`) in `TDKController/Interface/ILightCurtain.cs`
- [ ] T002 [P] Define `DioChannelConfig` struct and expand `LightCurtainConfig` class with DO/DI mapping properties and config properties in `TDKController/Config/LightCurtainConfig.cs`
- [ ] T003 [P] Define LightCurtain `const int` error codes (`LightCurtainNotConfigured`, `LightCurtainDisabled`, `LightCurtainDioReadFailed`, `LightCurtainDioWriteFailed`, `LightCurtainInvalidChannel`, `LightCurtainUnsafeState`) in `TDKController/Interface/ErrorCode.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: `ILightCurtain` 完整介面定義與 `LightCurtain` 建構骨架 — 必須在任何 user story 實作前完成

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T004 Expand `ILightCurtain` interface with full member signatures (properties, methods, events, and `GetLightCurtainStatus(...)`) per contract in `TDKController/Interface/ILightCurtain.cs`
- [ ] T005 Implement `LightCurtain` constructor with `LightCurtainConfig`, `IOBoard[]`, `ILogUtility` injection, null-checks, field storage, and `#region` skeleton in `TDKController/Module/LightCurtain.cs`
- [ ] T006 Implement `LightCurtain` read-only IO status properties (`OSSD1`, `OSSD2`, `Reset`, `Test`, `Interlock`, `LTCLed`) and `Config` property with `UpdateConfig` private method (including FR-003 validation: required mapping completeness, `DioDeviceID` range check against `IOBoard[]` length, and duplicate channel checks across all DI/DO tuples) in `TDKController/Module/LightCurtain.cs`

**Checkpoint**: Foundation ready — `ILightCurtain` fully declared, `LightCurtain` constructable with valid config

---

## Phase 3: User Story 1 — Detect Unsafe Curtain States (Priority: P1) 🎯 MVP

**Goal**: Read OSSD safety inputs from DIO hardware, detect safe/unsafe transitions, and emit alarm events

**Independent Test**: Configure valid light curtain profile, mock safe/unsafe DI inputs, verify alarm fires on safe→unsafe transition and auto-clears on recovery

### Tests for User Story 1

- [ ] T007 [P] [US1] Create test class and constructor injection tests (null-check, valid construction) in `AutoTest/TDKController.Tests/Unit/LightCurtainTests.cs`
- [ ] T008 [P] [US1] Add `ReadLightCurtainOSSD` tests: both safe, OSSD1 unsafe, OSSD2 unsafe, both unsafe, mismatched state, DIO read failure, not configured returns `LightCurtainNotConfigured` in `AutoTest/TDKController.Tests/Unit/LightCurtainTests.cs`
- [ ] T009 [P] [US1] Add `OSSDAlarmTriggered` event tests: safe→unsafe fires alarm, unsafe→safe auto-clears without event, alarm includes correct OSSD values in `AutoTest/TDKController.Tests/Unit/LightCurtainTests.cs`
- [ ] T010 [P] [US1] Add `GetLightCurtainDIStatus` tests: OSSD1/OSSD2 success, DO enum returns `LightCurtainInvalidChannel`, not configured returns `LightCurtainNotConfigured`, DIO read failure returns `LightCurtainDioReadFailed` in `AutoTest/TDKController.Tests/Unit/LightCurtainTests.cs`

### Implementation for User Story 1

- [ ] T011 [US1] Implement `ReadLightCurtainOSSD()` — read both OSSD DI channels via `IOBoard[].GetInputBit()`, convert byte→bool, update properties, detect safe→unsafe transition and fire `OSSDAlarmTriggered` event, fire `StatusChanged` on any change in `TDKController/Module/LightCurtain.cs`
- [ ] T012 [US1] Implement `GetLightCurtainDIStatus()` — read single DI channel (OSSD1 or OSSD2) via `IOBoard[].GetInputBit()`, return `LightCurtainInvalidChannel` for DO enum values in `TDKController/Module/LightCurtain.cs`
- [ ] T013 [US1] Implement shared unsafe-state evaluation helper used by OSSD reads and output-operation guards in `TDKController/Module/LightCurtain.cs`

**Checkpoint**: OSSD safety detection fully functional — can read safety inputs, detect unsafe states, and emit alarms

---

## Phase 4: User Story 2 — Configure Operating Behavior (Priority: P2)

**Goal**: Allow callers to set/get operating mode (`LightCurtainType`) and voltage mode (`LightCurtainVoltageMode`), with status change notifications

**Independent Test**: Set operating mode and voltage mode, retrieve via getter, verify `StatusChanged` fires on change

### Tests for User Story 2

- [ ] T014 [P] [US2] Add `SetLightCurtainType` / `GetLightCurtainType` tests: set each mode, get returns correct value, `StatusChanged` fires on mode change in `AutoTest/TDKController.Tests/Unit/LightCurtainTests.cs`
- [ ] T015 [P] [US2] Add `SetVoltageMode` / `GetVoltageMode` tests: set each mode, get returns correct value, `StatusChanged` fires on mode change in `AutoTest/TDKController.Tests/Unit/LightCurtainTests.cs`
- [ ] T016 [P] [US2] Add `Config` property tests: config readback returns injected config, config update propagates, null config setter throws `ArgumentNullException`, invalid config setter throws `ArgumentException` in `AutoTest/TDKController.Tests/Unit/LightCurtainTests.cs`

### Implementation for User Story 2

- [ ] T017 [US2] Implement `SetLightCurtainType()` and `GetLightCurtainType()` — store mode, fire `StatusChanged` on change in `TDKController/Module/LightCurtain.cs`
- [ ] T018 [US2] Implement `SetVoltageMode()` and `GetVoltageMode()` — store mode, fire `StatusChanged` on change in `TDKController/Module/LightCurtain.cs`

**Checkpoint**: Configuration fully functional — modes can be set/retrieved with change notifications

---

## Phase 5: User Story 3 — Observe And Control Light Curtain Signals (Priority: P3)

**Goal**: Allow callers to set/get DO output signals and read full status

**Independent Test**: Request a full status snapshot, set a DO output, read it back, verify `StatusChanged` fires with updated state; attempt invalid operations (DI channel for DO write, disabled mode, unsafe state) and verify correct error codes

### Tests for User Story 3

- [ ] T019 [P] [US3] Add `SetLightCurtainDOStatus` tests: set each DO channel (Reset/Test/Interlock/LTCLed), invalid channel (OSSD1/OSSD2) returns `LightCurtainInvalidChannel`, not configured returns `LightCurtainNotConfigured`, disabled mode returns `LightCurtainDisabled`, unsafe state returns `LightCurtainUnsafeState`, DIO write failure returns `LightCurtainDioWriteFailed` in `AutoTest/TDKController.Tests/Unit/LightCurtainTests.cs`
- [ ] T020 [P] [US3] Add `GetLightCurtainDOStatus` tests: get each DO channel, invalid channel returns `LightCurtainInvalidChannel`, not configured returns `LightCurtainNotConfigured`, DIO read failure returns `LightCurtainDioReadFailed`, hardware value differs from cached value triggers `StatusChanged` in `AutoTest/TDKController.Tests/Unit/LightCurtainTests.cs`
- [ ] T021 [P] [US3] Add `GetLightCurtainStatus` tests: single response returns OSSD1, OSSD2, Reset, Test, Interlock, LTCLed, `LightCurtainType`, and `LightCurtainVoltageMode` in `AutoTest/TDKController.Tests/Unit/LightCurtainTests.cs`
- [ ] T022 [P] [US3] Add `StatusChanged` event comprehensive tests: fires on DO change, includes all current signal values and modes in `AutoTest/TDKController.Tests/Unit/LightCurtainTests.cs`

### Implementation for User Story 3

- [ ] T023 [US3] Implement `SetLightCurtainDOStatus()` — validate IO enum is DO channel, reject disabled/unconfigured/unsafe state, write via `IOBoard[].SetOutputBit()`, convert bool→byte, update property, fire `StatusChanged` in `TDKController/Module/LightCurtain.cs`
- [ ] T024 [US3] Implement `GetLightCurtainDOStatus()` — validate IO enum is DO channel, guard config-null, read via `IOBoard[].GetOutputBit()`, convert byte→bool, compare with cached property and fire `StatusChanged` if changed in `TDKController/Module/LightCurtain.cs`
- [ ] T025 [US3] Implement `GetLightCurtainStatus()` — return a single-response snapshot using `LightCurtainStatusChangedEventArgs` in `TDKController/Module/LightCurtain.cs`

**Checkpoint**: All user stories independently functional — full DIO read/write, safety detection, configuration, and status reporting

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, documentation alignment, region organization, edge case coverage, and coverage verification

- [ ] T026 Add edge case tests: operations before valid config, mode change while alarmed, missing mappings, duplicated DI/DO mappings, and mismatched OSSD states in `AutoTest/TDKController.Tests/Unit/LightCurtainTests.cs`
- [ ] T027 Add XML documentation comments to public API and English implementation comments to non-trivial logic in `TDKController/Interface/ILightCurtain.cs`, `TDKController/Config/LightCurtainConfig.cs`, and `TDKController/Module/LightCurtain.cs`
- [ ] T028 Verify and finalize `#region` organization in `TDKController/Module/LightCurtain.cs` — regions: Constants, Construction, IO Status Properties, Configuration & Mode, DI Operations, DO Operations, Status Snapshot, OSSD Safety Detection, Event Helpers
- [ ] T029 Run quickstart.md validation — build `TDKController.csproj`, build test project, run all LightCurtain tests, and collect coverage for `TDKController/Module/LightCurtain.cs`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — T001, T002, T003 can all start in parallel
- **Foundational (Phase 2)**: Depends on Setup completion — T004 depends on T001; T005, T006 depend on T001+T002+T003+T004
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User stories can proceed in priority order: P1 → P2 → P3
  - US2 and US3 are independent of US1 at the implementation level (all depend only on Foundational)
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) — no dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) — independent of US1
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) — independent of US1/US2

### Within Each User Story

- Tests written first → verify they fail
- Implementation tasks in dependency order
- Story complete before checkpoint validation

### Parallel Opportunities

Within Phase 1:
- T001, T002, T003 — all different files, fully parallel

Within Phase 3 (US1 tests):
- T007, T008, T009, T010 — all in same file but logically independent test groups

Within Phase 4 (US2 tests):
- T014, T015, T016 — all parallel

Within Phase 5 (US3 tests):
- T019, T020, T021, T022 — all parallel

---

## Parallel Example: Setup Phase

```
# All three tasks can run in parallel (different files):
T001: Define enums and EventArgs in ILightCurtain.cs
T002: Define DioChannelConfig and LightCurtainConfig in LightCurtainConfig.cs
T003: Define LightCurtain const int error codes in ErrorCode.cs
```

## Parallel Example: User Story 1 Tests

```
# All test tasks target same file but independent test classes/methods:
T007: Constructor tests
T008: ReadLightCurtainOSSD tests
T009: OSSDAlarmTriggered event tests
T010: GetLightCurtainDIStatus tests
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T003) — type definitions
2. Complete Phase 2: Foundational (T004–T006) — interface + constructor
3. Complete Phase 3: User Story 1 (T007–T013) — safety detection
4. **STOP and VALIDATE**: Test OSSD read, alarm trigger, auto-clear
5. Deployable safety detection module

### Incremental Delivery

1. Setup + Foundational → Types and skeleton ready
2. Add User Story 1 → Safety detection works → **MVP!**
3. Add User Story 2 → Mode/voltage configuration works
4. Add User Story 3 → Full DO control and single-response status snapshot
5. Polish → Edge cases, documentation, region cleanup, validation

### File Impact Summary

| File | Tasks | Operation |
|------|-------|-----------|
| `TDKController/Interface/ILightCurtain.cs` | T001, T004, T027 | Modify (expand stub and add XML docs) |
| `TDKController/Config/LightCurtainConfig.cs` | T002, T027 | Modify (expand stub and add comments) |
| `TDKController/Interface/ErrorCode.cs` | T003 | Modify (add const int entries) |
| `TDKController/Module/LightCurtain.cs` | T005, T006, T011–T013, T017–T018, T023–T025, T027–T028 | Modify (expand stub, add comments, finalize regions) |
| `AutoTest/TDKController.Tests/Unit/LightCurtainTests.cs` | T007–T010, T014–T016, T019–T022, T026 | Create (new file) |

---

## Notes

- All DIO methods use `byte` (0/1) — module converts `bool ↔ byte` internally
- `ReadLightCurtainOSSD()` reads physical DIO and updates OSSD properties; `GetLightCurtainStatus()` returns the latest single-response snapshot using the same state model as `StatusChanged`
- No internal polling thread — caller drives all reads
- DO channels total 4: Reset, Test, Interlock, LTCLed — where LTCLed is FR-001 所述的 status indicator
- All public DIO operation methods (ReadOSSD, GetDI, SetDO, GetDO, GetStatus) must guard with a config-null check at entry and return `LightCurtainNotConfigured` if `Config` has not been set. Mode/voltage setters do not require this guard.
- FR-007 defines the unsafe state condition and its auto-clear rule (no manual reset); FR-008 defines the alarm event firing on safe→unsafe transition — auto-clear in FR-008 means the alarm condition clears silently (no event fired on unsafe→safe)
- Changing `LightCurtainType` mode does not affect existing alarm state — alarm is driven solely by OSSD values
- Constructor parameter order follows T005: `LightCurtainConfig` first, then `IOBoard[]`, then `ILogUtility`
- Error codes in range -400..-406 are defined as `const int` per constitution allocation
- Single test file for all LightCurtain tests per constitution's test file consolidation rule
- Same-file tasks marked `[P]` (e.g., T007–T010, T014–T016, T019–T022) denote logical independence only — they must be written sequentially since they target the same file
