using System;
using System.Collections.Generic;
using DIO;
using TDKLogUtility.Module;
using IOBoard = DIO.IIOBoard;

namespace TDKController
{
    /// <summary>
    /// Implements Banner light curtain DIO mapping, runtime state caching, and safety event behavior.
    /// The module is self-contained and does not depend on loadport workflow state.
    /// </summary>
    public class LightCurtain : ILightCurtain
    {
        #region Constants

        private const string LogKey = "LightCurtain";

        #endregion

        #region Construction

        private readonly IOBoard[] _ioBoards;
        private readonly ILogUtility _logger;
        private LightCurtainConfig _config;
        private bool _ossd1;
        private bool _ossd2;
        private bool _reset;
        private bool _test;
        private bool _interlock;
        private bool _ltcLed;
        // _isUnsafe tracks whether the most recent ReadLightCurtainOSSD(out bool lTCTriggered)
        // detected an unsafe condition.
        // It defaults to false even though _ossd1/_ossd2 also default to false (which would indicate unsafe).
        // This is intentional: before the first successful ReadLightCurtainOSSD(out bool lTCTriggered)
        // call, the module has never observed actual hardware state, so no safe-to-unsafe transition has
        // occurred. All DIO operation
        // methods are guarded by IsConfigured(), so this initial inconsistency cannot produce a safety gap.
        private bool _isUnsafe;
        private LightCurtainType _lightCurtainType;
        private LightCurtainVoltageMode _lightCurtainVoltageMode;

        /// <summary>
        /// Initializes a new instance of the <see cref="LightCurtain"/> class.
        /// </summary>
        /// <param name="ioBoards">Injected DIO boards used by DioDeviceID mappings.</param>
        /// <param name="config">Injected light curtain configuration applied during construction.</param>
        /// <param name="logger">Injected logger for diagnostics and exception reporting.</param>
        /// <exception cref="ArgumentNullException">Thrown when ioBoards, config, or logger is null.</exception>
        /// <exception cref="ArgumentException">Thrown when config is invalid.</exception>
        public LightCurtain(IOBoard[] ioBoards, LightCurtainConfig config, ILogUtility logger)
        {
            _ioBoards = ioBoards ?? throw new ArgumentNullException(nameof(ioBoards));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _lightCurtainType = LightCurtainType.Disable;
            _lightCurtainVoltageMode = LightCurtainVoltageMode.Voltage24V;
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            UpdateConfig(config);
        }

        #endregion

        #region IO Status Properties

        /// <inheritdoc />
        public bool[] OSSD
        {
            get { return new[] { _ossd1, _ossd2 }; }
        }

        /// <inheritdoc />
        public bool OSSD1
        {
            get { return _ossd1; }
        }

        /// <inheritdoc />
        public bool OSSD2
        {
            get { return _ossd2; }
        }

        /// <inheritdoc />
        public bool Reset
        {
            get { return _reset; }
        }

        /// <inheritdoc />
        public bool Test
        {
            get { return _test; }
        }

        /// <inheritdoc />
        public bool Interlock
        {
            get { return _interlock; }
        }

        /// <inheritdoc />
        public bool LTCLed
        {
            get { return _ltcLed; }
        }

        #endregion

        #region Configuration And Mode

        /// <inheritdoc />
        public LightCurtainConfig Config
        {
            get { return _config; }
            set
            {
                try
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException(nameof(value));
                    }

                    UpdateConfig(value);
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(LogKey, LogHeadType.Exception, string.Format("Config.setter: exception - {0}", ex.Message));
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public LightCurtainVoltageMode LightCurtainVoltageMode
        {
            get { return _lightCurtainVoltageMode; }
        }

        /// <inheritdoc />
        public LightCurtainType LightCurtainType
        {
            get { return _lightCurtainType; }
        }

        /// <inheritdoc />
        public ErrorCode SetLightCurtainType(LightCurtainType lightCurtainType)
        {
            try
            {
                // Reject undefined enum values to guard against invalid casts (e.g. (LightCurtainType)99).
                if (!Enum.IsDefined(typeof(LightCurtainType), lightCurtainType))
                {
                    _logger.WriteLog(LogKey, LogHeadType.Error, string.Format("SetLightCurtainType: invalid mode {0}", lightCurtainType));
                    return ErrorCode.LightCurtainError;
                }

                // Only fire StatusChanged when the value actually differs (same-value assignment is a no-op).
                bool changed = _lightCurtainType != lightCurtainType;
                _lightCurtainType = lightCurtainType;
                RaiseStatusChangedIfNeeded(changed);
                return ErrorCode.Success;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogKey, LogHeadType.Exception, string.Format("SetLightCurtainType: exception - {0}", ex.Message));
                throw;
            }
        }

