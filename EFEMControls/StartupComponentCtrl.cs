using Communication;
using Communication.Connector;
using Communication.Interface;
using Communication.Protocol;
using EFEM.DataCenter;
using EFEM.ExceptionManagements;
using EFEM.FileUtilities;
using LogUtility;
using Microsoft.VisualBasic.Devices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using EFEM.GUIControls.Enum;
using TDKController;
using TDKController.Interface;
using TDKLogUtility.Module;
using static EFEM.DataCenter.ConstVC;
using LogHeadType = EFEM.LogUtilities.LogHeadType;

namespace EFEM.GUIControls
{
    public delegate ArrayList delInitializeGUI();
    public partial class StartupComponentCtrl : UserControl
    {
        #region Fields
        
        private delInitializeGUI _initializeGuiHelper;
        private readonly ArrayList _errorHistory = new ArrayList();
        private const int DelayTimeForDebug = 100;
        private bool _inversed;
        AbstractFileUtilities _fu = FileUtility.GetUniqueInstance();
        private ILogUtility _log;
        private Dictionary<string, IConnector> _communication = new Dictionary<string, IConnector>();
        private readonly Dictionary<string, ILoadPortActor> _loadPort = new Dictionary<string, ILoadPortActor>(); 
        public List<(string, int)> CommList = new List<(string, int)>();
        public Dictionary<string, TCPConfig> TcpSetting = new Dictionary<string, TCPConfig>();
        public Dictionary<string, RS232Config> SerialSetting = new Dictionary<string, RS232Config>();
        public List<string> DioList = new List<string>();
        public Dictionary<string, DIOConfig> DioSetting = new Dictionary<string, DIOConfig>();
        public List<string> LoadPortList = new List<string>();
        public Dictionary<string, LoadPortConfig> LoadPortSetting = new Dictionary<string, LoadPortConfig>();
        public List<string> N2NozzleList = new List<string>();
        public Dictionary<string, N2NozzleConfig> N2NozzleSetting = new Dictionary<string, N2NozzleConfig>();
        #endregion Fields

        #region Property
        public ArrayList ErrorHistory
        {
            get
            {
                if (_errorHistory == null || _errorHistory.Count == 0)
                    return null;
                else
                    return _errorHistory;
            }
        }

        public Control HistoryListControl => dataGridViewResult;

        public bool IsAnyErrorOccurs => ErrorHistory != null;
        #endregion Property

        #region Event
        public event EventHandler InvokeHostActionRequested;
        #endregion Event
        #region Constructors
        public StartupComponentCtrl()
        {
            
            _log = LogUtilityClient.GetUniqueInstance("",0);
            InitializeComponent();
            InitDataGrid();
        }
        #endregion Constructors
        #region Public methods
        public void ShowContinueButton(bool Hide = false)
        {
            btnContinue.Visible = !Hide;
            if (IsAnyErrorOccurs)
                timerBlinking.Enabled = true;
        }
        public void ReleaseHistoryListControl(Control ctrl)
        {
            ctrl.Parent = panelDataGrid;
        }
        public HRESULT StartupAll(delInitializeGUI InitGUIMethod)
        {
            _initializeGuiHelper = InitGUIMethod;
            //ThreadPool.QueueUserWorkItem(new WaitCallback(TPOOL_StartupAll), com);
            TPOOL_StartupAll();
            return null;
        }
        #endregion Public methods

        #region Private methods
        private void InitDataGrid()
        {
            for (int i = 0; i < 10; i++)
            {
                dataGridViewResult.Rows.Add();
                dataGridViewResult.Rows[i].Cells["Image"].Value = imageListStatus.Images[0];
                dataGridViewResult.Rows[i].Cells["Status"].Value = "Waiting";
                dataGridViewResult.Rows[i].Cells["ObjectName"].Value = GetObjectName(i);
                dataGridViewResult.Rows[i].Cells["ErrorMsg"].Value = "";
            }

            ExtendMethods.StretchLastColumn(dataGridViewResult);
        }
        private string GetObjectName(int id)
        {
            switch (id)
            {
                case 0:
                    return "Communication";
                case 1:
                    return "DIO";
                case 2:
                    return "LoadPortActor";
                case 3:
                    return "CarrierIDReader";
                case 4:
                    return "LightCurtain";
                case 5:
                    return "N2Nozzle";
                case 6:
                    return "LoadPortController";
                case 7:
                    return "LoadPortService";
                case 8:
                    return "E84Station";
                case 9:
                    return "Initialize GUI Components";
                default:
                    return "";
            }
        }

