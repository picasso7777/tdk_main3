using Communication.Connector;
using Communication.Protocol;
using EFEM.FileUtilities;
using LogUtility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TDKController.GUI.ViewModels;
using TDKController.Interface;
using TDKLogUtility.Module;

namespace TDKController.GUI
{
    public partial class LoadportStatusPage : UserControl, ILoadportActorPage
    {
        #region Fields
        private LoadportStatusPageViewModel _viewModel;
        private ILogUtility _log;
        private ILoadPortActor _loadport;
        #endregion Fields

        #region Property
        public ILoadPortActor Loadport
        {
            get => _loadport;
            set
            {
                if (_viewModel != null)
                    _viewModel.Loadport = value;
                _loadport = value;
            }
        }
        #endregion Property

        #region Constructors
        public LoadportStatusPage()
        {
            InitializeComponent();
        }
        
        #endregion Constructors

        #region GUI Event
        private void LoadportStatusPage_Load(object sender, EventArgs e)
        {
            
        }
        private void Btn_Click(object sender, EventArgs e)
        {
            var name = ((Button)sender).Text;
            if (!Loadport.ModuleInitialize)
            {
                MessageBox.Show(@"Loadport is not connected yet, please connect first.");
                _log.WriteLog("TDKGUI", LogHeadType.Error, $"Loadport not connected, user action click method:{name}");
                return;
            }
            _log.WriteLog("TDKGUI", LogHeadType.Info, $"User action click method:{name}");

            try
            {
                ErrorCode result;
                switch (name)
                {
                    case "Initial":
                        result = Loadport.Init();
                        break;

                    case "Force Initial":
                        result = Loadport.InitForce();
                        break;
                    case "ON":
                        result = Loadport.StartReportFOUP();
                        if(result == ErrorCode.Success)
                            _viewModel.LpStatus = "ON";
                        break;
                    case "OFF":
                        result = Loadport.StopReportFOUP();
                        if (result == ErrorCode.Success)
                            _viewModel.LpStatus = "OFF";
                        break;
                    case "Send":
                        var command = SendCommandTxt.Text;
                        Loadport.SendLoadportCommand(out _, command);
                        break;
                    case "Get Status":
                        Loadport.GetLPStatus(out _);
                        break;
                }
            }
            catch (Exception ex)
            {
                _log.WriteLog("TDKGUI", LogHeadType.Error, $"User action click method:{name} error. Error message:{ex.Message}");
            }

            _log.WriteLog("TDKGUI", LogHeadType.Info, $"User action click method:{name} finish");
        }
        #endregion GUI Event

        #region Private methods
        private void DataBinding()
        {
            InitialStatusTxt.DataBindings.Add("Text", _viewModel, _viewModel.InitialStatus);
            LpEventStatusTxt.DataBindings.Add("Text", _viewModel, _viewModel.LpStatus);
            EqpStatusTxt.DataBindings.Add("Text", _viewModel, _viewModel.EqpStatus);
            EqpModeTxt.DataBindings.Add("Text", _viewModel, _viewModel.EqpMode);
            OperationStatusTxt.DataBindings.Add("Text", _viewModel, _viewModel.OperationStatus);
            ErrorCodeTxt.DataBindings.Add("Text", _viewModel, _viewModel.ErrorCode);
            SoftwareInterlockTxt.DataBindings.Add("Text", _viewModel, _viewModel.SoftwareInterlock);
            InfoPadTxt.DataBindings.Add("Text", _viewModel, _viewModel.InfoPad);
        }
        #endregion Private methods

        #region Public methods
        public void UpdateStatus()
        {
            if (Loadport.ModuleInitialize)
            {
                LoadportStatusGB.Enabled = true;
            }
            else
            {
                LoadportStatusGB.Enabled = false;
            }
        }
        public void CloseConnection()
        {
            LoadportStatusGB.Enabled = false;
        }
        public void InitData(ILoadPortActor loadport)
        {

            var ctx = SynchronizationContext.Current;
            _log = LogUtilityClient.GetUniqueInstance("", 0);
            Loadport = loadport;
            _viewModel = new LoadportStatusPageViewModel(Loadport, ctx);

            DataBinding();
            if (Loadport.ModuleInitialize)
            {
                UpdateStatus();
            }
            else
            {
                LoadportStatusGB.Enabled = false;
            }
        }
        #endregion Public methods
    }
}
