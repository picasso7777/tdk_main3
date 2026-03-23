using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Text;
using Communication.Interface;
using TDKLogUtility.Module;

namespace TDKController
{
    /// <summary>
    /// Parsed TAS300 GET:STATE response (20-character status string to 18 fields).
    /// </summary>
    public struct LoadportStatus
    {
        /// <summary>
        /// Initializes a new LoadportStatus with default field values.
        /// Required because C# 7.3 does not support struct field initializers.
        /// </summary>
        public LoadportStatus(bool initialize)
        {
            EqpStatus = '0';
            Mode = '0';
            Inited = '0';
            OpStatus = '0';
            Ecode = 0;
            FpPlace = '0';
            FpClamp = '?';
            LtchKey = '?';
            Vacuum = '0';
            FpDoor = '?';
            WfProtrusion = '0';
            ZPos = '?';
            YPos = '?';
            MpArmPos = '?';
            MpZPos = '?';
            MpStopper = '?';
            MappingStatus = '0';
            IntKey = '0';
            InfoPad = '0';
        }

        /// <summary>Position 1: Equipment status ('0'=normal, 'A'=recoverable error, 'E'=fatal error).</summary>
        public char EqpStatus { get; internal set; }

        /// <summary>Position 2: Mode ('0'=online).</summary>
        public char Mode { get; internal set; }

        /// <summary>Position 3: Initialized flag ('0'=not init, '1'=initialized).</summary>
        public char Inited { get; internal set; }

        /// <summary>Position 4: Operation status ('0'=stopped, '1'=operating).</summary>
        public char OpStatus { get; internal set; }

        /// <summary>Position 5-6: Error code (2 ASCII hex chars, e.g. "1A" = 0x1A).</summary>
        public byte Ecode { get; internal set; }

        /// <summary>Position 7: FOUP placement ('0'=absent, '1'=placed, '2'=misplaced).</summary>
        public char FpPlace { get; internal set; }

        /// <summary>Position 8: FOUP clamp ('0'=open, '1'=clamped, '?'=undefined).</summary>
        public char FpClamp { get; internal set; }

        /// <summary>Position 9: Latch key ('0'=open, '1'=close, '?'=undefined).</summary>
        public char LtchKey { get; internal set; }

        /// <summary>Position 10: Vacuum ('0'=off, '1'=on).</summary>
        public char Vacuum { get; internal set; }

        /// <summary>Position 11: FOUP door ('0'=open, '1'=close, '?'=undefined).</summary>
        public char FpDoor { get; internal set; }

        /// <summary>Position 12: Wafer protrusion sensor ('0'=blocked, '1'=unblocked).</summary>
        public char WfProtrusion { get; internal set; }

        /// <summary>
        /// Position 13: Z-axis position ('0'=up, '1'=down, '2'=Start Position, '3'=End Position, '?'=undefined).
        /// </summary>
        public char ZPos { get; internal set; }

        /// <summary>Position 14: Y-axis position ('0'=undock, '1'=dock, '?'=undefined).</summary>
        public char YPos { get; internal set; }

        /// <summary>Position 15: Map arm position ('0'=open, '1'=close, '?'=undefined).</summary>
        public char MpArmPos { get; internal set; }

        /// <summary>Position 16: Map Z position ('0'=retract, '1'=mapping, '?'=undefined).</summary>
        public char MpZPos { get; internal set; }

        /// <summary>Position 17: Map stopper ('0'=on, '1'=off, '?'=undefined).</summary>
        public char MpStopper { get; internal set; }

        /// <summary>Position 18: Mapping status ('0'=unmapped, '1'=mapped, '?'=map failed).</summary>
        public char MappingStatus { get; internal set; }

        /// <summary>Position 19: Interlock key ('0'=enable, '1'-'3'=disable).</summary>
        public char IntKey { get; internal set; }

        /// <summary>Position 20: Info pad ('0'=no input, '1'=A-pin on, '2'=B-pin on, '3'=both on).</summary>
        public char InfoPad { get; internal set; }

        /// <summary>
        /// Creates a new LoadportStatus with all default values initialized.
        /// </summary>
        public static LoadportStatus CreateDefault()
        {
            return new LoadportStatus(true);
        }
    }

    /// <summary>
    /// Executes TAS300 loadport commands and maintains a small in-memory state model for the device.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is the runtime command actor used by higher-level application code to communicate with a TAS300
    /// loadport through an <see cref="IConnector"/> implementation.
    /// </para>
    /// <para>
    /// Its responsibilities include sending commands, waiting for the expected handshake sequence, caching the most
    /// recent device state, and raising change notifications only when observable values actually change.
    /// </para>
    /// </remarks>
    public class LoadportActor : ILoadPortActor
    {
        #region Constants

        // === FOUP Status Constants ===
        private const int FPS_UNKNOWN = -1;
        private const int FPS_NOFOUP = 0;
        private const int FPS_PLACED = 1;
        private const int FPS_CLAMPED = 2;
        private const int FPS_DOCKED = 3;
        private const int FPS_OPENED = 4;

        // === FOUP Event Constants ===
        private const int FPEVT_NONE = 0xFF;
        private const int FPEVT_PODOF = 0;
        private const int FPEVT_SMTON = 1;
        private const int FPEVT_ABNST = 2;
        private const int FPEVT_PODON = 3;

        // === FXL AMHS State Constants ===
        private const int FXL_NOTINIT = 0;
        private const int FXL_READY = 1;
        private const int FXL_BUSY = 2;
        private const int FXL_AMHS = 3;

        // === Response Type Constants ===
        private const int RES_ACK = 0;
        private const int RES_NAK = 1;
        private const int RES_INF = 2;
        private const int RES_ABS = 3;

        // === TAS300 Operation Commands (MOV) — two-phase handshake ===
        private const string CMD_ORGSH = "MOV:ORGSH";
        private const string CMD_ABORG = "MOV:ABORG";
        private const string CMD_CLOAD = "MOV:CLOAD";
        private const string CMD_PODCL = "MOV:PODCL";
        private const string CMD_CLDYD = "MOV:CLDYD";
        private const string CMD_CULFC = "MOV:CULFC";
        private const string CMD_CULYD = "MOV:CULYD";
        private const string CMD_PODOP = "MOV:PODOP";
        private const string CMD_MAPDO = "MOV:MAPDO";

        // === TAS300 Operation Commands (SET) — two-phase handshake ===
        private const string CMD_RESET = "SET:RESET";
        private const string CMD_INITL = "SET:INITL";

