namespace TDKController
{
    /// <summary>
    /// Defines the board and bit mapping for a single logical light curtain signal.
    /// </summary>
    public struct DioChannelConfig
    {
        /// <summary>
        /// Gets or sets the injected DIO board array index.
        /// A negative value means the mapping is not configured.
        /// </summary>
        public int DioDeviceID { get; set; }

        /// <summary>
        /// Gets or sets the zero-based port index on the selected DIO board.
        /// </summary>
        public int PortID { get; set; }

        /// <summary>
        /// Gets or sets the zero-based bit index within the selected port.
        /// </summary>
        public int Channel_BitIndex { get; set; }
    }

    /// <summary>
    /// Stores the full Banner light curtain mapping and default runtime modes.
    /// </summary>
    public class LightCurtainConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LightCurtainConfig"/> class.
        /// All mappings start in an explicit unconfigured state using DioDeviceID = -1.
        /// </summary>
        public LightCurtainConfig()
        {
            LTC_DI_OSSD1 = CreateUnconfiguredChannel();
            LTC_DI_OSSD2 = CreateUnconfiguredChannel();
            LTC_DO_Reset = CreateUnconfiguredChannel();
            LTC_DO_Test = CreateUnconfiguredChannel();
            LTC_DO_Interlock = CreateUnconfiguredChannel();
            LTC_DO_LTCLed = CreateUnconfiguredChannel();
            LightCurtainType = LightCurtainType.Disable;
            LightCurtainVoltageMode = LightCurtainVoltageMode.Voltage24V;
        }

        /// <summary>
        /// Gets or sets the OSSD1 digital input mapping.
        /// </summary>
        public DioChannelConfig LTC_DI_OSSD1 { get; set; }

        /// <summary>
        /// Gets or sets the OSSD2 digital input mapping.
        /// </summary>
        public DioChannelConfig LTC_DI_OSSD2 { get; set; }

        /// <summary>
        /// Gets or sets the Reset digital output mapping.
        /// </summary>
        public DioChannelConfig LTC_DO_Reset { get; set; }

        /// <summary>
        /// Gets or sets the Test digital output mapping.
        /// </summary>
        public DioChannelConfig LTC_DO_Test { get; set; }

        /// <summary>
        /// Gets or sets the Interlock digital output mapping.
        /// </summary>
        public DioChannelConfig LTC_DO_Interlock { get; set; }

        /// <summary>
        /// Gets or sets the LTCLed digital output mapping.
        /// </summary>
        public DioChannelConfig LTC_DO_LTCLed { get; set; }

        /// <summary>
        /// Gets or sets the initial runtime operating mode.
        /// </summary>
        public LightCurtainType LightCurtainType { get; set; }

        /// <summary>
        /// Gets or sets the initial runtime voltage mode.
        /// </summary>
        public LightCurtainVoltageMode LightCurtainVoltageMode { get; set; }

        private static DioChannelConfig CreateUnconfiguredChannel()
        {
            return new DioChannelConfig
            {
                DioDeviceID = -1,
                PortID = -1,
                Channel_BitIndex = -1,
            };
        }
    }
}
