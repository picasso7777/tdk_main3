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
        private bool _isUnsafe;
        private LightCurtainType _lightCurtainType;
        private LightCurtainVoltageMode _lightCurtainVoltageMode;

        /// <summary>
        /// Initializes a new instance of the <see cref="LightCurtain"/> class.
        /// </summary>
        /// <param name="ioBoards">Injected DIO boards used by DioDeviceID mappings.</param>
        /// <param name="logger">Injected logger for diagnostics and exception reporting.</param>
        /// <exception cref="ArgumentNullException">Thrown when ioBoards or logger is null.</exception>
        public LightCurtain(IOBoard[] ioBoards, ILogUtility logger)
        {
            _ioBoards = ioBoards ?? throw new ArgumentNullException(nameof(ioBoards));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _lightCurtainType = LightCurtainType.Disable;
            _lightCurtainVoltageMode = LightCurtainVoltageMode.Voltage24V;
        }

        #endregion

        #region IO Status Properties

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
                if (!Enum.IsDefined(typeof(LightCurtainType), lightCurtainType))
                {
                    _logger.WriteLog(LogKey, LogHeadType.Error, string.Format("SetLightCurtainType: invalid mode {0}", lightCurtainType));
                    return ErrorCode.LightCurtainError;
                }

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
                if (!Enum.IsDefined(typeof(LightCurtainVoltageMode), lightCurtainVoltageMode))
                {
                    _logger.WriteLog(LogKey, LogHeadType.Error, string.Format("SetVoltageMode: invalid mode {0}", lightCurtainVoltageMode));
                    return ErrorCode.LightCurtainError;
                }

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
            // The incoming configuration is fully validated before it replaces the active one,
            // so the previous accepted configuration remains intact on any validation failure.
            ValidateConfig(config);

            bool shouldRaiseStatusChanged = _lightCurtainType != config.LightCurtainType
                || _lightCurtainVoltageMode != config.LightCurtainVoltageMode;

            _config = config;
            _lightCurtainType = config.LightCurtainType;
            _lightCurtainVoltageMode = config.LightCurtainVoltageMode;

            RaiseStatusChangedIfNeeded(shouldRaiseStatusChanged);
        }

        private void ValidateConfig(LightCurtainConfig config)
        {
            List<Tuple<string, DioChannelConfig, bool>> mappings = new List<Tuple<string, DioChannelConfig, bool>>
            {
                Tuple.Create("LTC_DI_OSSD1", config.LTC_DI_OSSD1, false),
                Tuple.Create("LTC_DI_OSSD2", config.LTC_DI_OSSD2, false),
                Tuple.Create("LTC_DO_Reset", config.LTC_DO_Reset, true),
                Tuple.Create("LTC_DO_Test", config.LTC_DO_Test, true),
                Tuple.Create("LTC_DO_Interlock", config.LTC_DO_Interlock, true),
                Tuple.Create("LTC_DO_LTCLed", config.LTC_DO_LTCLed, true),
            };

            HashSet<string> occupiedChannels = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < mappings.Count; index++)
            {
                string mappingName = mappings[index].Item1;
                DioChannelConfig channelConfig = mappings[index].Item2;
                bool isOutput = mappings[index].Item3;

                if (channelConfig.DioDeviceID < 0)
                {
                    throw new ArgumentException(string.Format("{0} is not configured.", mappingName), nameof(config));
                }

                if (channelConfig.DioDeviceID >= _ioBoards.Length)
                {
                    throw new ArgumentException(string.Format("{0} references out-of-range DioDeviceID {1}.", mappingName, channelConfig.DioDeviceID), nameof(config));
                }

                IOBoard ioBoard = _ioBoards[channelConfig.DioDeviceID];
                if (ioBoard == null)
                {
                    throw new ArgumentException(string.Format("{0} references a null DIO board at index {1}.", mappingName, channelConfig.DioDeviceID), nameof(config));
                }

                ValidateChannelRange(mappingName, channelConfig, ioBoard, isOutput);

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
        public ErrorCode ReadLightCurtainOSSD()
        {
            try
            {
                if (!IsConfigured())
                {
                    return ErrorCode.LightCurtainNotConfigured;
                }

                byte ossd1Value;
                byte ossd2Value;
                if (ReadInput(_config.LTC_DI_OSSD1, out ossd1Value) != ErrorCode.Success)
                {
                    return ErrorCode.LightCurtainDioReadFailed;
                }

                if (ReadInput(_config.LTC_DI_OSSD2, out ossd2Value) != ErrorCode.Success)
                {
                    return ErrorCode.LightCurtainDioReadFailed;
                }

                bool currentOssd1 = ConvertToBool(ossd1Value);
                bool currentOssd2 = ConvertToBool(ossd2Value);
                bool previousUnsafe = _isUnsafe;
                bool changed = UpdateCachedInputs(currentOssd1, currentOssd2);
                bool currentUnsafe = EvaluateUnsafeState(currentOssd1, currentOssd2);
                _isUnsafe = currentUnsafe;

                RaiseStatusChangedIfNeeded(changed);
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
        public ErrorCode GetLightCurtainDIStatus(LightCurtainIO io, out bool value)
        {
            try
            {
                value = false;
                if (!IsConfigured())
                {
                    return ErrorCode.LightCurtainNotConfigured;
                }

                DioChannelConfig channelConfig;
                if (!TryGetInputChannel(io, out channelConfig))
                {
                    return ErrorCode.LightCurtainInvalidChannel;
                }

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
                if (!IsConfigured())
                {
                    return ErrorCode.LightCurtainNotConfigured;
                }

                DioChannelConfig channelConfig;
                if (!TryGetOutputChannel(io, out channelConfig))
                {
                    return ErrorCode.LightCurtainInvalidChannel;
                }

                if (_lightCurtainType == LightCurtainType.Disable)
                {
                    return ErrorCode.LightCurtainDisabled;
                }

                if (EvaluateUnsafeState(_ossd1, _ossd2))
                {
                    return ErrorCode.LightCurtainUnsafeState;
                }

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

                DioChannelConfig channelConfig;
                if (!TryGetOutputChannel(io, out channelConfig))
                {
                    return ErrorCode.LightCurtainInvalidChannel;
                }

                byte rawValue;
                ErrorCode readResult = ReadOutput(channelConfig, out rawValue);
                if (readResult != ErrorCode.Success)
                {
                    return readResult;
                }

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

        private static bool UpdateBooleanField(ref bool target, bool value)
        {
            if (target == value)
            {
                return false;
            }

            target = value;
            return true;
        }

        private bool TryGetInputChannel(LightCurtainIO io, out DioChannelConfig channelConfig)
        {
            switch (io)
            {
                case LightCurtainIO.OSSD1:
                    channelConfig = _config.LTC_DI_OSSD1;
                    return true;
                case LightCurtainIO.OSSD2:
                    channelConfig = _config.LTC_DI_OSSD2;
                    return true;
                default:
                    channelConfig = default(DioChannelConfig);
                    return false;
            }
        }

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

        private void RaiseAlarmTriggered()
        {
            EventHandler<LightCurtainAlarmEventArgs> handler = OSSDAlarmTriggered;
            if (handler != null)
            {
                handler(this, new LightCurtainAlarmEventArgs(_ossd1, _ossd2));
            }
        }

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