        /// <inheritdoc />
        public ErrorCode GetLightCurtainType(out LightCurtainType lightCurtainType)
        {
            try
            {
                lightCurtainType = _lightCurtainType;
                return ErrorCode.Success;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogKey, LogHeadType.Exception, string.Format("GetLightCurtainType: exception - {0}", ex.Message));
                throw;
            }
        }

        /// <inheritdoc />
        public ErrorCode SetVoltageMode(LightCurtainVoltageMode lightCurtainVoltageMode)
        {
            try
            {
                // Reject undefined enum values to guard against invalid casts.
                if (!Enum.IsDefined(typeof(LightCurtainVoltageMode), lightCurtainVoltageMode))
                {
                    _logger.WriteLog(LogKey, LogHeadType.Error, string.Format("SetVoltageMode: invalid mode {0}", lightCurtainVoltageMode));
                    return ErrorCode.LightCurtainError;
                }

                // Only fire StatusChanged when the value actually differs (same-value assignment is a no-op).
                bool changed = _lightCurtainVoltageMode != lightCurtainVoltageMode;
                _lightCurtainVoltageMode = lightCurtainVoltageMode;
                RaiseStatusChangedIfNeeded(changed);
                return ErrorCode.Success;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogKey, LogHeadType.Exception, string.Format("SetVoltageMode: exception - {0}", ex.Message));
                throw;
            }
        }

        /// <inheritdoc />
        public ErrorCode GetVoltageMode(out LightCurtainVoltageMode lightCurtainVoltageMode)
        {
            try
            {
                lightCurtainVoltageMode = _lightCurtainVoltageMode;
                return ErrorCode.Success;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogKey, LogHeadType.Exception, string.Format("GetVoltageMode: exception - {0}", ex.Message));
                throw;
            }
        }

        private void UpdateConfig(LightCurtainConfig config)
        {
            // Step 1: Full validation up front — if any check fails, an ArgumentException propagates
            // and the previously accepted _config remains untouched (FR-003).
            ValidateConfig(config);

            // Step 2: Determine whether the incoming config carries different mode values.
            // Config setter performs one-way sync: config modes → module-level properties.
            // SetLightCurtainType/SetVoltageMode never write back to the Config object.
            bool shouldRaiseStatusChanged = _lightCurtainType != config.LightCurtainType
                || _lightCurtainVoltageMode != config.LightCurtainVoltageMode;

            // Step 3: Atomically replace config and sync module-level mode fields.
            _config = config;
            _lightCurtainType = config.LightCurtainType;
            _lightCurtainVoltageMode = config.LightCurtainVoltageMode;

            // Step 4: Notify subscribers only if mode values actually changed.
            RaiseStatusChangedIfNeeded(shouldRaiseStatusChanged);
        }

        private void ValidateConfig(LightCurtainConfig config)
        {
            // Build the ordered list of all 6 required DIO channel mappings (2 DI + 4 DO).
            // Item1 = human-readable name, Item2 = channel config, Item3 = true if output.
            List<Tuple<string, DioChannelConfig, bool>> mappings = new List<Tuple<string, DioChannelConfig, bool>>
            {
                Tuple.Create("LTC_DO_Reset", config.LTC_DO_Reset, true),
                Tuple.Create("LTC_DO_Test", config.LTC_DO_Test, true),
                Tuple.Create("LTC_DO_Interlock", config.LTC_DO_Interlock, true),
                Tuple.Create("LTC_DO_LTCLed", config.LTC_DO_LTCLed, true),
            };

            if (config.LTC_DI_OSSD == null || config.LTC_DI_OSSD.Length != 2)
            {
                throw new ArgumentException("LTC_DI_OSSD must contain exactly two mappings.", nameof(config));
            }

            for (int ossdIndex = 0; ossdIndex < config.LTC_DI_OSSD.Length; ossdIndex++)
            {
                mappings.Insert(
                    ossdIndex,
                    Tuple.Create(string.Format("LTC_DI_OSSD[{0}]", ossdIndex), config.LTC_DI_OSSD[ossdIndex], false));
            }

            // Track occupied channels using "DeviceID:PortID:BitIndex" composite keys
            // to detect cross-DI/DO duplicate mappings (FR-003).
            HashSet<string> occupiedChannels = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < mappings.Count; index++)
            {
                string mappingName = mappings[index].Item1;
                DioChannelConfig channelConfig = mappings[index].Item2;
                bool isOutput = mappings[index].Item3;

                // Check 1: DioDeviceID < 0 indicates an unconfigured sentinel (required mapping missing).
                if (channelConfig.DioDeviceID < 0)
                {
                    throw new ArgumentException(string.Format("{0} is not configured.", mappingName), nameof(config));
                }

                // Check 2: DioDeviceID must be within the injected IOBoard[] array bounds.
                if (channelConfig.DioDeviceID >= _ioBoards.Length)
                {
                    throw new ArgumentException(string.Format("{0} references out-of-range DioDeviceID {1}.", mappingName, channelConfig.DioDeviceID), nameof(config));
                }

                // Check 3: The referenced IOBoard slot must not be null.
                IOBoard ioBoard = _ioBoards[channelConfig.DioDeviceID];
                if (ioBoard == null)
                {
                    throw new ArgumentException(string.Format("{0} references a null DIO board at index {1}.", mappingName, channelConfig.DioDeviceID), nameof(config));
                }

                // Check 4: Port and bit indices must be within the board's declared capacity.
                ValidateChannelRange(mappingName, channelConfig, ioBoard, isOutput);

                // Check 5: No two signals may share the same physical DIO channel.
                string channelKey = string.Format(
                    "{0}:{1}:{2}",
                    channelConfig.DioDeviceID,
                    channelConfig.PortID,
                    channelConfig.Channel_BitIndex);
                if (!occupiedChannels.Add(channelKey))
                {
                    throw new ArgumentException(string.Format("{0} duplicates an existing DIO mapping ({1}).", mappingName, channelKey), nameof(config));
                }
            }
        }

        // Validates that port and bit indices fall within the board's declared I/O capacity.
        // Uses output-side or input-side counts depending on the isOutput flag.
        private static void ValidateChannelRange(string mappingName, DioChannelConfig channelConfig, IOBoard ioBoard, bool isOutput)
        {
            if (channelConfig.PortID < 0 || channelConfig.Channel_BitIndex < 0)
            {
                throw new ArgumentException(string.Format("{0} contains a negative port or bit index.", mappingName));
            }

            int portCount = isOutput ? ioBoard.OutputPortCount : ioBoard.InputPortCount;
            int bitCount = isOutput ? ioBoard.OutputBitsPerPort : ioBoard.InputBitsPerPort;
            if (channelConfig.PortID >= portCount)
            {
                throw new ArgumentException(string.Format("{0} references out-of-range port {1}.", mappingName, channelConfig.PortID));
            }

            if (channelConfig.Channel_BitIndex >= bitCount)
            {
                throw new ArgumentException(string.Format("{0} references out-of-range bit {1}.", mappingName, channelConfig.Channel_BitIndex));
            }
        }

        #endregion

        #region DI Operations

        /// <inheritdoc />
        public ErrorCode ReadLightCurtainOSSD(out bool lTCTriggered)
        {
            try
            {
                lTCTriggered = false;
                if (!IsConfigured())
                {
                    return ErrorCode.LightCurtainNotConfigured;
                }

                // Step 1: Read both OSSD safety channels from DIO hardware.
                // If either read fails, abort immediately without updating any cached state (fail-fast).
                byte ossd1Value;
                byte ossd2Value;
                if (ReadInput(_config.LTC_DI_OSSD[0], out ossd1Value) != ErrorCode.Success)
                {
                    return ErrorCode.LightCurtainDioReadFailed;
                }

                if (ReadInput(_config.LTC_DI_OSSD[1], out ossd2Value) != ErrorCode.Success)
                {
                    return ErrorCode.LightCurtainDioReadFailed;
                }

                // Step 2: Convert raw byte values (0/1) to boolean (true = safe).
                bool currentOssd1 = ConvertToBool(ossd1Value);
                bool currentOssd2 = ConvertToBool(ossd2Value);

                // Step 3: Capture prior unsafe flag before updating, then refresh cached OSSD properties.
                bool previousUnsafe = _isUnsafe;
                bool changed = UpdateCachedInputs(currentOssd1, currentOssd2);

                // Step 4: Evaluate current unsafe state — unsafe if either channel is false (FR-007).
                bool currentUnsafe = EvaluateUnsafeState(currentOssd1, currentOssd2);
                _isUnsafe = currentUnsafe;
                lTCTriggered = currentUnsafe;

                // Step 5: Fire StatusChanged if any cached value changed.
                RaiseStatusChangedIfNeeded(changed);

                // Step 6: Fire alarm only on safe-to-unsafe transition (FR-008).
                // Unsafe-to-safe recovery clears silently (auto-clear, no event).
                if (!previousUnsafe && currentUnsafe)
                {
                    RaiseAlarmTriggered();
                }

                return ErrorCode.Success;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogKey, LogHeadType.Exception, string.Format("ReadLightCurtainOSSD: exception - {0}", ex.Message));
                throw;
            }
        }

        /// <inheritdoc />
        public ErrorCode TriggerLightCurtainAlarm()
        {
            try
            {
                if (!IsConfigured())
                {
                    return ErrorCode.LightCurtainNotConfigured;
                }

                RaiseAlarmTriggered();
                return ErrorCode.Success;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogKey, LogHeadType.Exception, string.Format("TriggerLightCurtainAlarm: exception - {0}", ex.Message));
                throw;
            }
        }

        /// <inheritdoc />
        public ErrorCode GetLightCurtainDIStatus(LightCurtainIO io, out bool value)
        {
            try
            {
                value = false;
                if (!IsConfigured())
                {
                    return ErrorCode.LightCurtainNotConfigured;
                }

                // Validate that io is a DI channel (OSSD1/OSSD2); reject DO enum values.
                DioChannelConfig channelConfig;
                if (!TryGetInputChannel(io, out channelConfig))
                {
                    return ErrorCode.LightCurtainInvalidChannel;
                }

                // Read from hardware. DI reads are allowed in all modes including Disable (FR-010)
                // to preserve diagnostic availability.
                byte rawValue;
                ErrorCode readResult = ReadInput(channelConfig, out rawValue);
                if (readResult != ErrorCode.Success)
                {
                    return readResult;
                }

                value = ConvertToBool(rawValue);
                return ErrorCode.Success;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogKey, LogHeadType.Exception, string.Format("GetLightCurtainDIStatus: exception - {0}", ex.Message));
                throw;
            }
        }

        #endregion

        #region DO Operations

        /// <inheritdoc />
        public ErrorCode SetLightCurtainDOStatus(LightCurtainIO io, bool turnOn)
        {
            try
            {
                // Guard 1 of 3: Module must have an accepted configuration.
                if (!IsConfigured())
                {
                    return ErrorCode.LightCurtainNotConfigured;
                }

                // Validate that io is a DO channel (Reset/Test/Interlock/LTCLed).
                DioChannelConfig channelConfig;
                if (!TryGetOutputChannel(io, out channelConfig))
                {
                    return ErrorCode.LightCurtainInvalidChannel;
                }

                // Guard 2 of 3: Feature must not be in Disable mode (FR-010).
                if (_lightCurtainType == LightCurtainType.Disable)
                {
                    return ErrorCode.LightCurtainDisabled;
                }

                // Guard 3 of 3: Light curtain must be in a safe state (FR-010).
                if (EvaluateUnsafeState(_ossd1, _ossd2))
                {
                    return ErrorCode.LightCurtainUnsafeState;
                }

                // All guards passed — write the bit to hardware, then update local cache.
                ErrorCode writeResult = WriteOutput(channelConfig, turnOn);
                if (writeResult != ErrorCode.Success)
                {
                    return writeResult;
                }

                bool changed = UpdateCachedOutput(io, turnOn);
                RaiseStatusChangedIfNeeded(changed);
                return ErrorCode.Success;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogKey, LogHeadType.Exception, string.Format("SetLightCurtainDOStatus: exception - {0}", ex.Message));
                throw;
            }
        }

        /// <inheritdoc />
        public ErrorCode GetLightCurtainDOStatus(LightCurtainIO io, out bool turnOn)
        {
            try
            {
                turnOn = false;
                if (!IsConfigured())
                {
                    return ErrorCode.LightCurtainNotConfigured;
                }

                // Validate that io is a DO channel; DI channels are rejected.
                DioChannelConfig channelConfig;
                if (!TryGetOutputChannel(io, out channelConfig))
                {
                    return ErrorCode.LightCurtainInvalidChannel;
                }

                // DO reads are not gated by mode or safety state (FR-010) to preserve diagnostics.
                byte rawValue;
                ErrorCode readResult = ReadOutput(channelConfig, out rawValue);
                if (readResult != ErrorCode.Success)
                {
                    return readResult;
                }

                // If hardware value differs from local cache, update and fire StatusChanged (FR-009).
                turnOn = ConvertToBool(rawValue);
                bool changed = UpdateCachedOutput(io, turnOn);
                RaiseStatusChangedIfNeeded(changed);
                return ErrorCode.Success;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogKey, LogHeadType.Exception, string.Format("GetLightCurtainDOStatus: exception - {0}", ex.Message));
                throw;
            }
        }

        #endregion

        #region Status Snapshot

        /// <inheritdoc />
        public ErrorCode GetLightCurtainStatus(out LightCurtainStatusChangedEventArgs status)
        {
            try
            {
                status = null;
                if (!IsConfigured())
                {
                    return ErrorCode.LightCurtainNotConfigured;
                }

                // Return a snapshot built from cached values only — no additional hardware I/O (FR-006).
                status = CreateStatusSnapshot();
                return ErrorCode.Success;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogKey, LogHeadType.Exception, string.Format("GetLightCurtainStatus: exception - {0}", ex.Message));
                throw;
            }
        }

        #endregion

        #region OSSD Safety Detection

        /// <inheritdoc />
        public event EventHandler<LightCurtainAlarmEventArgs> OSSDAlarmTriggered;

        /// <inheritdoc />
        public event EventHandler<LightCurtainStatusChangedEventArgs> StatusChanged;

        // Unsafe when either safety channel is broken or when channels disagree (FR-007).
        private static bool EvaluateUnsafeState(bool ossd1, bool ossd2)
        {
            return !ossd1 || !ossd2;
        }

        private bool UpdateCachedInputs(bool ossd1, bool ossd2)
        {
            bool changed = false;
            if (_ossd1 != ossd1)
            {
                _ossd1 = ossd1;
                changed = true;
            }

            if (_ossd2 != ossd2)
            {
                _ossd2 = ossd2;
                changed = true;
            }

            return changed;
        }

        #endregion

        #region Event Helpers And DIO Access

        private bool IsConfigured()
        {
            return _config != null;
        }

        // Reads a single DI bit from the IOBoard specified by the channel config.
        // Returns Success if the hardware read succeeds; LightCurtainDioReadFailed otherwise.
        private ErrorCode ReadInput(DioChannelConfig channelConfig, out byte value)
        {
            value = 0;
            int result = _ioBoards[channelConfig.DioDeviceID].GetInputBit(channelConfig.PortID, channelConfig.Channel_BitIndex, out value);
            if (result != 0)
            {
                _logger.WriteLog(LogKey, LogHeadType.Error, string.Format("ReadInput: failed for board={0}, port={1}, bit={2}, result={3}", channelConfig.DioDeviceID, channelConfig.PortID, channelConfig.Channel_BitIndex, result));
                return ErrorCode.LightCurtainDioReadFailed;
            }

            return ErrorCode.Success;
        }

        // Reads a single DO bit from the IOBoard specified by the channel config.
        private ErrorCode ReadOutput(DioChannelConfig channelConfig, out byte value)
        {
            value = 0;
            int result = _ioBoards[channelConfig.DioDeviceID].GetOutputBit(channelConfig.PortID, channelConfig.Channel_BitIndex, out value);
            if (result != 0)
            {
                _logger.WriteLog(LogKey, LogHeadType.Error, string.Format("ReadOutput: failed for board={0}, port={1}, bit={2}, result={3}", channelConfig.DioDeviceID, channelConfig.PortID, channelConfig.Channel_BitIndex, result));
                return ErrorCode.LightCurtainDioReadFailed;
            }

            return ErrorCode.Success;
        }

        // Writes a single DO bit to the IOBoard. Converts bool to byte (1/0) internally.
        private ErrorCode WriteOutput(DioChannelConfig channelConfig, bool turnOn)
        {
            byte rawValue = ConvertToByte(turnOn);
            int result = _ioBoards[channelConfig.DioDeviceID].SetOutputBit(channelConfig.PortID, channelConfig.Channel_BitIndex, rawValue);
            if (result != 0)
            {
                _logger.WriteLog(LogKey, LogHeadType.Error, string.Format("WriteOutput: failed for board={0}, port={1}, bit={2}, value={3}, result={4}", channelConfig.DioDeviceID, channelConfig.PortID, channelConfig.Channel_BitIndex, rawValue, result));
                return ErrorCode.LightCurtainDioWriteFailed;
            }

            return ErrorCode.Success;
        }

        private static byte ConvertToByte(bool value)
        {
            return value ? (byte)1 : (byte)0;
        }

        private static bool ConvertToBool(byte value)
        {
            return value != 0;
        }

        // Dispatches a cached DO property update by IO enum. Returns true if the value changed.
        private bool UpdateCachedOutput(LightCurtainIO io, bool value)
        {
            switch (io)
            {
                case LightCurtainIO.Reset:
                    return UpdateBooleanField(ref _reset, value);
                case LightCurtainIO.Test:
                    return UpdateBooleanField(ref _test, value);
                case LightCurtainIO.Interlock:
                    return UpdateBooleanField(ref _interlock, value);
                case LightCurtainIO.LTCLed:
                    return UpdateBooleanField(ref _ltcLed, value);
                default:
                    return false;
            }
        }

        // Atomically compares and updates a boolean field. Returns true if the value changed.
        private static bool UpdateBooleanField(ref bool target, bool value)
        {
            if (target == value)
            {
                return false;
            }

            target = value;
            return true;
        }

        // Maps a LightCurtainIO enum to the corresponding DI channel config.
        // Returns false for DO enum values (OSSD1/OSSD2 are the only valid DI channels).
        private bool TryGetInputChannel(LightCurtainIO io, out DioChannelConfig channelConfig)
        {
            switch (io)
            {
                case LightCurtainIO.OSSD1:
                    channelConfig = _config.LTC_DI_OSSD[0];
                    return true;
                case LightCurtainIO.OSSD2:
                    channelConfig = _config.LTC_DI_OSSD[1];
                    return true;
                default:
                    channelConfig = default(DioChannelConfig);
                    return false;
            }
        }

        // Maps a LightCurtainIO enum to the corresponding DO channel config.
        // Returns false for DI enum values (Reset/Test/Interlock/LTCLed are the only valid DO channels).
        private bool TryGetOutputChannel(LightCurtainIO io, out DioChannelConfig channelConfig)
        {
            switch (io)
            {
                case LightCurtainIO.Reset:
                    channelConfig = _config.LTC_DO_Reset;
                    return true;
                case LightCurtainIO.Test:
                    channelConfig = _config.LTC_DO_Test;
                    return true;
                case LightCurtainIO.Interlock:
                    channelConfig = _config.LTC_DO_Interlock;
                    return true;
                case LightCurtainIO.LTCLed:
                    channelConfig = _config.LTC_DO_LTCLed;
                    return true;
                default:
                    channelConfig = default(DioChannelConfig);
                    return false;
            }
        }

        // Builds a full status snapshot from cached values. Shared by GetLightCurtainStatus
        // and StatusChanged event to maintain the same data shape (FR-006).
        private LightCurtainStatusChangedEventArgs CreateStatusSnapshot()
        {
            return new LightCurtainStatusChangedEventArgs(
                _ossd1,
                _ossd2,
                _reset,
                _test,
                _interlock,
                _ltcLed,
                _lightCurtainVoltageMode,
                _lightCurtainType);
        }

        // Fires OSSDAlarmTriggered with current OSSD values. Uses a local handler copy
        // to avoid race conditions if a subscriber unsubscribes during invocation.
        private void RaiseAlarmTriggered()
        {
            EventHandler<LightCurtainAlarmEventArgs> handler = OSSDAlarmTriggered;
            if (handler != null)
            {
                handler(this, new LightCurtainAlarmEventArgs(_ossd1, _ossd2));
            }
        }

        // Fires StatusChanged only when a value actually changed (FR-009).
        // Uses a local handler copy to prevent race conditions on concurrent unsubscribe.
        private void RaiseStatusChangedIfNeeded(bool changed)
        {
            if (!changed)
            {
                return;
            }

            EventHandler<LightCurtainStatusChangedEventArgs> handler = StatusChanged;
            if (handler != null)
            {
                handler(this, CreateStatusSnapshot());
            }
        }

        #endregion
    }
}
