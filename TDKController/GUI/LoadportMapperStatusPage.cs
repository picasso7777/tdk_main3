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
    public partial class LoadportMapperStatusPage : UserControl, ILoadportActorPage
    {
        #region Fields
        private LoadportMapperStatusPageViewModel _viewModel;
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
        public LoadportMapperStatusPage()
        {
            InitializeComponent();
        }
        #endregion Constructors

        #region GUI Event
        private void LoadportMapperStatusPage_Load(object sender, EventArgs e)
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
                    case "Scan Slot":
                        result = Loadport.ScanSlotMapStatus(out _);
                        break;

                    case "Return Slot":
                        result = Loadport.ReturnSlotMapStatus(out _);
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
            ArmsPositionTxt.DataBindings.Add("Text", _viewModel, _viewModel.ArmsPosition);
            ZPositionTxt.DataBindings.Add("Text", _viewModel, _viewModel.ZPosition);
            StoperTxt.DataBindings.Add("Text", _viewModel, _viewModel.Stoper);
            MappingStatusTxt.DataBindings.Add("Text", _viewModel, _viewModel.MappingStatus);
        }
        #endregion Private methods

        #region Public methods
        public void UpdateStatus()
        {
            if (Loadport.ModuleInitialize)
            {
                MapperStatusGB.Enabled = true;
            }
            else
            {
                MapperStatusGB.Enabled = false;
            }
        }
        public void CloseConnection()
        {
            MapperStatusGB.Enabled = false;
        }
        public void InitData(ILoadPortActor loadport)
        {
            var ctx = SynchronizationContext.Current;
            _log = LogUtilityClient.GetUniqueInstance("", 0);
            Loadport = loadport;
            _viewModel = new LoadportMapperStatusPageViewModel(Loadport, ctx);
            DataBinding();
            if (Loadport.ModuleInitialize)
            {
                MapperStatusGB.Enabled = true;
            }
            else
            {
                MapperStatusGB.Enabled = false;
            }
        }
        #endregion
    }
}