        private string GetProcedureName(int id)
        {
            switch (id)
            {
                case 0:
                case 4:
                    return "Instantiate Objects";
                case 1:
                case 5:
                    return "Establish Communications";
                case 2:
                case 6:
                    return "Download Parameters";
                case 3:
                case 7:
                    return "Initialize";
                case 9:
                    return "Initialize GUI Components";
                default:
                    return "";
            }
        }
        private void UpdateStatus(string currentStatus)
        {
            if (lCurrentStatus.InvokeRequired)
            {
                MethodInvoker del = delegate { UpdateStatus(currentStatus); };
                lCurrentStatus.Invoke(del);
            }
            else
            {
                if (currentStatus == "Finish")
                {
                    if (IsAnyErrorOccurs)
                    {
                        lCurrentStatus.BackColor = Color.Red;
                        lCurrentStatus.ForeColor = Color.White;
                        lCurrentStatus.Text = "Error Occurred During Initialization of EFEM components.";
                    }
                    else
                    {
                        lCurrentStatus.Text = "Success";
                    }
                }
                else
                    lCurrentStatus.Text = currentStatus;
            }
        }
        private void UpdateProcedureStatus(int ProcedureID, StartupComponentEnum.ProcedureStatus status, ArrayList rst = null)
        {
            if (lCurrentStatus.InvokeRequired)
            {
                MethodInvoker del = delegate { UpdateProcedureStatus(ProcedureID, status, rst); };
                lCurrentStatus.Invoke(del);
            }
            else
            {
                switch (status)
                {
                    case StartupComponentEnum.ProcedureStatus.Pending:
                        {
                            dataGridViewResult.Rows[ProcedureID].Cells["Image"].Value = imageListStatus.Images[0];
                            dataGridViewResult.Rows[ProcedureID].Cells["Status"].Value = "Pending";
                            dataGridViewResult.Rows[ProcedureID].Cells["ErrorMsg"].Value = "";
                            break;
                        }
                    case StartupComponentEnum.ProcedureStatus.Working:
                        {
                            GUIBasic.Instance().WriteLog(LogHeadType.CallStart, GetProcedureName(ProcedureID));
                            dataGridViewResult.Rows[ProcedureID].Cells["Image"].Value = imageListStatus.Images[1];
                            dataGridViewResult.Rows[ProcedureID].Cells["Status"].Value = "Working";
                            dataGridViewResult.Rows[ProcedureID].Cells["ErrorMsg"].Value = "";
                            break;
                        }
                    case StartupComponentEnum.ProcedureStatus.Finish:
                        {
                            if (rst != null && rst.Count != 0)
                            {
                                string errMsg = ExtendMethods.ToStringHelper(rst, "; ");
                                GUIBasic.Instance().WriteLog(LogHeadType.CallEnd, GetProcedureName(ProcedureID) + ", Fail. Reason: " + errMsg);
                                _errorHistory.Add("[" + GetProcedureName(ProcedureID) + "] " + errMsg);
                                dataGridViewResult.Rows[ProcedureID].Cells["Image"].Value = imageListStatus.Images[3];
                                dataGridViewResult.Rows[ProcedureID].Cells["Status"].Value = "Error";
                                dataGridViewResult.Rows[ProcedureID].Cells["ErrorMsg"].Value = errMsg;
                            }
                            else
                            {
                                GUIBasic.Instance().WriteLog(LogHeadType.CallEnd, GetProcedureName(ProcedureID) + ", Success.");
                                dataGridViewResult.Rows[ProcedureID].Cells["Image"].Value = imageListStatus.Images[2];
                                dataGridViewResult.Rows[ProcedureID].Cells["Status"].Value = "Success";
                                dataGridViewResult.Rows[ProcedureID].Cells["ErrorMsg"].Value = "";
                            }
                            break;
                        }
                    default:
                        break;
                }
            }
        }
        private void RunInitStartFlow(Func<ArrayList> func, int curProcrdure = 0)
        {
            var objectName = GetObjectName(curProcrdure);
            var procedureName = GetProcedureName(7);
            var str = objectName + " Initialize Start.";
            _log.WriteLog("TDK", str);
            UpdateStatus(objectName + " : " + procedureName);
            UpdateProcedureStatus(curProcrdure, StartupComponentEnum.ProcedureStatus.Working);
            GUIBasic.Instance().VariableCenter.SetValueAndFireCallback(ConstVC.VariableCenter.CurrentStatus,
                objectName + " -> " + procedureName);

            var rst = func?.Invoke();
            UpdateProcedureStatus(curProcrdure, StartupComponentEnum.ProcedureStatus.Finish, rst);
            SpinWait.SpinUntil(() => false, DelayTimeForDebug);
            str = objectName + " Initialize End.";
            _log.WriteLog("TDK", str);
        }
        private void TPOOL_StartupAll()
        {
            try
            {
                _errorHistory.Clear();
                #region Communication Init
                RunInitStartFlow(Communication_Initialize, 0);
                #endregion Communication Init

                #region DIO Init
                RunInitStartFlow(DIO_Initialize, 1);
                #endregion DIO Init

                #region LoadPortActor Init
                RunInitStartFlow(LoadPort_Initialize, 2);
                #endregion LoadPortActor Init

                #region N2Nozzle Init
                RunInitStartFlow(N2Nozzle_Initialize, 5);
                #endregion N2Nozzle Init

                #region Init GUI
                RunInitStartFlow(() => _initializeGuiHelper(), 9);
                
                //Clean log and start the timer to claen logs every 24 hours
                GUIBasic.Instance().Log.ClearLogs();
                GUIBasic.Instance().WriteLog(LogHeadType.CallEnd, "", "Initialize GUI Components.Initialize");
                #region GUI Assign
                #endregion GUI Assign
                #endregion



                UpdateStatus("Finish");
                GUIBasic.Instance().VariableCenter.SetValueAndFireCallback(ConstVC.VariableCenter.CurrentStatus,
                    "All Initializations Finish");
            }
            catch (Exception e)
            {
                UpdateStatus("Exception: " + e.Message);
                _errorHistory.Add("[TPOOL_StartupAll] " + e.Message + e.StackTrace);
                GUIBasic.Instance().VariableCenter.SetValueAndFireCallback(ConstVC.VariableCenter.CurrentStatus,
                    "All Initializations Finish (Fail)");
            }
            finally
            {
                ShowContinueButton(false);
            }
        }

