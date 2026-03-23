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
using Communication.Connector;
using Communication.Protocol;
using EFEM.FileUtilities;
using LogUtility;
using TDKController.GUI.ViewModels;
using TDKLogUtility.Module;

namespace TDKController.GUI
{
    public partial class LoadportFOUPInfoPage : UserControl
    {
        #region Fields
        private LoadportFOUPInfoPageViewModel _viewModel;
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
        /// <summary>
        /// Show the display info by this tag.
        /// true: only display 4 data.
        /// false: display all data.
        /// </summary>
        public bool DisplayOnly
        {
            set
            {
                if (value)
                {
                    ContentTLP.RowStyles[2].SizeType = SizeType.Absolute;
                    ContentTLP.RowStyles[2].Height = 0;
                    ContentTLP.RowStyles[3].SizeType = SizeType.Absolute;
                    ContentTLP.RowStyles[3].Height = 0;
                    ContentTLP.RowStyles[5].SizeType = SizeType.Absolute;
                    ContentTLP.RowStyles[5].Height = 0;
                    ContentTLP.RowStyles[6].SizeType = SizeType.Absolute;
                    ContentTLP.RowStyles[6].Height = 0;
                    ContentTLP.RowStyles[8].SizeType = SizeType.Absolute;
                    ContentTLP.RowStyles[8].Height = 0;
                    ContentTLP.RowStyles[9].SizeType = SizeType.Absolute;
                    ContentTLP.RowStyles[9].Height = 0;
                }
                else
                {
                    ContentTLP.RowStyles[0].SizeType = SizeType.Percent;
                    ContentTLP.RowStyles[0].Height = 8;
                    ContentTLP.RowStyles[1].SizeType = SizeType.Percent;
                    ContentTLP.RowStyles[1].Height = 8;
                    ContentTLP.RowStyles[2].SizeType = SizeType.Percent;
                    ContentTLP.RowStyles[2].Height = 8;
                    ContentTLP.RowStyles[3].SizeType = SizeType.Percent;
                    ContentTLP.RowStyles[3].Height = 8;
                    ContentTLP.RowStyles[4].SizeType = SizeType.Percent;
                    ContentTLP.RowStyles[4].Height = 8;
                    ContentTLP.RowStyles[5].SizeType = SizeType.Percent;
                    ContentTLP.RowStyles[5].Height = 8;
                    ContentTLP.RowStyles[6].SizeType = SizeType.Percent;
                    ContentTLP.RowStyles[6].Height = 8;
                    ContentTLP.RowStyles[7].SizeType = SizeType.Percent;
                    ContentTLP.RowStyles[7].Height = 8;
                    ContentTLP.RowStyles[8].SizeType = SizeType.Percent;
                    ContentTLP.RowStyles[8].Height = 8;
                    ContentTLP.RowStyles[9].SizeType = SizeType.Percent;
                    ContentTLP.RowStyles[9].Height = 28;
                }
            }
        }
        #endregion Property
        
        #region Constructors
        public LoadportFOUPInfoPage()
        {
            InitializeComponent();
        }
        
        #endregion Constructors
        #region GUI Event
        private void LoadportFOUPInfoPage_Load(object sender, EventArgs e)
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
                    case "FOUP EVT ON":
                        result = Loadport.StartReportFOUP();

                        if (result == ErrorCode.Success)
                        {
                            FoupEvtOnBtn.Enabled = false;
                            FoupEvtOffBtn.Enabled = true;
                        }
                        else
                        {
                            FoupEvtOnBtn.Enabled = true;
                            FoupEvtOffBtn.Enabled = false;
                        }
                        break;

                    case "FOUP EVT OFF":
                        result = Loadport.StopReportFOUP();
                        if (result == ErrorCode.Success)
                        {
                            FoupEvtOnBtn.Enabled = true;
                            FoupEvtOffBtn.Enabled = false;
                        }
                        else
                        {
                            FoupEvtOnBtn.Enabled = false;
                            FoupEvtOffBtn.Enabled = true;
                        }
                        break;

