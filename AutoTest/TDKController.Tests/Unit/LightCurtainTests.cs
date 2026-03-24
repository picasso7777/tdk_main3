using System;
using System.Collections.Generic;
using DIO;
using Moq;
using NUnit.Framework;
using TDKLogUtility.Module;

namespace TDKController.Tests.Unit
{
    /// <summary>
    /// Unit tests for Banner light curtain behavior.
    /// </summary>
    [TestFixture]
    public class LightCurtainTests
    {
        private delegate int GetBitDelegate(int portIndex, int bitIndex, out byte value);
        private delegate int SetBitDelegate(int portIndex, int bitIndex, byte value);

        private Mock<ILogUtility> _logger;
        private Mock<IIOBoard>[] _boardMocks;
        private IIOBoard[] _ioBoards;
        private Dictionary<string, byte> _inputValues;
        private Dictionary<string, byte> _outputValues;
        private HashSet<string> _inputFailures;
        private HashSet<string> _outputReadFailures;
        private HashSet<string> _outputWriteFailures;
        private LightCurtain _sut;
        private LightCurtainConfig _validConfig;

        [SetUp]
        public void SetUp()
        {
            _logger = new Mock<ILogUtility>();
            _inputValues = new Dictionary<string, byte>(StringComparer.Ordinal);
            _outputValues = new Dictionary<string, byte>(StringComparer.Ordinal);
            _inputFailures = new HashSet<string>(StringComparer.Ordinal);
            _outputReadFailures = new HashSet<string>(StringComparer.Ordinal);
            _outputWriteFailures = new HashSet<string>(StringComparer.Ordinal);

            _boardMocks = new[]
            {
                CreateBoardMock(0),
                CreateBoardMock(1),
                CreateBoardMock(2),
            };
            _ioBoards = new IIOBoard[]
            {
                _boardMocks[0].Object,
                _boardMocks[1].Object,
                _boardMocks[2].Object,
            };

            _validConfig = CreateValidConfig();
            _sut = new LightCurtain(_ioBoards, _logger.Object);
        }

        #region Constructor Tests