        #region Each flow step initialize
        private ArrayList Communication_Initialize()
        {
            string str = string.Empty;
            ArrayList al = new ArrayList();
            try
            {
                AbstractFileUtilities fu = FileUtility.GetUniqueInstance();
                IConnectorConfig com;

                CommList = fu.GetCommList();

                if (CommList.Count == 0)
                {
                    str = "Communication Config Load Error.";
                    al.Add(str);
                    _log.WriteLog("TDK_GUI",TDKLogUtility.Module.LogHeadType.Error, str);
                }
                else
                {
                    foreach (var comm in CommList)
                    {
                        switch ((StartupComponentEnum.CommType)comm.Item2)
                        {
                            case StartupComponentEnum.CommType.TCPIP:
                                TcpSetting[comm.Item1] = fu.GetTCPSetting(comm.Item1);
                                if (TcpSetting[comm.Item1].Ip.Equals(string.Empty)
                                    || TcpSetting[comm.Item1].Port.Equals(string.Empty))
                                {
                                    str = comm.Item1 + " : Wrong Value of Communication Setting.(TCPIP)";
                                    al.Add(str);
                                    _log.WriteLog("TDK_GUI", TDKLogUtility.Module.LogHeadType.Error, str);
                                }
                                else
                                {
                                    com = new TCPConnectorConfig(TcpSetting[comm.Item1]);
                                    _communication[comm.Item1] = new TcpipConnector(new DefaultProtocol(), com, _log);
                                }

                                break;
                            case StartupComponentEnum.CommType.RS232:
                                SerialSetting[comm.Item1] = fu.GetSerialSetting(comm.Item1);
                                if (!(Regex.IsMatch(SerialSetting[comm.Item1].Port, @"^COM\d+$"))
                                    || SerialSetting[comm.Item1].Baud == -1
                                    || SerialSetting[comm.Item1].Parity == -1
                                    || SerialSetting[comm.Item1].DataBits == -1
                                    || SerialSetting[comm.Item1].StopBits == -1)
                                {
                                    str = comm.Item1 + " : Wrong Value of Communication Setting.(RS232)";
                                    al.Add(str);
                                    _log.WriteLog("TDK_GUI", TDKLogUtility.Module.LogHeadType.Error, str);
                                }
                                else
                                {
                                    com = new RS232ConnectorConfig(SerialSetting[comm.Item1]);
                                    _communication[comm.Item1] = new Rs232Connector(_log, com);
                                }

                                break;
                            default:
                                str = comm.Item1 + " : Wrong Type of Communication Setting.(Not TCP or RS232)";
                                al.Add(str);
                                _log.WriteLog("TDK_GUI", TDKLogUtility.Module.LogHeadType.Error, str);
                                break;
                        }

                    }
                }
                return al;

            }
            finally
            {
            }

        }