        // === TAS300 Quick Commands (GET/EVT) — ACK only ===
        private const string CMD_STATE = "GET:STATE";
        private const string CMD_MAPRD = "GET:MAPRD";
        private const string CMD_EVTON = "EVT:EVTON";
        private const string CMD_EVTOF = "EVT:EVTOF";
        private const string CMD_FPEON = "EVT:FPEON";
        private const string CMD_FPEOF = "EVT:FPEOF";
        private const string CMD_FIN_PREFIX = "FIN:";

        // === LED Command Prefixes (SET) — Operation Commands ===
        private const string CMD_LED_ON_PREFIX = "SET:LON";
        private const string CMD_LED_OFF_PREFIX = "SET:LOF";
        private const string CMD_LED_BLINK_PREFIX = "SET:LBL";

        // === LED Action Constants ===
        private const int LED_OFF = 0;
        private const int LED_ON = 1;
        private const int LED_BLINK = 2;
        private const int LED_COUNT = 13;

        // === Slot Map ===
        private const int SLOT_COUNT = 25;

        // === FOUP Event Response Identifiers ===
        private const string FOUP_EVT_PODOF = "PODOF";
        private const string FOUP_EVT_SMTON = "SMTON";
        private const string FOUP_EVT_ABNST = "ABNST";
        private const string FOUP_EVT_PODON = "PODON";

        // === statfxl code lookup table ===
        private static readonly Dictionary<int, string> StatfxlCodes = new Dictionary<int, string>
        {
            { FPS_NOFOUP,  "0x28" },
            { FPS_PLACED,  "0x69" },
            { FPS_CLAMPED, "0x59" },
            { FPS_DOCKED,  "0x53" },
            { FPS_OPENED,  "0x57" },
        };
        private const string STATFXL_MISPLACED = "0x68";

        // === Logging key ===
        private const string LOG_KEY = "LoadportActor";

        #endregion

        #region Injected Dependencies

        /// <summary>
        /// Configuration parameters for TAS300 communication timeouts.
        /// </summary>
        public LoadportActorConfig Config { get; set; }
        private readonly ILogUtility _logger;

        #endregion

        #region IConnector Property Setter Pattern

        private IConnector _connector;

        /// <summary>
        /// Communication channel to TAS300.
        /// Uses property setter pattern: unsubscribes from old connector,
        /// subscribes to new connector's DataReceived event.
        /// Declared in ILoadPortActor so external consumers can replace
        /// the connector at runtime without knowing the concrete type.
        /// </summary>
        public IConnector Connector
        {
            get { return _connector; }
            set
            {
                ThrowIfDisposed();

                if (_connector != null)
                {
                    _connector.DataReceived -= OnDataReceived;
                    _connector.ConnectedSuccess -= MouduleSuccessCheck;
                }

                _connector = value;

                if (_connector != null)
                {
                    _connector.DataReceived += OnDataReceived;
                    _connector.ConnectedSuccess += MouduleSuccessCheck;
                }
            }
        }


        /// <summary>
        /// Gets or sets a flag indicating whether the module has been verified as reachable and responsive.
        /// </summary>
        /// <remarks>
        /// The value is updated by connection-related callbacks and reflects application-level verification rather than
        /// a persistent state stored inside TAS300.
        /// </remarks>
        public bool ModuleInitialize { get; set; }
        #endregion

        #region State Machine Fields

        // FXL AMHS state — Interlocked operations
        private int _fxlState = FXL_NOTINIT;

        // FOUP mechanical status — volatile for thread-safe reads
        private volatile int _fpStatus = FPS_UNKNOWN;

        // Most recent FOUP event — volatile for thread-safe reads
        private volatile int _fpEvent = FPEVT_NONE;

        #endregion

        #region Internal Fields

        // Equipment status cache (GET:STATE response)
        private LoadportStatus _status = LoadportStatus.CreateDefault();

        // Synchronization signals for two-phase handshake
        private readonly ManualResetEventSlim _ackSignal = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim _infSignal = new ManualResetEventSlim(false);

        // Last response type from TAS300 (ACK/NAK/INF/ABS)
        private volatile int _lastResponseType;

        // Last response data payload from TAS300
        private volatile string _lastResponseData;

        // Cached SEMI format SlotMap string
        private string _cachedSlotMap;

        // Cached SEMI format int array
        private int[] _cachedSlotMapArray;

        // LED status cache: index 0-12 corresponds to LED 1-13
        private readonly int[] _ledStatus = new int[LED_COUNT];

        // Dispose idempotent flag
        private int _disposed = 0;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of <see cref="LoadportActor"/>.
        /// </summary>
        /// <param name="config">Configuration parameters for TAS300 communication timeouts.</param>
        /// <param name="connector">Communication channel to TAS300 hardware.</param>
        /// <param name="logger">Logging utility for diagnostic output.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="config"/>, <paramref name="connector"/>,
        /// or <paramref name="logger"/> is null.
        /// </exception>
        public LoadportActor(LoadportActorConfig config, IConnector connector, ILogUtility logger)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (connector == null) throw new ArgumentNullException(nameof(connector));
            Connector = connector;
            Connector.ConnectedSuccess += MouduleSuccessCheck;
            Connector.ConnectedSuccess += Connector_ConnectedSuccess;
        }

        #endregion

        #region ILoadPortActor Properties

        /// <summary>
        /// Gets the most recently cached slot-map string in SEMI-format text form.
        /// </summary>
        public string SlotMap
        {
            get { return _cachedSlotMap; }
        }

        /// <summary>
        /// Gets the most recently parsed raw loadport status snapshot.
        /// </summary>
        public LoadportStatus Status
        {
            get { return _status; }
        }

        /// <summary>
        /// Gets the currently derived high-level FOUP mechanical status.
        /// </summary>
        public int FoupStatus
        {
            get { return _fpStatus; }
        }

        /// <summary>
        /// Gets the most recent FOUP event code observed from asynchronous TAS300 event traffic.
        /// </summary>
        public int FoupEvent
        {
            get { return _fpEvent; }
        }

        #endregion

        #region ILoadPortActor Events

        /// <summary>
        /// Raised when a successful LED operation changes the cached state of an LED.
        /// </summary>
        public event LedChangedEventHandler LedChanged;

        /// <summary>
        /// Raised when a completed slot-map scan produces a new cached slot-map result.
        /// </summary>
        public event SlotMapScannedEventHandler SlotMapScanned;