        [Test]
        public void Constructor_NullIoBoards_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new LightCurtain(null, _logger.Object));
        }

        [Test]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new LightCurtain(_ioBoards, null));
        }

        [Test]
        public void Constructor_ValidDependencies_CreatesInstance()
        {
            Assert.DoesNotThrow(() => new LightCurtain(_ioBoards, _logger.Object));
        }

        #endregion

        #region OSSD Read Tests

        [Test]
        public void ReadLightCurtainOSSD_BothSafe_ReturnsSuccessAndUpdatesCachedState()
        {
            ConfigureSut(_validConfig);
            SetInputValue(_validConfig.LTC_DI_OSSD[0], true);
            SetInputValue(_validConfig.LTC_DI_OSSD[1], true);

            bool triggered;
            ErrorCode result = _sut.ReadLightCurtainOSSD(out triggered);

            Assert.AreEqual(ErrorCode.Success, result);
            Assert.IsFalse(triggered);
            Assert.IsTrue(_sut.OSSD[0]);
            Assert.IsTrue(_sut.OSSD[1]);
            Assert.IsTrue(_sut.OSSD1);
            Assert.IsTrue(_sut.OSSD2);
        }

        [Test]
        public void ReadLightCurtainOSSD_Ossd1Unsafe_ReturnsSuccessAndMarksUnsafe()
        {
            ConfigureSut(_validConfig);
            SetInputValue(_validConfig.LTC_DI_OSSD[0], false);
            SetInputValue(_validConfig.LTC_DI_OSSD[1], true);

            bool triggered;
            ErrorCode result = _sut.ReadLightCurtainOSSD(out triggered);

            Assert.AreEqual(ErrorCode.Success, result);
            Assert.IsTrue(triggered);
            Assert.IsFalse(_sut.OSSD1);
            Assert.IsTrue(_sut.OSSD2);
            Assert.AreEqual(ErrorCode.LightCurtainUnsafeState, _sut.SetLightCurtainDOStatus(LightCurtainIO.Reset, true));
        }

        [Test]
        public void ReadLightCurtainOSSD_Ossd2Unsafe_ReturnsSuccessAndMarksUnsafe()
        {
            ConfigureSut(_validConfig);
            SetInputValue(_validConfig.LTC_DI_OSSD[0], true);
            SetInputValue(_validConfig.LTC_DI_OSSD[1], false);

            bool triggered;
            ErrorCode result = _sut.ReadLightCurtainOSSD(out triggered);

            Assert.AreEqual(ErrorCode.Success, result);
            Assert.IsTrue(triggered);
            Assert.IsTrue(_sut.OSSD1);
            Assert.IsFalse(_sut.OSSD2);
            Assert.AreEqual(ErrorCode.LightCurtainUnsafeState, _sut.SetLightCurtainDOStatus(LightCurtainIO.Reset, true));
        }

        [Test]
        public void ReadLightCurtainOSSD_BothUnsafe_ReturnsSuccessAndUpdatesCachedState()
        {
            ConfigureSut(_validConfig);
            SetInputValue(_validConfig.LTC_DI_OSSD[0], false);
            SetInputValue(_validConfig.LTC_DI_OSSD[1], false);

            bool triggered;
            ErrorCode result = _sut.ReadLightCurtainOSSD(out triggered);

            Assert.AreEqual(ErrorCode.Success, result);
            Assert.IsTrue(triggered);
            Assert.IsFalse(_sut.OSSD1);
            Assert.IsFalse(_sut.OSSD2);
        }

        [Test]
        public void ReadLightCurtainOSSD_DioReadFailure_ReturnsReadFailedAndPreservesCachedState()
        {
            ConfigureSut(_validConfig);
            bool triggered;
            SetInputValue(_validConfig.LTC_DI_OSSD[0], true);
            SetInputValue(_validConfig.LTC_DI_OSSD[1], true);
            Assert.AreEqual(ErrorCode.Success, _sut.ReadLightCurtainOSSD(out triggered));
            MarkInputFailure(_validConfig.LTC_DI_OSSD[1]);
            SetInputValue(_validConfig.LTC_DI_OSSD[1], false);

            ErrorCode result = _sut.ReadLightCurtainOSSD(out triggered);

            Assert.AreEqual(ErrorCode.LightCurtainDioReadFailed, result);
            Assert.IsFalse(triggered);
            Assert.IsTrue(_sut.OSSD1);
            Assert.IsTrue(_sut.OSSD2);
        }

        [Test]
        public void ReadLightCurtainOSSD_NotConfigured_ReturnsNotConfigured()
        {
            bool triggered;
            ErrorCode result = _sut.ReadLightCurtainOSSD(out triggered);

            Assert.AreEqual(ErrorCode.LightCurtainNotConfigured, result);
            Assert.IsFalse(triggered);
        }

        [Test]
        public void ReadLightCurtainOSSD_SafeToUnsafe_FiresAlarmWithCurrentValues()
        {
            ConfigureSut(_validConfig);
            bool triggered;
            SetInputValue(_validConfig.LTC_DI_OSSD[0], true);
            SetInputValue(_validConfig.LTC_DI_OSSD[1], true);
            Assert.AreEqual(ErrorCode.Success, _sut.ReadLightCurtainOSSD(out triggered));

            LightCurtainAlarmEventArgs alarmArgs = null;
            int eventCount = 0;
            _sut.OSSDAlarmTriggered += delegate(object sender, LightCurtainAlarmEventArgs args)
            {
                eventCount++;
                alarmArgs = args;
            };

            SetInputValue(_validConfig.LTC_DI_OSSD[0], false);
            SetInputValue(_validConfig.LTC_DI_OSSD[1], true);
            ErrorCode result = _sut.ReadLightCurtainOSSD(out triggered);

            Assert.AreEqual(ErrorCode.Success, result);
            Assert.IsTrue(triggered);
            Assert.AreEqual(1, eventCount);
            Assert.NotNull(alarmArgs);
            Assert.IsFalse(alarmArgs.OSSD1);
            Assert.IsTrue(alarmArgs.OSSD2);
        }

        [Test]
        public void ReadLightCurtainOSSD_UnsafeToSafe_ClearsUnsafeWithoutFiringAdditionalAlarm()
        {
            ConfigureSut(_validConfig);
            Assert.AreEqual(ErrorCode.Success, _sut.SetLightCurtainType(LightCurtainType.Enable_Always));
            bool triggered;
            SetInputValue(_validConfig.LTC_DI_OSSD[0], true);
            SetInputValue(_validConfig.LTC_DI_OSSD[1], false);

            int eventCount = 0;
            _sut.OSSDAlarmTriggered += delegate { eventCount++; };
            Assert.AreEqual(ErrorCode.Success, _sut.ReadLightCurtainOSSD(out triggered));

            SetInputValue(_validConfig.LTC_DI_OSSD[0], true);
            SetInputValue(_validConfig.LTC_DI_OSSD[1], true);
            ErrorCode result = _sut.ReadLightCurtainOSSD(out triggered);

            Assert.AreEqual(ErrorCode.Success, result);
            Assert.IsFalse(triggered);
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(ErrorCode.Success, _sut.SetLightCurtainDOStatus(LightCurtainIO.Reset, true));
        }

        [Test]
        public void TriggerLightCurtainAlarm_Configured_RaisesAlarmWithCachedValues()
        {
            ConfigureSut(_validConfig);
            bool triggered;
            SetInputValue(_validConfig.LTC_DI_OSSD[0], false);
            SetInputValue(_validConfig.LTC_DI_OSSD[1], true);
            Assert.AreEqual(ErrorCode.Success, _sut.ReadLightCurtainOSSD(out triggered));

            LightCurtainAlarmEventArgs alarmArgs = null;
            int eventCount = 0;
            _sut.OSSDAlarmTriggered += delegate(object sender, LightCurtainAlarmEventArgs args)
            {
                eventCount++;
                alarmArgs = args;
            };

            ErrorCode result = _sut.TriggerLightCurtainAlarm();

            Assert.AreEqual(ErrorCode.Success, result);
            Assert.AreEqual(1, eventCount);
            Assert.NotNull(alarmArgs);
            Assert.IsFalse(alarmArgs.OSSD1);
            Assert.IsTrue(alarmArgs.OSSD2);
        }

        #endregion

        #region DI Status Tests

        [TestCase(LightCurtainIO.OSSD1, true)]
        [TestCase(LightCurtainIO.OSSD2, false)]
        public void GetLightCurtainDIStatus_OssdChannels_ReturnsCurrentValue(LightCurtainIO io, bool expectedValue)
        {
            ConfigureSut(_validConfig);
            SetInputValue(_validConfig.LTC_DI_OSSD[0], true);
            SetInputValue(_validConfig.LTC_DI_OSSD[1], false);

            bool value;
            ErrorCode result = _sut.GetLightCurtainDIStatus(io, out value);

            Assert.AreEqual(ErrorCode.Success, result);
            Assert.AreEqual(expectedValue, value);
        }

        [Test]
        public void GetLightCurtainDIStatus_DoChannel_ReturnsInvalidChannel()
        {
            ConfigureSut(_validConfig);

            bool value;
            ErrorCode result = _sut.GetLightCurtainDIStatus(LightCurtainIO.Reset, out value);

            Assert.AreEqual(ErrorCode.LightCurtainInvalidChannel, result);
        }

        [Test]
        public void GetLightCurtainDIStatus_NotConfigured_ReturnsNotConfigured()
        {
            bool value;
            ErrorCode result = _sut.GetLightCurtainDIStatus(LightCurtainIO.OSSD1, out value);

            Assert.AreEqual(ErrorCode.LightCurtainNotConfigured, result);
        }

        [Test]
        public void GetLightCurtainDIStatus_DioReadFailure_ReturnsReadFailed()
        {
            ConfigureSut(_validConfig);
            MarkInputFailure(_validConfig.LTC_DI_OSSD[0]);

            bool value;
            ErrorCode result = _sut.GetLightCurtainDIStatus(LightCurtainIO.OSSD1, out value);

            Assert.AreEqual(ErrorCode.LightCurtainDioReadFailed, result);
        }

        [Test]
        public void GetLightCurtainDIStatus_DisableMode_StillReturnsSuccess()
        {
            LightCurtainConfig disabledConfig = CreateValidConfig();
            disabledConfig.LightCurtainType = LightCurtainType.Disable;
            ConfigureSut(disabledConfig);
            SetInputValue(disabledConfig.LTC_DI_OSSD[0], true);

            bool value;
            ErrorCode result = _sut.GetLightCurtainDIStatus(LightCurtainIO.OSSD1, out value);

            Assert.AreEqual(ErrorCode.Success, result);
            Assert.IsTrue(value);
        }

        #endregion

        #region Mode And Config Tests

        [Test]
        public void SetLightCurtainType_TransitionsAcrossModes_RaisesStatusChanged()
        {
            int eventCount = 0;
            _sut.StatusChanged += delegate { eventCount++; };

            Assert.AreEqual(ErrorCode.Success, _sut.SetLightCurtainType(LightCurtainType.Enable_InTransfer));
            Assert.AreEqual(ErrorCode.Success, _sut.SetLightCurtainType(LightCurtainType.Enable_Always));
            Assert.AreEqual(ErrorCode.Success, _sut.SetLightCurtainType(LightCurtainType.Disable));

            LightCurtainType currentType;
            Assert.AreEqual(ErrorCode.Success, _sut.GetLightCurtainType(out currentType));
            Assert.AreEqual(LightCurtainType.Disable, currentType);
            Assert.AreEqual(3, eventCount);
        }

        [Test]
        public void SetVoltageMode_TransitionsAcrossModes_RaisesStatusChanged()
        {
            int eventCount = 0;
            _sut.StatusChanged += delegate { eventCount++; };

            Assert.AreEqual(ErrorCode.Success, _sut.SetVoltageMode(LightCurtainVoltageMode.Voltage0V));
            Assert.AreEqual(ErrorCode.Success, _sut.SetVoltageMode(LightCurtainVoltageMode.Voltage24V));

            LightCurtainVoltageMode currentMode;
            Assert.AreEqual(ErrorCode.Success, _sut.GetVoltageMode(out currentMode));
            Assert.AreEqual(LightCurtainVoltageMode.Voltage24V, currentMode);
            Assert.AreEqual(2, eventCount);
        }

        [Test]
        public void Config_Setter_ReadBackReturnsAcceptedConfig()
        {
            ConfigureSut(_validConfig);

            Assert.AreSame(_validConfig, _sut.Config);
        }

        [Test]
        public void Config_Setter_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _sut.Config = null);
        }

        [Test]
        public void Config_Setter_InvalidImmediatelyThrowsArgumentException()
        {
            LightCurtainConfig invalidConfig = CreateValidConfig();
            invalidConfig.LTC_DO_Test = invalidConfig.LTC_DO_Reset;

            Assert.Throws<ArgumentException>(() => _sut.Config = invalidConfig);
        }

        [Test]
        public void Config_Setter_InvalidRejected_DoesNotOverwritePreviousValidConfig()
        {
            ConfigureSut(_validConfig);
            LightCurtainConfig invalidConfig = CreateValidConfig();
            invalidConfig.LTC_DO_LTCLed = invalidConfig.LTC_DO_Reset;

            Assert.Throws<ArgumentException>(() => _sut.Config = invalidConfig);
            Assert.AreSame(_validConfig, _sut.Config);
        }

        [Test]
        public void Config_Setter_ModeChange_RaisesStatusChanged()
        {
            LightCurtainConfig config = CreateValidConfig();
            config.LightCurtainType = LightCurtainType.Enable_Always;
            config.LightCurtainVoltageMode = LightCurtainVoltageMode.Voltage0V;
            LightCurtainStatusChangedEventArgs lastArgs = null;
            int eventCount = 0;
            _sut.StatusChanged += delegate(object sender, LightCurtainStatusChangedEventArgs args)
            {
                eventCount++;
                lastArgs = args;
            };

            _sut.Config = config;

            Assert.AreEqual(1, eventCount);
            Assert.NotNull(lastArgs);
            Assert.AreEqual(LightCurtainType.Enable_Always, lastArgs.LightCurtainType);
            Assert.AreEqual(LightCurtainVoltageMode.Voltage0V, lastArgs.LightCurtainVoltageMode);
        }

        [Test]
        public void SetLightCurtainTypeAndVoltageMode_DoNotWriteBackToConfigObject()
        {
            LightCurtainConfig config = CreateValidConfig();
            config.LightCurtainType = LightCurtainType.Enable_Always;
            config.LightCurtainVoltageMode = LightCurtainVoltageMode.Voltage0V;
            ConfigureSut(config);

            Assert.AreEqual(ErrorCode.Success, _sut.SetLightCurtainType(LightCurtainType.Disable));
            Assert.AreEqual(ErrorCode.Success, _sut.SetVoltageMode(LightCurtainVoltageMode.Voltage24V));

            Assert.AreEqual(LightCurtainType.Enable_Always, config.LightCurtainType);
            Assert.AreEqual(LightCurtainVoltageMode.Voltage0V, config.LightCurtainVoltageMode);
            Assert.AreEqual(LightCurtainType.Disable, _sut.LightCurtainType);
            Assert.AreEqual(LightCurtainVoltageMode.Voltage24V, _sut.LightCurtainVoltageMode);
        }

        [Test]
        public void SetLightCurtainType_SameValue_DoesNotRaiseStatusChanged()
        {
            Assert.AreEqual(ErrorCode.Success, _sut.SetLightCurtainType(LightCurtainType.Enable_Always));
            int eventCount = 0;
            _sut.StatusChanged += delegate { eventCount++; };

            Assert.AreEqual(ErrorCode.Success, _sut.SetLightCurtainType(LightCurtainType.Enable_Always));

            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void SetVoltageMode_SameValue_DoesNotRaiseStatusChanged()
        {
            Assert.AreEqual(ErrorCode.Success, _sut.SetVoltageMode(LightCurtainVoltageMode.Voltage0V));
            int eventCount = 0;
            _sut.StatusChanged += delegate { eventCount++; };

            Assert.AreEqual(ErrorCode.Success, _sut.SetVoltageMode(LightCurtainVoltageMode.Voltage0V));

            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void Config_Setter_SameModes_DoesNotRaiseStatusChanged()
        {
            LightCurtainConfig config = CreateValidConfig();
            config.LightCurtainType = LightCurtainType.Disable;
            config.LightCurtainVoltageMode = LightCurtainVoltageMode.Voltage24V;
            int eventCount = 0;
            _sut.StatusChanged += delegate { eventCount++; };

            _sut.Config = config;

            Assert.AreEqual(0, eventCount);
        }

        #endregion

        #region DO And Snapshot Tests

        [TestCase(LightCurtainIO.Reset)]
        [TestCase(LightCurtainIO.Test)]
        [TestCase(LightCurtainIO.Interlock)]
        [TestCase(LightCurtainIO.LTCLed)]
        public void SetLightCurtainDOStatus_EachDoChannel_WritesHardwareAndRaisesStatusChanged(LightCurtainIO io)
        {
            ConfigureForSafeOutputOperations();
            int eventCount = 0;
            _sut.StatusChanged += delegate { eventCount++; };

            ErrorCode result = _sut.SetLightCurtainDOStatus(io, true);
            bool value;

            Assert.AreEqual(ErrorCode.Success, result);
            Assert.AreEqual(ErrorCode.Success, _sut.GetLightCurtainDOStatus(io, out value));
            Assert.IsTrue(value);
            Assert.AreEqual(1, eventCount);
        }

        [Test]
        public void SetLightCurtainDOStatus_InputChannel_ReturnsInvalidChannel()
        {
            ConfigureSut(_validConfig);

            ErrorCode result = _sut.SetLightCurtainDOStatus(LightCurtainIO.OSSD1, true);

            Assert.AreEqual(ErrorCode.LightCurtainInvalidChannel, result);
        }

        [Test]
        public void SetLightCurtainDOStatus_NotConfigured_ReturnsNotConfigured()
        {
            ErrorCode result = _sut.SetLightCurtainDOStatus(LightCurtainIO.Reset, true);

            Assert.AreEqual(ErrorCode.LightCurtainNotConfigured, result);
        }

        [Test]
        public void SetLightCurtainDOStatus_Disabled_ReturnsDisabled()
        {
            ConfigureSut(_validConfig);

            ErrorCode result = _sut.SetLightCurtainDOStatus(LightCurtainIO.Reset, true);

            Assert.AreEqual(ErrorCode.LightCurtainDisabled, result);
        }

        [Test]
        public void SetLightCurtainDOStatus_UnsafeState_ReturnsUnsafeState()
        {
            ConfigureSut(_validConfig);
            Assert.AreEqual(ErrorCode.Success, _sut.SetLightCurtainType(LightCurtainType.Enable_Always));
            bool triggered;
            SetInputValue(_validConfig.LTC_DI_OSSD[0], true);
            SetInputValue(_validConfig.LTC_DI_OSSD[1], false);
            Assert.AreEqual(ErrorCode.Success, _sut.ReadLightCurtainOSSD(out triggered));

            ErrorCode result = _sut.SetLightCurtainDOStatus(LightCurtainIO.Reset, true);

            Assert.AreEqual(ErrorCode.LightCurtainUnsafeState, result);
        }

        [Test]
        public void SetLightCurtainDOStatus_DioWriteFailure_ReturnsWriteFailed()
        {
            ConfigureForSafeOutputOperations();
            MarkOutputWriteFailure(_validConfig.LTC_DO_Reset);

            ErrorCode result = _sut.SetLightCurtainDOStatus(LightCurtainIO.Reset, true);

            Assert.AreEqual(ErrorCode.LightCurtainDioWriteFailed, result);
        }

        [TestCase(LightCurtainIO.Reset)]
        [TestCase(LightCurtainIO.Test)]
        [TestCase(LightCurtainIO.Interlock)]
        [TestCase(LightCurtainIO.LTCLed)]
        public void GetLightCurtainDOStatus_EachDoChannel_ReturnsHardwareValue(LightCurtainIO io)
        {
            ConfigureSut(_validConfig);
            SetOutputValue(GetDoConfig(io), true);

            bool value;
            ErrorCode result = _sut.GetLightCurtainDOStatus(io, out value);

            Assert.AreEqual(ErrorCode.Success, result);
            Assert.IsTrue(value);
        }

        [Test]
        public void GetLightCurtainDOStatus_InputChannel_ReturnsInvalidChannel()
        {
            ConfigureSut(_validConfig);

            bool value;
            ErrorCode result = _sut.GetLightCurtainDOStatus(LightCurtainIO.OSSD1, out value);

            Assert.AreEqual(ErrorCode.LightCurtainInvalidChannel, result);
        }

        [Test]
        public void GetLightCurtainDOStatus_NotConfigured_ReturnsNotConfigured()
        {
            bool value;
            ErrorCode result = _sut.GetLightCurtainDOStatus(LightCurtainIO.Reset, out value);

            Assert.AreEqual(ErrorCode.LightCurtainNotConfigured, result);
        }

        [Test]
        public void GetLightCurtainDOStatus_DioReadFailure_ReturnsReadFailed()
        {
            ConfigureSut(_validConfig);
            MarkOutputReadFailure(_validConfig.LTC_DO_Reset);

            bool value;
            ErrorCode result = _sut.GetLightCurtainDOStatus(LightCurtainIO.Reset, out value);

            Assert.AreEqual(ErrorCode.LightCurtainDioReadFailed, result);
        }

        [Test]
        public void GetLightCurtainDOStatus_HardwareDiffersFromCache_RaisesStatusChanged()
        {
            ConfigureForSafeOutputOperations();
            Assert.AreEqual(ErrorCode.Success, _sut.SetLightCurtainDOStatus(LightCurtainIO.Reset, true));
            SetOutputValue(_validConfig.LTC_DO_Reset, false);

            int eventCount = 0;
            _sut.StatusChanged += delegate { eventCount++; };
            bool value;
            ErrorCode result = _sut.GetLightCurtainDOStatus(LightCurtainIO.Reset, out value);

            Assert.AreEqual(ErrorCode.Success, result);
            Assert.IsFalse(value);
            Assert.AreEqual(1, eventCount);
        }

        [Test]
        public void GetLightCurtainStatus_ReturnsCompleteSnapshot()
        {
            ConfigureForSafeOutputOperations();
            Assert.AreEqual(ErrorCode.Success, _sut.SetLightCurtainDOStatus(LightCurtainIO.Reset, true));
            Assert.AreEqual(ErrorCode.Success, _sut.SetVoltageMode(LightCurtainVoltageMode.Voltage0V));

            LightCurtainStatusChangedEventArgs status;
            ErrorCode result = _sut.GetLightCurtainStatus(out status);

            Assert.AreEqual(ErrorCode.Success, result);
            Assert.NotNull(status);
            Assert.IsTrue(status.OSSD1);
            Assert.IsTrue(status.OSSD2);
            Assert.IsTrue(status.Reset);
            Assert.IsFalse(status.Test);
            Assert.IsFalse(status.Interlock);
            Assert.IsFalse(status.LTCLed);
            Assert.AreEqual(LightCurtainType.Enable_Always, status.LightCurtainType);
            Assert.AreEqual(LightCurtainVoltageMode.Voltage0V, status.LightCurtainVoltageMode);
        }

        [Test]
        public void GetLightCurtainStatus_NotConfigured_ReturnsNotConfigured()
        {
            LightCurtainStatusChangedEventArgs status;
            ErrorCode result = _sut.GetLightCurtainStatus(out status);

            Assert.AreEqual(ErrorCode.LightCurtainNotConfigured, result);
            Assert.IsNull(status);
        }

        [Test]
        public void StatusChanged_DoChange_IncludesAllCurrentValuesAndModes()
        {
            ConfigureForSafeOutputOperations();
            Assert.AreEqual(ErrorCode.Success, _sut.SetVoltageMode(LightCurtainVoltageMode.Voltage0V));

            LightCurtainStatusChangedEventArgs lastArgs = null;
            _sut.StatusChanged += delegate(object sender, LightCurtainStatusChangedEventArgs args)
            {
                lastArgs = args;
            };

            Assert.AreEqual(ErrorCode.Success, _sut.SetLightCurtainDOStatus(LightCurtainIO.Reset, true));

            Assert.NotNull(lastArgs);
            Assert.IsTrue(lastArgs.OSSD1);
            Assert.IsTrue(lastArgs.OSSD2);
            Assert.IsTrue(lastArgs.Reset);
            Assert.IsFalse(lastArgs.Test);
            Assert.IsFalse(lastArgs.Interlock);
            Assert.IsFalse(lastArgs.LTCLed);
            Assert.AreEqual(LightCurtainType.Enable_Always, lastArgs.LightCurtainType);
            Assert.AreEqual(LightCurtainVoltageMode.Voltage0V, lastArgs.LightCurtainVoltageMode);
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void Operations_BeforeValidConfig_ReturnExpectedErrors()
        {
            bool value;
            LightCurtainStatusChangedEventArgs status;
            bool triggered;

            Assert.AreEqual(ErrorCode.LightCurtainNotConfigured, _sut.ReadLightCurtainOSSD(out triggered));
            Assert.AreEqual(ErrorCode.LightCurtainNotConfigured, _sut.GetLightCurtainDIStatus(LightCurtainIO.OSSD1, out value));
            Assert.AreEqual(ErrorCode.LightCurtainNotConfigured, _sut.GetLightCurtainDOStatus(LightCurtainIO.Reset, out value));
            Assert.AreEqual(ErrorCode.LightCurtainNotConfigured, _sut.SetLightCurtainDOStatus(LightCurtainIO.Reset, true));
            Assert.AreEqual(ErrorCode.LightCurtainNotConfigured, _sut.GetLightCurtainStatus(out status));
        }

        [Test]
        public void SetLightCurtainType_WhileAlarmed_DoesNotClearUnsafeState()
        {
            ConfigureSut(_validConfig);
            Assert.AreEqual(ErrorCode.Success, _sut.SetLightCurtainType(LightCurtainType.Enable_Always));
            bool triggered;
            SetInputValue(_validConfig.LTC_DI_OSSD[0], false);
            SetInputValue(_validConfig.LTC_DI_OSSD[1], true);
            Assert.AreEqual(ErrorCode.Success, _sut.ReadLightCurtainOSSD(out triggered));

            ErrorCode modeResult = _sut.SetLightCurtainType(LightCurtainType.Enable_InTransfer);
            ErrorCode outputResult = _sut.SetLightCurtainDOStatus(LightCurtainIO.Reset, true);

            Assert.AreEqual(ErrorCode.Success, modeResult);
            Assert.AreEqual(ErrorCode.LightCurtainUnsafeState, outputResult);
            Assert.IsFalse(_sut.OSSD1);
            Assert.IsTrue(_sut.OSSD2);
        }

        [Test]
        public void Config_Setter_MissingMappings_ThrowsArgumentException()
        {
            LightCurtainConfig invalidConfig = new LightCurtainConfig();
            invalidConfig.LTC_DI_OSSD[0] = _validConfig.LTC_DI_OSSD[0];
            invalidConfig.LightCurtainType = LightCurtainType.Enable_Always;

            Assert.Throws<ArgumentException>(() => _sut.Config = invalidConfig);
        }

        [Test]
        public void Config_Setter_DuplicateMappings_ThrowsArgumentException()
        {
            LightCurtainConfig invalidConfig = CreateValidConfig();
            invalidConfig.LTC_DI_OSSD[1] = invalidConfig.LTC_DI_OSSD[0];

            Assert.Throws<ArgumentException>(() => _sut.Config = invalidConfig);
        }

        [Test]
        public void Config_Setter_NullBoardReference_ThrowsArgumentException()
        {
            IIOBoard[] boardsWithNull = new IIOBoard[]
            {
                _boardMocks[0].Object,
                null,
                _boardMocks[2].Object,
            };
            LightCurtain sut = new LightCurtain(boardsWithNull, _logger.Object);

            Assert.Throws<ArgumentException>(() => sut.Config = _validConfig);
        }

        #endregion

        #region Helpers

        private Mock<IIOBoard> CreateBoardMock(int boardIndex)
        {
            Mock<IIOBoard> boardMock = new Mock<IIOBoard>(MockBehavior.Strict);
            boardMock.SetupGet(x => x.InputPortCount).Returns(2);
            boardMock.SetupGet(x => x.InputBitsPerPort).Returns(8);
            boardMock.SetupGet(x => x.OutputPortCount).Returns(2);
            boardMock.SetupGet(x => x.OutputBitsPerPort).Returns(8);

            boardMock
                .Setup(x => x.GetInputBit(It.IsAny<int>(), It.IsAny<int>(), out It.Ref<byte>.IsAny))
                .Returns(new GetBitDelegate(delegate(int portIndex, int bitIndex, out byte value)
                {
                    string key = GetKey(boardIndex, portIndex, bitIndex);
                    if (_inputFailures.Contains(key))
                    {
                        value = 0;
                        return -1;
                    }

                    if (!_inputValues.TryGetValue(key, out value))
                    {
                        value = 0;
                    }

                    return 0;
                }));

            boardMock
                .Setup(x => x.GetOutputBit(It.IsAny<int>(), It.IsAny<int>(), out It.Ref<byte>.IsAny))
                .Returns(new GetBitDelegate(delegate(int portIndex, int bitIndex, out byte value)
                {
                    string key = GetKey(boardIndex, portIndex, bitIndex);
                    if (_outputReadFailures.Contains(key))
                    {
                        value = 0;
                        return -1;
                    }

                    if (!_outputValues.TryGetValue(key, out value))
                    {
                        value = 0;
                    }

                    return 0;
                }));

            boardMock
                .Setup(x => x.SetOutputBit(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<byte>()))
                .Returns(new SetBitDelegate(delegate(int portIndex, int bitIndex, byte value)
                {
                    string key = GetKey(boardIndex, portIndex, bitIndex);
                    if (_outputWriteFailures.Contains(key))
                    {
                        return -1;
                    }

                    _outputValues[key] = value;
                    return 0;
                }));

            return boardMock;
        }

        private LightCurtainConfig CreateValidConfig()
        {
            return new LightCurtainConfig
            {
                LTC_DI_OSSD = new[]
                {
                    new DioChannelConfig { DioDeviceID = 0, PortID = 0, Channel_BitIndex = 0 },
                    new DioChannelConfig { DioDeviceID = 0, PortID = 0, Channel_BitIndex = 1 },
                },
                LTC_DO_Reset = new DioChannelConfig { DioDeviceID = 1, PortID = 0, Channel_BitIndex = 0 },
                LTC_DO_Test = new DioChannelConfig { DioDeviceID = 1, PortID = 0, Channel_BitIndex = 1 },
                LTC_DO_Interlock = new DioChannelConfig { DioDeviceID = 1, PortID = 0, Channel_BitIndex = 2 },
                LTC_DO_LTCLed = new DioChannelConfig { DioDeviceID = 1, PortID = 0, Channel_BitIndex = 3 },
                LightCurtainType = LightCurtainType.Disable,
                LightCurtainVoltageMode = LightCurtainVoltageMode.Voltage24V,
            };
        }

        private void ConfigureSut(LightCurtainConfig config)
        {
            _sut.Config = config;
        }

        private void ConfigureForSafeOutputOperations()
        {
            LightCurtainConfig config = CreateValidConfig();
            config.LightCurtainType = LightCurtainType.Enable_Always;
            ConfigureSut(config);
            _validConfig = config;
            bool triggered;
            SetInputValue(config.LTC_DI_OSSD[0], true);
            SetInputValue(config.LTC_DI_OSSD[1], true);
            Assert.AreEqual(ErrorCode.Success, _sut.ReadLightCurtainOSSD(out triggered));
        }

        private void SetInputValue(DioChannelConfig channelConfig, bool value)
        {
            _inputValues[GetKey(channelConfig)] = value ? (byte)1 : (byte)0;
        }

        private void SetOutputValue(DioChannelConfig channelConfig, bool value)
        {
            _outputValues[GetKey(channelConfig)] = value ? (byte)1 : (byte)0;
        }

        private void MarkInputFailure(DioChannelConfig channelConfig)
        {
            _inputFailures.Add(GetKey(channelConfig));
        }

        private void MarkOutputReadFailure(DioChannelConfig channelConfig)
        {
            _outputReadFailures.Add(GetKey(channelConfig));
        }

        private void MarkOutputWriteFailure(DioChannelConfig channelConfig)
        {
            _outputWriteFailures.Add(GetKey(channelConfig));
        }

        private DioChannelConfig GetDoConfig(LightCurtainIO io)
        {
            switch (io)
            {
                case LightCurtainIO.Reset:
                    return _validConfig.LTC_DO_Reset;
                case LightCurtainIO.Test:
                    return _validConfig.LTC_DO_Test;
                case LightCurtainIO.Interlock:
                    return _validConfig.LTC_DO_Interlock;
                default:
                    return _validConfig.LTC_DO_LTCLed;
            }
        }

        private string GetKey(DioChannelConfig channelConfig)
        {
            return GetKey(channelConfig.DioDeviceID, channelConfig.PortID, channelConfig.BitIndex);
        }

        private static string GetKey(int dioDeviceId, int portId, int bitIndex)
        {
            return string.Format("{0}:{1}:{2}", dioDeviceId, portId, bitIndex);
        }

        #endregion
    }
}