        public Dictionary<string, ILoadPortActor> GetModuleLoadPortList()
        {
            return _loadPort;
        }

        private ArrayList LoadPort_Initialize()
        {
            string str = string.Empty;
            ArrayList al = new ArrayList();
            LoadportActorConfig loadConfig = new LoadportActorConfig();
            List<string> connectorList = CommList.Select(x => x.Item1).ToList();
            try
            {
                LoadPortList = _fu.GetLoadPortList();
                if (LoadPortList.Count == 0)
                {
                    str = "LoadPort Config Load Error.";
                    al.Add(str);
                    _log.WriteLog("TDK_GUI", TDKLogUtility.Module.LogHeadType.Error, str);
                }
                else
                {
                    foreach (var loadPort in LoadPortList)
                    {
                        LoadPortSetting[loadPort] = _fu.GetLoadPortConfigSetting(loadPort);
                        string connector = LoadPortSetting[loadPort].Comm != null
                            ? LoadPortSetting[loadPort].Comm
                            : string.Empty;
                        LoadportActor tmplpa = null;
                        if (connector.Equals(string.Empty)
                            || !(connectorList.Contains(connector))
                            || LoadPortSetting[loadPort].ACKTimeout < 0
                            || LoadPortSetting[loadPort].INFTimeout < 0)
                        {
                            str = loadPort + " : Wrong Value of LoadPort Setting.";
                            al.Add(str);
                            _log.WriteLog("TDK_GUI", TDKLogUtility.Module.LogHeadType.Error, str);
                        }
                        else if (!(_communication.Keys.Contains(connector)))
                        {
                            str = loadPort + " : " + connector + " Initial Error.";
                            al.Add(str);
                            _log.WriteLog("TDK_GUI", TDKLogUtility.Module.LogHeadType.Error, str);
                        }
                        else
                        {
                            _communication[connector].Protocol = new LoadportProtocol();
                            loadConfig.AckTimeout = LoadPortSetting[loadPort].ACKTimeout;
                            loadConfig.InfTimeout = LoadPortSetting[loadPort].INFTimeout;
                            tmplpa = new LoadportActor(loadConfig, _communication[connector], _log);

                        }
                        if (ConnectCheck(_communication[connector]))
                        {
                            str = loadPort + " : " + connector + " Communication Connect Error.";
                            al.Add(str);
                            _log.WriteLog("TDK_GUI", TDKLogUtility.Module.LogHeadType.Error, str);
                        }
                        else
                        {
                            _loadPort[loadPort] = tmplpa;
                        }


                    }
                }

                return al;
            }
            finally
            {
            }
        }

        private ArrayList DIO_Initialize()
        {
            string str = string.Empty;
            ArrayList al = new ArrayList();

            try
            {
                DioList = _fu.GetDIOList();

                if (DioList.Count == 0)
                {
                    str = "DIO Config Load Error.";
                    al.Add(str);
                    _log.WriteLog("TDK_GUI", TDKLogUtility.Module.LogHeadType.Error, str);
                }
                else
                {
                    foreach (var dioName in DioList)
                    {
                        DioSetting[dioName] = _fu.GetDIOConfigSetting(dioName);

                        if (DioSetting[dioName].Type == -1
                            || DioSetting[dioName].Index == -1
                            || DioSetting[dioName].MaxDIPort == -1
                            || DioSetting[dioName].MaxDOPort == -1)
                        {
                            str = dioName + " : Wrong Value of DIO Setting.";
                            al.Add(str);
                            _log.WriteLog("TDK_GUI", TDKLogUtility.Module.LogHeadType.Error, str);
                        }
                        else
                        {

                        }

                    }
                }

                //al.
                return al;

            }
            finally
            {
            }
        }