        /// <summary>
        /// Raised when TAS300 reports a FOUP-related event frame such as <c>PODON</c> or <c>PODOF</c>.
        /// </summary>
        public event FoupReportStartedEventHandler FoupReportStarted;

        /// <summary>
        /// Raised when a newly parsed <c>GET:STATE</c> payload differs from the previously cached loadport status.
        /// </summary>
        public event StatusChangedEventHandler StatusChanged;

        /// <summary>
        /// Raised when the derived FOUP mechanical status transitions to a different value.
        /// </summary>
        public event FoupStatusChangedEventHandler FoupStatusChanged;

        #endregion

        #region Initialization Commands

        /// <summary>
        /// Handles a successful connector-level connection notification and verifies the TAS300 session.
        /// </summary>
        /// <remarks>
        /// Establishing the transport link alone does not guarantee that the loadport is ready to accept
        /// commands. This method performs a lightweight functional check by issuing <c>GET:STATE</c> through
        /// <see cref="GetFxlAmhsStatus"/> and updates <see cref="ModuleInitialize"/> from the result.
        /// </remarks>
        private void Connector_ConnectedSuccess()
        {
            try
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Info, "Connector_ConnectedSuccess: verifying TAS300 with GET:STATE");
                ModuleInitialize = GetFxlAmhsStatus() == ErrorCode.Success;
            }
            catch (Exception ex)
            {
                ModuleInitialize = false;

                _logger.WriteLog(LOG_KEY, LogHeadType.Exception,
                    string.Format("Connector_ConnectedSuccess: exception - {0}", ex.Message));
                throw;
            }
            
        }
        /// <summary>
        /// Executes the normal homing sequence and refreshes the cached device state on success.
        /// </summary>
        /// <returns>The result of the TAS300 <c>MOV:ORGSH</c> operation.</returns>
        public ErrorCode Init()
        {
            try
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Info, "Init: sending MOV:ORGSH");
                var result = SendMovSetCommand(CMD_ORGSH);
                if (result == ErrorCode.Success)
                {
                    GetFxlAmhsStatus();
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("Init: exception - {0}", ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Executes the forced homing sequence and refreshes the cached device state on success.
        /// </summary>
        /// <returns>The result of the TAS300 <c>MOV:ABORG</c> operation.</returns>
        public ErrorCode InitForce()
        {
            try
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Info, "InitForce: sending MOV:ABORG");
                var result = SendMovSetCommand(CMD_ABORG);
                if (result == ErrorCode.Success)
                {
                    GetFxlAmhsStatus();
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("InitForce: exception - {0}", ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Executes the TAS300 program initialization sequence and refreshes the cached device state on success.
        /// </summary>
        /// <returns>The result of the TAS300 <c>SET:INITL</c> operation.</returns>
        public ErrorCode InitProgram()
        {
            try
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Info, "InitProgram: sending SET:INITL");
                var result = SendMovSetCommand(CMD_INITL);
                if (result == ErrorCode.Success)
                {
                    GetFxlAmhsStatus();
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("InitProgram: exception - {0}", ex.Message));
                throw;
            }
        }

        #endregion

        #region Load Commands

        /// <summary>
        /// Moves the loadport into the load-ready sequence and refreshes cached status on success.
        /// </summary>
        /// <returns>The result of the TAS300 <c>MOV:CLOAD</c> operation.</returns>
        public ErrorCode Load()
        {
            try
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Info, "Load: sending MOV:CLOAD");
                var result = SendMovSetCommand(CMD_CLOAD);
                if (result == ErrorCode.Success)
                {
                    GetFxlAmhsStatus();
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("Load: exception - {0}", ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Clamps the FOUP and refreshes cached status on success.
        /// </summary>
        /// <returns>The result of the TAS300 <c>MOV:PODCL</c> operation.</returns>
        public ErrorCode Clamp()
        {
            try
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Info, "Clamp: sending MOV:PODCL");
                var result = SendMovSetCommand(CMD_PODCL);
                if (result == ErrorCode.Success)
                {
                    GetFxlAmhsStatus();
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("Clamp: exception - {0}", ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Docks the FOUP and refreshes cached status on success.
        /// </summary>
        /// <returns>The result of the TAS300 <c>MOV:CLDYD</c> operation.</returns>
        public ErrorCode Dock()
        {
            try
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Info, "Dock: sending MOV:CLDYD");
                var result = SendMovSetCommand(CMD_CLDYD);
                if (result == ErrorCode.Success)
                {
                    GetFxlAmhsStatus();
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("Dock: exception - {0}", ex.Message));
                throw;
            }
        }

        #endregion

        #region Unload Commands

        /// <summary>
        /// Starts the unload sequence using the currently cached FOUP status to choose the proper TAS300 command.
        /// </summary>
        /// <remarks>
        /// When the FOUP is still clamped, the actor sends <c>MOV:PODOP</c>; otherwise it falls back to
        /// <c>MOV:ABORG</c>.
        /// </remarks>
        /// <returns>The result of the selected unload-related operation.</returns>
        public ErrorCode Unload()
        {
            try
            {
                // FOUP status determines which command to send
                string cmd = (_fpStatus == FPS_CLAMPED) ? CMD_PODOP : CMD_ABORG;
                _logger.WriteLog(LOG_KEY, LogHeadType.Info, string.Format("Unload: FoupStatus={0}, sending {1}", _fpStatus, cmd));
                var result = SendMovSetCommand(cmd);
                if (result == ErrorCode.Success)
                {
                    GetFxlAmhsStatus();
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("Unload: exception - {0}", ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Moves the loadport to the undock position and refreshes cached status on success.
        /// </summary>
        /// <returns>The result of the TAS300 <c>MOV:CULFC</c> operation.</returns>
        public ErrorCode Undock()
        {
            try
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Info, "Undock: sending MOV:CULFC");
                var result = SendMovSetCommand(CMD_CULFC);
                if (result == ErrorCode.Success)
                {
                    GetFxlAmhsStatus();
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("Undock: exception - {0}", ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Closes the FOUP door path and refreshes cached status on success.
        /// </summary>
        /// <returns>The result of the TAS300 <c>MOV:CULYD</c> operation.</returns>
        public ErrorCode CloseDoor()
        {
            try
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Info, "CloseDoor: sending MOV:CULYD");
                var result = SendMovSetCommand(CMD_CULYD);
                if (result == ErrorCode.Success)
                {
                    GetFxlAmhsStatus();
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("CloseDoor: exception - {0}", ex.Message));
                throw;
            }
        }

        #endregion

        #region Error Recovery

        /// <summary>
        /// Sends the TAS300 reset command to clear recoverable loadport errors.
        /// </summary>
        /// <returns>The result of the TAS300 <c>SET:RESET</c> operation.</returns>
        public ErrorCode ResetError()
        {
            try
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Info, "ResetError: sending SET:RESET");
                return SendMovSetCommand(CMD_RESET);
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("ResetError: exception - {0}", ex.Message));
                throw;
            }
        }

        #endregion

        #region Status Query Commands

        /// <summary>
        /// Queries <c>GET:STATE</c>, updates the cached status model, and returns the raw response payload.
        /// </summary>
        /// <param name="data">The raw status payload returned by TAS300 when the query succeeds.</param>
        /// <returns>The result of the status query.</returns>
        public ErrorCode GetLPStatus(out string data)
        {
            try
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Info, "GetLPStatus: sending GET:STATE");
                var result = GetFxlAmhsStatus();
                if (result == ErrorCode.Success)
                {
                    data = _lastResponseData ?? string.Empty;
                }
                else
                {
                    data = string.Empty;
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("GetLPStatus: exception - {0}", ex.Message));
                data = string.Empty;
                throw;
            }
        }

        /// <summary>
        /// Returns the cached LED state for a specific LED index without querying the device.
        /// </summary>
        /// <param name="data">The cached LED state encoded as a string.</param>
        /// <param name="ledNo">The one-based LED index.</param>
        /// <returns><see cref="ErrorCode.Success"/> for valid cache access.</returns>
        public ErrorCode GetLedStatus(out string data, int ledNo)
        {
            try
            {
                ThrowIfDisposed();

                if (ledNo < 1 || ledNo > LED_COUNT)
                {
                    data = string.Empty;
                    return ErrorCode.Success;
                }
                data = _ledStatus[ledNo - 1].ToString();
                return ErrorCode.Success;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("GetLedStatus: exception - {0}", ex.Message));
                data = string.Empty;
                throw;
            }
        }

        /// <summary>
        /// Queries device status and translates the current FOUP-related state into the legacy <c>statfxl</c> code.
        /// </summary>
        /// <param name="statfxl">The translated status code string when the query succeeds.</param>
        /// <returns>The result of the underlying status query.</returns>
        public ErrorCode GetFOUPStatus(out string statfxl)
        {
            try
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Info, "GetFOUPStatus: querying TAS300 status");
                var result = GetFxlAmhsStatus();
                if (result == ErrorCode.Success)
                {
                    statfxl = GetStatfxlCode(_fpStatus, _status.FpPlace);
                }
                else
                {
                    statfxl = string.Empty;
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("GetFOUPStatus: exception - {0}", ex.Message));
                statfxl = string.Empty;
                throw;
            }
        }

        #endregion

        #region LED Control Commands

        /// <summary>
        /// Turns on the specified LED.
        /// </summary>
        /// <param name="ledNo">The one-based LED index.</param>
        /// <returns>The result of the underlying LED command.</returns>
        public ErrorCode LedOn(int ledNo)
        {
            try
            {
                return LampOP(ledNo, LED_ON);
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("LedOn: exception - {0}", ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Sets the specified LED to blink mode.
        /// </summary>
        /// <param name="ledNo">The one-based LED index.</param>
        /// <returns>The result of the underlying LED command.</returns>
        public ErrorCode LedBlink(int ledNo)
        {
            try
            {
                return LampOP(ledNo, LED_BLINK);
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("LedBlink: exception - {0}", ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Turns off the specified LED.
        /// </summary>
        /// <param name="ledNo">The one-based LED index.</param>
        /// <returns>The result of the underlying LED command.</returns>
        public ErrorCode LedOff(int ledNo)
        {
            try
            {
                return LampOP(ledNo, LED_OFF);
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("LedOff: exception - {0}", ex.Message));
                throw;
            }
        }

        #endregion

        #region Slot Map Commands

        /// <summary>
        /// Executes a fresh slot-map scan, converts the result to SEMI format, and updates the slot-map cache.
        /// </summary>
        /// <param name="data">The space-separated SEMI-format slot-map string.</param>
        /// <returns>The result of the combined mapping and readback sequence.</returns>
        public ErrorCode ScanSlotMapStatus(out string data)
        {
            try
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Info, "ScanSlotMapStatus: sending MOV:MAPDO");

                // Phase 1: Execute mapping operation
                var result = SendMovSetCommand(CMD_MAPDO);
                if (result != ErrorCode.Success)
                {
                    data = string.Empty;
                    return result;
                }

                // Phase 2: Read mapping results
                _logger.WriteLog(LOG_KEY, LogHeadType.Info, "ScanSlotMapStatus: sending GET:MAPRD");
                result = SendAckOnlyCommand(CMD_MAPRD);
                if (result != ErrorCode.Success)
                {
                    data = string.Empty;
                    return ErrorCode.CommandFailed;
                }


                // Convert raw map to SEMI format
                string rawMap = _lastResponseData ?? string.Empty;
                var slotMapArray = ConvertRawToSemiArray(rawMap);
                bool slotMapChanged = !SlotMapEquals(_cachedSlotMapArray, slotMapArray);
                _cachedSlotMapArray = slotMapArray;
                _cachedSlotMap = FormatSlotMapString(slotMapArray);
                data = _cachedSlotMap;

                // Raise SlotMapScanned event only when the cached map actually changes.
                if (slotMapChanged)
                {
                    RaiseSlotMapScanned(slotMapArray);
                }

                return ErrorCode.Success;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("ScanSlotMapStatus: exception - {0}", ex.Message));
                data = string.Empty;
                throw;
            }
        }

        /// <summary>
        /// Returns the most recently cached slot-map string without querying the device again.
        /// </summary>
        /// <param name="data">The cached slot-map string, or an empty string when no map is cached.</param>
        /// <returns><see cref="ErrorCode.Success"/> when the cache is accessible.</returns>
        public ErrorCode ReturnSlotMapStatus(out string data)
        {
            try
            {
                ThrowIfDisposed();

                data = _cachedSlotMap ?? string.Empty;
                return ErrorCode.Success;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("ReturnSlotMapStatus: exception - {0}", ex.Message));
                data = string.Empty;
                throw;
            }
        }

        #endregion

        #region Event Reporting Commands

        /// <summary>
        /// Starts loadport-related event reporting on the TAS300 side.
        /// </summary>
        /// <remarks>
        /// This method enables both general loadport events and FOUP-specific events by sending
        /// <c>EVT:EVTON</c> followed by <c>EVT:FPEON</c>.
        /// </remarks>
        /// <returns>The result of the enable sequence.</returns>
        public ErrorCode StartReportLoadport()
        {
            try
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Info, "StartReportLoadport: sending EVT:EVTON + EVT:FPEON");
                var result = SendAckOnlyCommand(CMD_EVTON);
                if (result != ErrorCode.Success)
                {
                    return result;
                }
                return SendAckOnlyCommand(CMD_FPEON);
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("StartReportLoadport: exception - {0}", ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Starts FOUP-only event reporting on the TAS300 side.
        /// </summary>
        /// <returns>The result of the <c>EVT:FPEON</c> command.</returns>
        public ErrorCode StartReportFOUP()
        {
            try
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Info, "StartReportFOUP: sending EVT:FPEON");
                return SendAckOnlyCommand(CMD_FPEON);
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("StartReportFOUP: exception - {0}", ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Stops general loadport event reporting on the TAS300 side.
        /// </summary>
        /// <returns>The result of the <c>EVT:EVTOF</c> command.</returns>
        public ErrorCode StopReportLoadport()
        {
            try
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Info, "StopReportLoadport: sending EVT:EVTOF");
                return SendAckOnlyCommand(CMD_EVTOF);
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("StopReportLoadport: exception - {0}", ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Stops FOUP event reporting on the TAS300 side.
        /// </summary>
        /// <returns>The result of the <c>EVT:FPEOF</c> command.</returns>
        public ErrorCode StopReportFOUP()
        {
            try
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Info, "StopReportFOUP: sending EVT:FPEOF");
                return SendAckOnlyCommand(CMD_FPEOF);
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("StopReportFOUP: exception - {0}", ex.Message));
                throw;
            }
        }

        #endregion

        #region Debug / Pass-through

        /// <summary>
        /// Sends a caller-supplied TAS300 command through the appropriate handshake path.
        /// </summary>
        /// <param name="data">The most recent TAS300 payload captured for the command.</param>
        /// <param name="command">The TAS300 command string to send.</param>
        /// <returns>The result of the selected command path.</returns>
        public ErrorCode SendLoadportCommand(out string data, string command)
        {
            try
            {
                data = string.Empty;
                if (string.IsNullOrWhiteSpace(command))
                {
                    _logger.WriteLog(LOG_KEY, LogHeadType.Error, "SendLoadportCommand: command is empty");
                    return ErrorCode.CommandFailed;
                }

                string trimmed = command.TrimStart();
                _logger.WriteLog(LOG_KEY, LogHeadType.Info, string.Format("SendLoadportCommand: sending {0}", trimmed));

                ErrorCode result;
                if (trimmed.StartsWith("GET:", StringComparison.OrdinalIgnoreCase)
                    || trimmed.StartsWith("EVT:", StringComparison.OrdinalIgnoreCase)
                    || trimmed.StartsWith("MOD:", StringComparison.OrdinalIgnoreCase)
                    || trimmed.StartsWith("TCH:", StringComparison.OrdinalIgnoreCase))
                {
                    result = SendAckOnlyCommand(trimmed);
                }
                else if (trimmed.StartsWith("MOV:", StringComparison.OrdinalIgnoreCase)
                    || trimmed.StartsWith("SET:", StringComparison.OrdinalIgnoreCase))
                {
                    result = SendMovSetCommand(trimmed);
                }
                else
                {
                    _logger.WriteLog(LOG_KEY, LogHeadType.Error,
                        string.Format("SendLoadportCommand: unknown command prefix, rejected: {0}", trimmed));
                    return ErrorCode.CommandFailed;
                }

                if (result == ErrorCode.Success)
                {
                    data = _lastResponseData ?? string.Empty;
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("SendLoadportCommand: exception - {0}", ex.Message));
                data = string.Empty;
                throw;
            }
        }

        #endregion

        #region Internal Methods — Command Execution

        /// <summary>
        /// Sends a MOV or SET command and waits for the full TAS300 operation handshake to complete.
        /// </summary>
        /// <param name="command">The TAS300 operation command string.</param>
        /// <returns>The handshake outcome translated to an <see cref="ErrorCode"/> value.</returns>
        private ErrorCode SendMovSetCommand(string command)
        {
            ThrowIfDisposed();

            if (_connector == null)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Error, "SendMovSetCommand: connector is null");
                return ErrorCode.CommandFailed;
            }

            _ackSignal.Reset();
            _infSignal.Reset();

            // Phase 1: Send command, wait for ACK
            var commandBytes = Encoding.ASCII.GetBytes(command);
            _connector.Send(commandBytes, commandBytes.Length);
            if (!_ackSignal.Wait(Config.AckTimeout))
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Error, string.Format("SendMovSetCommand: ACK timeout for {0}", command));
                return ErrorCode.AckTimeout;
            }

            if (_lastResponseType == RES_NAK)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Error, string.Format("SendMovSetCommand: NAK received for {0}", command));
                return ErrorCode.CommandFailed;
            }

            // Phase 2: Wait for INF/ABS completion
            if (!_infSignal.Wait(Config.InfTimeout))
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Error, string.Format("SendMovSetCommand: INF timeout for {0}", command));
                return ErrorCode.InfTimeout;
            }

            SendFinCommand(command);

            return (_lastResponseType == RES_INF)
                ? ErrorCode.Success
                : ErrorCode.CommandFailed;
        }

        /// <summary>
        /// Sends the terminal <c>FIN</c> frame for a completed MOV/SET command sequence.
        /// </summary>
        /// <param name="command">The original TAS300 command that completed successfully.</param>
        private void SendFinCommand(string command)
        {
            string commandName = ExtractCommandName(command);
            if (string.IsNullOrEmpty(commandName))
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Warning,
                    string.Format("SendFinCommand: unable to extract command name from {0}", command));
                return;
            }

            string finCommand = CMD_FIN_PREFIX + commandName;
            _logger.WriteLog(LOG_KEY, LogHeadType.Info, string.Format("SendFinCommand: sending {0}", finCommand));

            var finBytes = Encoding.ASCII.GetBytes(finCommand);
            _connector.Send(finBytes, finBytes.Length);
        }

        /// <summary>
        /// Extracts the TAS300 command name from a full command string.
        /// </summary>
        /// <param name="command">A TAS300 command such as <c>MOV:ORGSH</c> or <c>SET:LON01</c>.</param>
        /// <returns>The command name without prefix, parameters, or trailing separators.</returns>
        private static string ExtractCommandName(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return string.Empty;

            int colonIndex = command.IndexOf(':');
            if (colonIndex < 0 || colonIndex + 1 >= command.Length)
                return string.Empty;

            int slashIndex = command.IndexOf('/', colonIndex + 1);
            string commandName = slashIndex >= 0
                ? command.Substring(colonIndex + 1, slashIndex - colonIndex - 1)
                : command.Substring(colonIndex + 1);

            return commandName.Trim().TrimEnd(';');
        }

        /// <summary>
        /// Sends a MOD, GET, EVT, or TCH command and waits only for the TAS300 acknowledgement frame.
        /// </summary>
        /// <param name="command">The TAS300 command string.</param>
        /// <returns>The acknowledgement outcome translated to an <see cref="ErrorCode"/> value.</returns>
        private ErrorCode SendAckOnlyCommand(string command)
        {
            ThrowIfDisposed();

            if (_connector == null)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Error, "SendAckOnlyCommand: connector is null");
                return ErrorCode.CommandFailed;
            }

            _ackSignal.Reset();

            var commandBytes = Encoding.ASCII.GetBytes(command);
            _connector.Send(commandBytes, commandBytes.Length);
            if (!_ackSignal.Wait(Config.AckTimeout))
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Error, string.Format("SendAckOnlyCommand: ACK timeout for {0}", command));
                return ErrorCode.AckTimeout;
            }

            return (_lastResponseType == RES_NAK)
                ? ErrorCode.CommandFailed
                : ErrorCode.Success;
        }

        #endregion

        #region Internal Methods — DataReceived Handler

        /// <summary>
        /// Processes inbound TAS300 frames on the connector callback thread.
        /// </summary>
        /// <remarks>
        /// The handler first checks for asynchronous FOUP event frames. All other responses are classified and then
        /// routed to the acknowledgement or completion wait handle used by the synchronous command methods.
        /// </remarks>
        private void OnDataReceived(byte[] byData, int length)
        {
            try
            {
                if (Interlocked.CompareExchange(ref _disposed, 0, 0) != 0)
                    return;

                if (byData == null || length <= 0)
                    return;

                string data = Encoding.ASCII.GetString(byData, 0, length);

                // Check for FOUP event responses first
                if (TryParseFoupEvent(data))
                    return;

                var responseType = ParseResponseType(data);
                _lastResponseType = responseType;
                _lastResponseData = ExtractResponseData(data);

                switch (responseType)
                {
                    case RES_ACK:
                    case RES_NAK:
                        _ackSignal.Set();
                        break;
                    case RES_INF:
                    case RES_ABS:
                        _infSignal.Set();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Exception, string.Format("OnDataReceived: exception - {0}", ex.Message));
            }
        }

        /// <summary>
        /// Classifies a raw TAS300 frame as ACK, NAK, INF, or ABS.
        /// </summary>
        /// <param name="data">Raw response string.</param>
        /// <returns>Response type constant.</returns>
        private int ParseResponseType(string data)
        {
            if (string.IsNullOrEmpty(data))
                return RES_NAK;

            if (data.IndexOf("ACK", StringComparison.Ordinal) >= 0)
                return RES_ACK;
            if (data.IndexOf("NAK", StringComparison.Ordinal) >= 0)
                return RES_NAK;
            if (data.IndexOf("INF", StringComparison.Ordinal) >= 0)
                return RES_INF;
            if (data.IndexOf("ABS", StringComparison.Ordinal) >= 0)
                return RES_ABS;

            return RES_NAK;
        }

        /// <summary>
        /// Extracts the payload portion of a TAS300 frame.
        /// </summary>
        /// <param name="data">Raw response string.</param>
        /// <returns>Extracted data payload, or empty string.</returns>
        private string ExtractResponseData(string data)
        {
            if (string.IsNullOrEmpty(data))
                return string.Empty;

            // Find the data payload after the response type + colon separator
            int colonIdx = data.IndexOf(':');
            if (colonIdx >= 0 && colonIdx + 1 < data.Length)
            {
                return data.Substring(colonIdx + 1);
            }
            return data;
        }

        /// <summary>
        /// Detects and processes asynchronous FOUP event frames from TAS300.
        /// </summary>
        /// <remarks>
        /// When a supported FOUP event is found, this method updates the cached FOUP event data, adjusts the cached
        /// FOUP status when appropriate, raises the matching event, and prevents the frame from being treated as a
        /// normal command response.
        /// </remarks>
        /// <param name="data">Raw response string.</param>
        /// <returns>True if a FOUP event was detected and processed.</returns>
        private bool TryParseFoupEvent(string data)
        {
            if (data.IndexOf(FOUP_EVT_PODOF, StringComparison.Ordinal) >= 0)
            {
                UpdateFoupStatus(FPS_NOFOUP);
                _fpEvent = FPEVT_PODOF;
                RaiseFoupReportStarted(FPEVT_PODOF);
                return true;
            }
            if (data.IndexOf(FOUP_EVT_PODON, StringComparison.Ordinal) >= 0)
            {
                UpdateFoupStatus(FPS_PLACED);
                _fpEvent = FPEVT_PODON;
                RaiseFoupReportStarted(FPEVT_PODON);
                return true;
            }
            if (data.IndexOf(FOUP_EVT_SMTON, StringComparison.Ordinal) >= 0)
            {
                _fpEvent = FPEVT_SMTON;
                RaiseFoupReportStarted(FPEVT_SMTON);
                return true;
            }
            if (data.IndexOf(FOUP_EVT_ABNST, StringComparison.Ordinal) >= 0)
            {
                _fpEvent = FPEVT_ABNST;
                RaiseFoupReportStarted(FPEVT_ABNST);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Handles the legacy connector success callback retained for compatibility with the existing wiring.
        /// </summary>
        /// <remarks>
        /// The authoritative readiness decision is made by <see cref="Connector_ConnectedSuccess"/>, which
        /// actively validates the device through <c>GET:STATE</c>. This method preserves the historical event
        /// flow expected by older code paths.
        /// </remarks>
        private void MouduleSuccessCheck()
        {
            #region Send check status is correct
            #endregion

            ModuleInitialize = true;
        }

        #endregion

        #region Internal Methods — Status Parsing

        /// <summary>
        /// Queries <c>GET:STATE</c> and refreshes all cached status values derived from that payload.
        /// </summary>
        /// <remarks>
        /// A successful query updates the raw <see cref="LoadportStatus"/>, the derived FOUP mechanical state, and the
        /// actor's simplified FXL readiness state.
        /// </remarks>
        /// <returns>ErrorCode from the GET:STATE query.</returns>
        private ErrorCode GetFxlAmhsStatus()
        {
            var result = SendAckOnlyCommand(CMD_STATE);
            if (result != ErrorCode.Success)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Error, string.Format("GetFxlAmhsStatus: GET:STATE failed - {0}", result));
                return result;
            }

            string responseData = _lastResponseData ?? string.Empty;
            ParseStatusResponse(responseData);
            DeriveFoupStatus();
            UpdateFxlState();

            return ErrorCode.Success;
        }

        /// <summary>
        /// Parses a 20-character <c>GET:STATE</c> payload into the cached <see cref="LoadportStatus"/> structure.
        /// </summary>
        /// <param name="data">20-character response string.</param>
        private void ParseStatusResponse(string data)
        {
            if (string.IsNullOrEmpty(data) || data.Length < 20)
            {
                _logger.WriteLog(LOG_KEY, LogHeadType.Warning, string.Format("ParseStatusResponse: invalid data length {0}", data?.Length ?? 0));
                return;
            }

            var oldStatus = _status;

            _status.EqpStatus = data[0];
            _status.Mode = data[1];
            _status.Inited = data[2];
            _status.OpStatus = data[3];

            // Positions 5-6 (0-indexed: [4],[5]): 2 ASCII hex chars → byte
            byte ecode;
            if (byte.TryParse(data.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ecode))
            {
                _status.Ecode = ecode;
            }

            _status.FpPlace = data[6];
            _status.FpClamp = data[7];
            _status.LtchKey = data[8];
            _status.Vacuum = data[9];
            _status.FpDoor = data[10];
            _status.WfProtrusion = data[11];
            _status.ZPos = data[12];
            _status.YPos = data[13];
            _status.MpArmPos = data[14];
            _status.MpZPos = data[15];
            _status.MpStopper = data[16];
            _status.MappingStatus = data[17];
            _status.IntKey = data[18];
            _status.InfoPad = data[19];

            if (!StatusEquals(oldStatus, _status))
            {
                RaiseStatusChanged(_status);
            }
        }

        /// <summary>
        /// Compares two <see cref="LoadportStatus"/> values field by field.
        /// </summary>
        private static bool StatusEquals(LoadportStatus a, LoadportStatus b)
        {
            return a.EqpStatus == b.EqpStatus
                && a.Mode == b.Mode
                && a.Inited == b.Inited
                && a.OpStatus == b.OpStatus
                && a.Ecode == b.Ecode
                && a.FpPlace == b.FpPlace
                && a.FpClamp == b.FpClamp
                && a.LtchKey == b.LtchKey
                && a.Vacuum == b.Vacuum
                && a.FpDoor == b.FpDoor
                && a.WfProtrusion == b.WfProtrusion
                && a.ZPos == b.ZPos
                && a.YPos == b.YPos
                && a.MpArmPos == b.MpArmPos
                && a.MpZPos == b.MpZPos
                && a.MpStopper == b.MpStopper
                && a.MappingStatus == b.MappingStatus
                && a.IntKey == b.IntKey
                && a.InfoPad == b.InfoPad;
        }

        /// <summary>
        /// Derives the high-level FOUP mechanical status from the cached sensor fields.
        /// </summary>
        /// <remarks>
        /// The decision flow follows the sensor dependency order <c>FpPlace → FpClamp → YPos → FpDoor</c>.
        /// </remarks>
        private void DeriveFoupStatus()
        {
            char fpPlace = _status.FpPlace;
            char fpClamp = _status.FpClamp;
            char yPos = _status.YPos;
            char fpDoor = _status.FpDoor;

            if (fpPlace == '0')
            {
                UpdateFoupStatus(FPS_NOFOUP);
                return;
            }

            // fpPlace == '2' (misplaced): do not update FoupStatus
            if (fpPlace == '2')
                return;

            if (fpPlace != '1')
                return;

            // fpPlace == '1': placed
            if (fpClamp == '0')
            {
                UpdateFoupStatus(FPS_PLACED);
                return;
            }

            if (fpClamp != '1')
                return;

            // fpClamp == '1': clamped
            if (yPos == '0')
            {
                UpdateFoupStatus(FPS_CLAMPED);
                return;
            }

            if (yPos != '1')
                return;

            // yPos == '1': docked position
            if (fpDoor == '1')
            {
                UpdateFoupStatus(FPS_DOCKED);
            }
            else if (fpDoor == '0')
            {
                UpdateFoupStatus(FPS_OPENED);
            }
            // fpDoor == '?': do not update
        }

        /// <summary>
        /// Updates the cached FOUP mechanical status and raises the change notification when it transitions.
        /// </summary>
        /// <param name="newStatus">The newly derived FOUP status value.</param>
        private void UpdateFoupStatus(int newStatus)
        {
            if (_fpStatus == newStatus)
                return;

            _fpStatus = newStatus;
            RaiseFoupStatusChanged();
        }

        /// <summary>
        /// Updates the simplified internal FXL state from the cached initialization and FOUP conditions.
        /// </summary>
        private void UpdateFxlState()
        {
            if (_status.Inited == '0')
            {
                Interlocked.Exchange(ref _fxlState, FXL_NOTINIT);
                return;
            }

            int newState = (_fpStatus <= FPS_PLACED) ? FXL_READY : FXL_BUSY;
            Interlocked.Exchange(ref _fxlState, newState);
        }

        /// <summary>
        /// Translates the current FOUP-related status into the legacy <c>statfxl</c> code expected by callers.
        /// </summary>
        /// <remarks>
        /// A misplaced FOUP is handled as a special case and returns <c>0x68</c> even though the cached FOUP status is
        /// intentionally not rewritten to that value.
        /// </remarks>
        /// <param name="foupStatus">Current derived FOUP status.</param>
        /// <param name="fpPlace">Raw fpPlace sensor value.</param>
        /// <returns>Hex status code string (e.g. "0x69").</returns>
        private string GetStatfxlCode(int foupStatus, char fpPlace)
        {
            if (fpPlace == '2')
                return STATFXL_MISPLACED;

            string code;
            return StatfxlCodes.TryGetValue(foupStatus, out code) ? code : string.Empty;
        }

        #endregion

        #region Internal Methods — LED Control

        /// <summary>
        /// Builds and sends a TAS300 LED command, then updates the cached LED state on success.
        /// </summary>
        /// <param name="ledNo">LED number (1-13).</param>
        /// <param name="action">Action: 0=OFF, 1=ON, 2=BLINK.</param>
        /// <returns>ErrorCode from the Operation Command.</returns>
        private ErrorCode LampOP(int ledNo, int action)
        {
            string prefix;
            switch (action)
            {
                case LED_ON:
                    prefix = CMD_LED_ON_PREFIX;
                    break;
                case LED_OFF:
                    prefix = CMD_LED_OFF_PREFIX;
                    break;
                case LED_BLINK:
                    prefix = CMD_LED_BLINK_PREFIX;
                    break;
                default:
                    return ErrorCode.CommandFailed;
            }

            string command = string.Format("{0}{1:D2}", prefix, ledNo);
            _logger.WriteLog(LOG_KEY, LogHeadType.Info, string.Format("LampOP: sending {0}", command));

            var result = SendMovSetCommand(command);
            if (result == ErrorCode.Success && ledNo >= 1 && ledNo <= LED_COUNT)
            {
                int previousStatus = _ledStatus[ledNo - 1];
                _ledStatus[ledNo - 1] = action;
                if (previousStatus != action)
                {
                    RaiseLedChanged(ledNo, action);
                }
            }
            return result;
        }

        #endregion

        #region Internal Methods — Slot Map Conversion

        /// <summary>
        /// Convert TAS300 raw slot map character to SEMI standard value (FR-071).
        /// '0'→1 (empty), '1'→3 (normal), '2'→5 (crossed), 'W'→4 (double), other→0 (undefined).
        /// </summary>
        /// <param name="rawValue">Raw character from TAS300 MAPRD response.</param>
        /// <returns>SEMI format integer value.</returns>
        internal static int ConvertToSemiFormat(char rawValue)
        {
            switch (rawValue)
            {
                case '0': return 1;
                case '1': return 3;
                case '2': return 5;
                case 'W': return 4;
                default: return 0;
            }
        }

        /// <summary>
        /// Converts a raw TAS300 slot-map payload into a fixed-length SEMI-format array.
        /// </summary>
        /// <param name="rawMap">Raw map string from GET:MAPRD response.</param>
        /// <returns>25-element SEMI format array.</returns>
        private int[] ConvertRawToSemiArray(string rawMap)
        {
            var result = new int[SLOT_COUNT];
            for (int i = 0; i < SLOT_COUNT && i < rawMap.Length; i++)
            {
                result[i] = ConvertToSemiFormat(rawMap[i]);
            }
            return result;
        }

        /// <summary>
        /// Compares two slot-map arrays for value equality.
        /// </summary>
        /// <param name="left">The previously cached slot-map array.</param>
        /// <param name="right">The newly parsed slot-map array.</param>
        /// <returns><see langword="true"/> when both arrays contain the same values in the same order.</returns>
        private static bool SlotMapEquals(int[] left, int[] right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left == null || right == null || left.Length != right.Length)
                return false;

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Formats a SEMI-format slot-map array as the space-separated string returned to callers.
        /// </summary>
        /// <param name="slotMap">SEMI format int array.</param>
        /// <returns>Space-separated string of SEMI values.</returns>
        private string FormatSlotMapString(int[] slotMap)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < slotMap.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(slotMap[i]);
            }
            return sb.ToString();
        }

        #endregion

        #region Internal Methods — Event Raise Helpers

        /// <summary>
        /// Raises the slot-map scanned event with the latest SEMI-format slot-map snapshot.
        /// </summary>
        /// <param name="slotMap">The slot-map data delivered to subscribers.</param>
        private void RaiseSlotMapScanned(int[] slotMap)
        {
            SlotMapScanned?.Invoke(slotMap);
        }

        /// <summary>
        /// Raises the FOUP report event for an asynchronously reported TAS300 FOUP event.
        /// </summary>
        /// <param name="reportType">The internal FOUP event identifier.</param>
        private void RaiseFoupReportStarted(int reportType)
        {
            FoupReportStarted?.Invoke(reportType);
        }

        /// <summary>
        /// Raises the LED changed event after a successful LED command changes the cached LED state.
        /// </summary>
        /// <param name="ledNo">The one-based LED index.</param>
        /// <param name="status">The new LED state value.</param>
        private void RaiseLedChanged(int ledNo, int status)
        {
            LedChanged?.Invoke(ledNo, status);
        }

        /// <summary>
        /// Raises the loadport status changed event with the newly parsed <see cref="LoadportStatus"/> snapshot.
        /// </summary>
        /// <param name="newStatus">The updated status delivered to subscribers.</param>
        private void RaiseStatusChanged(LoadportStatus newStatus)
        {
            StatusChanged?.Invoke(newStatus);
        }

        /// <summary>
        /// Raises the FOUP status changed event after the derived FOUP mechanical state transitions.
        /// </summary>
        private void RaiseFoupStatusChanged()
        {
            FoupStatusChanged?.Invoke();
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Releases connector subscriptions, wait handles, and cached state held by this actor.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose pattern with idempotent flag using Interlocked.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(); false from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            // Idempotent: only execute once
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return;

            if (disposing)
            {
                // 1. Unsubscribe connector events without re-entering the public setter.
                if (_connector != null)
                {
                    _connector.DataReceived -= OnDataReceived;
                    _connector = null;
                }

                // 2. Reset state machines
                Interlocked.Exchange(ref _fxlState, FXL_NOTINIT);
                _fpStatus = FPS_UNKNOWN;
                _fpEvent = FPEVT_NONE;

                // 3. Clear cached data
                _cachedSlotMap = null;
                _cachedSlotMapArray = null;

                // 4. Dispose signal handles
                _ackSignal?.Dispose();
                _infSignal?.Dispose();
            }
        }

        /// <summary>
        /// Throws <see cref="ObjectDisposedException"/> when the actor has already been disposed.
        /// Prevents command execution and connector replacement after cleanup.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (Interlocked.CompareExchange(ref _disposed, 0, 0) != 0)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        #endregion
    }
}
