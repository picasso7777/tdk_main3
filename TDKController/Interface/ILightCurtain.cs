using System;

namespace TDKController
{
    /// <summary>
    /// Identifies the supported light curtain digital I/O channels.
    /// </summary>
    public enum LightCurtainIO
    {
        OSSD1 = 0,
        OSSD2 = 1,
        Reset = 2,
        Test = 3,
        Interlock = 4,
        LTCLed = 5,
    }

    /// <summary>
    /// Defines the runtime operating mode for the light curtain module.
    /// </summary>
    public enum LightCurtainType
    {
        Disable = 0,
        Enable_InTransfer = 1,
        Enable_Always = 2,
    }

    /// <summary>
    /// Describes the configured voltage polarity mode of the installed light curtain.
    /// This setting is configuration metadata and does not alter the OSSD safe/unsafe logic.
    /// </summary>
    public enum LightCurtainVoltageMode
    {
        Voltage24V = 0,
        Voltage0V = 1,
    }

    /// <summary>
    /// Provides event data when the light curtain transitions from a safe state to an unsafe state.
    /// </summary>
    public sealed class LightCurtainAlarmEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LightCurtainAlarmEventArgs"/> class.
        /// </summary>
        /// <param name="ossd1">Current OSSD1 value. True means safe.</param>
        /// <param name="ossd2">Current OSSD2 value. True means safe.</param>
        public LightCurtainAlarmEventArgs(bool ossd1, bool ossd2)
        {
            OSSD1 = ossd1;
            OSSD2 = ossd2;
        }

        /// <summary>
        /// Gets the OSSD1 channel value. True means safe.
        /// </summary>
        public bool OSSD1 { get; }