                    case "Load":
                        result = Loadport.Load();
                        if (result == ErrorCode.Success)
                        {
                            LoadBtn.Enabled = false;
                            UnloadBtn.Enabled = true;
                        }
                        else
                        {
                            LoadBtn.Enabled = true;
                            LoadBtn.Enabled = false;
                        }
                        break;
                    case "Dock":
                        result = Loadport.Dock();
                        if (result == ErrorCode.Success)
                        {
                            DockBtn.Enabled = false;
                            UndockBtn.Enabled = true;
                        }
                        else
                        {
                            DockBtn.Enabled = true;
                            UndockBtn.Enabled = false;
                        }
                        break;
                    case "Clamp":
                        result = Loadport.Clamp();
                        if (result == ErrorCode.Success)
                        {
                            ClampBtn.Enabled = false;
                            CloseDoorBtn.Enabled = true;
                        }
                        else
                        {
                            ClampBtn.Enabled = true;
                            CloseDoorBtn.Enabled = false;
                        }
                        break;
                    case "Unload":
                        result = Loadport.Unload();
                        if (result == ErrorCode.Success)
                        {
                            LoadBtn.Enabled = true;
                            UnloadBtn.Enabled = false;
                        }
                        else
                        {
                            LoadBtn.Enabled = false;
                            LoadBtn.Enabled = true;
                        }
                        break;
                    case "Undock":
                        result = Loadport.Undock();
                        if (result == ErrorCode.Success)
                        {
                            DockBtn.Enabled = true;
                            UndockBtn.Enabled = false;
                        }
                        else
                        {
                            DockBtn.Enabled = false;
                            UndockBtn.Enabled = true;
                        }
                        break;
                    case "Close door":
                        result = Loadport.CloseDoor();
                        if (result == ErrorCode.Success)
                        {
                            ClampBtn.Enabled = true;
                            CloseDoorBtn.Enabled = false;
                        }
                        else
                        {
                            ClampBtn.Enabled = false;
                            CloseDoorBtn.Enabled = true;
                        }
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
            PresenceTxt.DataBindings.Add(nameof(TextBox.Text), _viewModel, _viewModel.Presence);
            ClampTxt.DataBindings.Add(nameof(TextBox.Text), _viewModel, _viewModel.Clamp);
            DoorLatchTxt.DataBindings.Add(nameof(TextBox.Text), _viewModel, _viewModel.DoorLatch);
            VacuumTxt.DataBindings.Add(nameof(TextBox.Text), _viewModel, _viewModel.Vacuum);
            DoorTxt.DataBindings.Add(nameof(TextBox.Text), _viewModel, _viewModel.Door);
            WaferProtrusionTxt.DataBindings.Add(nameof(TextBox.Text), _viewModel, _viewModel.WaferProtrusion);
            DoorTopBottomTxt.DataBindings.Add(nameof(TextBox.Text), _viewModel, _viewModel.DoorTopBottom);
            DockTxt.DataBindings.Add(nameof(TextBox.Text), _viewModel, _viewModel.Docking);
        }
        #endregion Private methods

        #region Public methods
        public void UpdateStatus()
        {

            if (Loadport.ModuleInitialize)
            {
                FoupGB.Enabled = true;
            }
            else
            {
                FoupGB.Enabled = false;
            }
        }

        public void CloseConnection()
        {
            FoupGB.Enabled = false;
        }
        public void InitData(ILoadPortActor loadport)
        {
            var ctx = SynchronizationContext.Current;
            _log = LogUtilityClient.GetUniqueInstance("", 0);
            Loadport = loadport;
            _viewModel = new LoadportFOUPInfoPageViewModel(Loadport, ctx);
            DataBinding();

            if (Loadport.ModuleInitialize)
            {
                FoupGB.Enabled = true;
            }
            else
            {
                FoupGB.Enabled = false;
            }
        }
        #endregion
    }
}