        private ArrayList N2Nozzle_Initialize()
        {
            //log = new LogUtilityClient();
            string str = string.Empty;
            ArrayList al = new ArrayList();
            List<string> commList = CommList.Select(x => x.Item1).ToList();
            try
            {
                N2NozzleList = _fu.GetN2NozzleList();
                if (N2NozzleList.Count == 0)
                {
                    str = "N2Nozzle Config Load Error.";
                    al.Add(str);
                    _log.WriteLog("TDK_GUI", TDKLogUtility.Module.LogHeadType.Error, str);
                }
                else
                {
                    foreach (var n2Nozzle in N2NozzleList)
                    {
                        N2NozzleSetting[n2Nozzle] = _fu.GetN2NozzleConfigSetting(n2Nozzle);
                        string comm = N2NozzleSetting[n2Nozzle].Comm != null
                            ? N2NozzleSetting[n2Nozzle].Comm
                            : string.Empty;
                        if (comm.Equals(string.Empty) || !(commList.Contains(comm)))
                        {
                            str = n2Nozzle + " : Wrong Value of N2Nozzle Setting.";
                            al.Add(str);
                            _log.WriteLog("TDK_GUI", TDKLogUtility.Module.LogHeadType.Error, str);
                        }
                        else if (!(_communication.Keys.Contains(comm)))
                        {
                            str = n2Nozzle + " : " + comm + " Initial Error.";
                            al.Add(str);
                            _log.WriteLog("TDK_GUI", TDKLogUtility.Module.LogHeadType.Error, str);
                        }
                        else if (ConnectCheck(_communication[comm]))
                        {
                            str = n2Nozzle + " : " + comm + " Communication Connect Error.";
                            al.Add(str);
                            _log.WriteLog("TDK_GUI", TDKLogUtility.Module.LogHeadType.Error, str);
                        }
                        else
                        {

                        }

                    }
                }

                return al;

            }
            finally
            {
            }
        }

        private bool ConnectCheck(IConnector connector)
        {
            if (connector.IsConnected)
            {
                return false;
            }
            else if (connector.Connect() != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion Each flow step initialize
        #endregion Private methods

        #region GUI methods

        private void dataGridViewResult_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            e.Row.Cells["Image"].Value = imageListStatus.Images[0];
            e.Row.Cells["Status"].Value = "Waiting";
            e.Row.Cells["ErrorMsg"].Value = "";
        }

        private void btnContinue_Click(object sender, EventArgs e)
        {
            timerBlinking.Enabled = false;
            if (IsAnyErrorOccurs)
            {
                lCurrentStatus.BackColor = Color.Red;
                lCurrentStatus.ForeColor = Color.White;
            }

            InvokeHostActionRequested?.Invoke(this, EventArgs.Empty);

            GUIBasic.Instance().VariableCenter.SetValueAndFireCallback(ConstVC.VariableCenter.CurrentStatus, "ForceOperatedByUser");
        }

        private void timerBlinking_Tick(object sender, EventArgs e)
        {
            if (_inversed)
            {
                lCurrentStatus.BackColor = Color.Red;
                lCurrentStatus.ForeColor = Color.White;
            }
            else
            {
                lCurrentStatus.BackColor = SystemColors.Control;
                lCurrentStatus.ForeColor = Color.Black;
            }

            _inversed = !_inversed;
        }

        private void dataGridViewResult_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                object err = dataGridViewResult.Rows[e.RowIndex].Cells["ErrorMsg"].Value;
                if (err == null)
                    return;
                else
                {
                    string errorMsg = dataGridViewResult.Rows[e.RowIndex].Cells["ErrorMsg"].Value.ToString();
                    if (string.IsNullOrWhiteSpace(errorMsg))
                        return;
                    else
                    {
                        using (InitStatusErrorListForm listForm = new InitStatusErrorListForm())
                        {
                            string caption = string.Format("{0}",
                                dataGridViewResult.Rows[e.RowIndex].Cells["ObjectName"].Value.ToString());

                            if (listForm.AssignData(caption, errorMsg))
                            {
                                listForm.TopMost = true;
                                listForm.StartPosition = FormStartPosition.CenterParent;
                                listForm.ShowDialog();
                            }
                        }
                    }
                }
            }
        }

        private void copyAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                StringBuilder buffer = new StringBuilder();
                for (int j = 0; j < dataGridViewResult.RowCount; j++)
                {
                    for (int i = 1; i < dataGridViewResult.ColumnCount; i++)
                    {
                        buffer.Append(dataGridViewResult.Rows[j].Cells[i].Value.ToString());
                        buffer.Append("\t");
                    }

                    buffer.Append("\r\n");
                }

                Clipboard.SetText(buffer.ToString());
            }
            catch (Exception ex)
            {
                GUIBasic.Instance().WriteLog(LogHeadType.Exception, "Copy to clipboard failed! Reason: " + ex.Message);
                GUIBasic.Instance().ShowMessageOnTop("Copy to clipboard failed!");
            }
        }
        #endregion GUI methods
    }
}