        /// <summary>
        /// Gets the OSSD2 channel value. True means safe.
        /// </summary>
        public bool OSSD2 { get; }
    }

    /// <summary>
    /// Provides the full logical light curtain state used both for notifications and snapshot queries.
    /// </summary>
    public sealed class LightCurtainStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LightCurtainStatusChangedEventArgs"/> class.
        /// </summary>
        public LightCurtainStatusChangedEventArgs(
            bool ossd1,
            bool ossd2,
            bool reset,
            bool test,
            bool interlock,
            bool ltcLed,
            LightCurtainVoltageMode lightCurtainVoltageMode,
            LightCurtainType lightCurtainType)
        {
            OSSD1 = ossd1;
            OSSD2 = ossd2;
            Reset = reset;
            Test = test;
            Interlock = interlock;
            LTCLed = ltcLed;
            LightCurtainVoltageMode = lightCurtainVoltageMode;
            LightCurtainType = lightCurtainType;
        }

        /// <summary>
        /// Gets the OSSD1 channel value. True means safe.
        /// </summary>
        public bool OSSD1 { get; }

        /// <summary>
        /// Gets the OSSD2 channel value. True means safe.
        /// </summary>
        public bool OSSD2 { get; }

        /// <summary>
        /// Gets the Reset output state.
        /// </summary>
        public bool Reset { get; }

        /// <summary>
        /// Gets the Test output state.
        /// </summary>
        public bool Test { get; }

        /// <summary>
        /// Gets the Interlock output state.
        /// </summary>
        public bool Interlock { get; }

        /// <summary>
        /// Gets the LTCLed output state.
        /// </summary>
        public bool LTCLed { get; }

        /// <summary>
        /// Gets the configured voltage mode.
        /// </summary>
        public LightCurtainVoltageMode LightCurtainVoltageMode { get; }

        /// <summary>
        /// Gets the current runtime operating mode.
        /// </summary>
        public LightCurtainType LightCurtainType { get; }
    }

    /// <summary>
    /// Defines the runtime contract for a self-contained light curtain controller.
    /// </summary>
    public interface ILightCurtain
    {
        #region Logical IO Status

        /// <summary>
        /// Gets the latest cached OSSD status array.
        /// Index 0 = OSSD1, index 1 = OSSD2.
        /// </summary>
        bool[] OSSD { get; }

        /// <summary>
        /// Gets the latest cached OSSD1 status. True means safe.
        /// </summary>
        bool OSSD1 { get; }

        /// <summary>
        /// Gets the latest cached OSSD2 status. True means safe.
        /// </summary>
        bool OSSD2 { get; }

        /// <summary>
        /// Gets the latest cached Reset output status.
        /// </summary>
        bool Reset { get; }

        /// <summary>
        /// Gets the latest cached Test output status.
        /// </summary>
        bool Test { get; }

        /// <summary>
        /// Gets the latest cached Interlock output status.
        /// </summary>
        bool Interlock { get; }

        /// <summary>
        /// Gets the latest cached LTCLed output status.
        /// </summary>
        bool LTCLed { get; }

        #endregion

        #region Configuration Properties

        /// <summary>
        /// Gets or sets the accepted light curtain configuration.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the assigned value is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the assigned configuration is invalid.</exception>
        LightCurtainConfig Config { get; set; }

        /// <summary>
        /// Gets the current runtime voltage mode.
        /// </summary>
        LightCurtainVoltageMode LightCurtainVoltageMode { get; }

        /// <summary>
        /// Gets the current runtime operating mode.
        /// </summary>
        LightCurtainType LightCurtainType { get; }

        #endregion

        #region Operations

        /// <summary>
        /// Sets the runtime operating mode.
        /// </summary>
        /// <param name="lightCurtainType">The target operating mode.</param>
        /// <returns>The operation result.</returns>
        ErrorCode SetLightCurtainType(LightCurtainType lightCurtainType);

        /// <summary>
        /// Gets the current runtime operating mode.
        /// </summary>
        /// <param name="lightCurtainType">The returned mode.</param>
        /// <returns>The operation result.</returns>
        ErrorCode GetLightCurtainType(out LightCurtainType lightCurtainType);

        /// <summary>
        /// Sets the runtime voltage mode.
        /// </summary>
        /// <param name="lightCurtainVoltageMode">The target voltage mode.</param>
        /// <returns>The operation result.</returns>
        ErrorCode SetVoltageMode(LightCurtainVoltageMode lightCurtainVoltageMode);

        /// <summary>
        /// Gets the current runtime voltage mode.
        /// </summary>
        /// <param name="lightCurtainVoltageMode">The returned voltage mode.</param>
        /// <returns>The operation result.</returns>
        ErrorCode GetVoltageMode(out LightCurtainVoltageMode lightCurtainVoltageMode);

        /// <summary>
        /// Reads both OSSD channels from hardware and updates the cached safety state.
        /// </summary>
        /// <param name="lTCTriggered">True when the current read result is unsafe; otherwise false.</param>
        /// <returns>The operation result.</returns>
        ErrorCode ReadLightCurtainOSSD(out bool lTCTriggered);

        /// <summary>
        /// Manually raises the current OSSD alarm event using cached OSSD values.
        /// </summary>
        /// <returns>The operation result.</returns>
        ErrorCode TriggerLightCurtainAlarm();

        /// <summary>
        /// Returns a full light curtain status snapshot using cached values only.
        /// </summary>
        /// <param name="status">The returned snapshot.</param>
        /// <returns>The operation result.</returns>
        ErrorCode GetLightCurtainStatus(out LightCurtainStatusChangedEventArgs status);

        /// <summary>
        /// Writes a digital output channel.
        /// </summary>
        /// <param name="io">The target output channel.</param>
        /// <param name="turnOn">True to write 1; false to write 0.</param>
        /// <returns>The operation result.</returns>
        ErrorCode SetLightCurtainDOStatus(LightCurtainIO io, bool turnOn);

        /// <summary>
        /// Reads a digital output channel.
        /// </summary>
        /// <param name="io">The target output channel.</param>
        /// <param name="turnOn">The returned output state.</param>
        /// <returns>The operation result.</returns>
        ErrorCode GetLightCurtainDOStatus(LightCurtainIO io, out bool turnOn);

        /// <summary>
        /// Reads a digital input channel.
        /// </summary>
        /// <param name="io">The target input channel.</param>
        /// <param name="value">The returned input state.</param>
        /// <returns>The operation result.</returns>
        ErrorCode GetLightCurtainDIStatus(LightCurtainIO io, out bool value);

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the light curtain transitions from safe to unsafe.
        /// </summary>
        event EventHandler<LightCurtainAlarmEventArgs> OSSDAlarmTriggered;

        /// <summary>
        /// Occurs when any cached signal or runtime mode value changes.
        /// </summary>
        event EventHandler<LightCurtainStatusChangedEventArgs> StatusChanged;

        #endregion
    }
}
