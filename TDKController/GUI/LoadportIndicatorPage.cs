using Communication.Connector;
using Communication.Protocol;
using EFEM.FileUtilities;
using LogUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TDKController.GUI.ViewModels;
using TDKController.Interface;
using TDKLogUtility.Module;

namespace TDKController.GUI
{
    public partial class LoadportIndicatorPage : UserControl, ILoadportActorPage
    {
        #region Fields
        private LoadportIndicatorPageViewModel _viewModel;
        private ILogUtility _log;
        private const int LED_COUNT = 10;
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
        public LoadportIndicatorPage()
        {
            InitializeComponent();
        }
        #endregion Constructors

        #region GUI Event
        private void LoadportIndicatorPage_Load(object sender, EventArgs e)
        {
           
        }
        private void Setting_SelectedIndexChanged(object sender, EventArgs e)
        {
            var index = Convert.ToInt32(((ComboBox)sender).Tag);
            var selectedIndex = ((ComboBox)sender).SelectedIndex;
            if (!Loadport.ModuleInitialize)
            {
                MessageBox.Show(@"Loadport is not connected yet, please connect first.");
                _log.WriteLog("TDKGUI", LogHeadType.Error, $"Loadport not connected, user action Comboxbox method:Indicator {index} Select index{selectedIndex}");
                return;
            }
            _log.WriteLog("TDKGUI", LogHeadType.Info, $"user action Comboxbox method:Indicator {index} Select index{selectedIndex}");

            try
            {
                ErrorCode errorCode = ErrorCode.Success;
                switch (selectedIndex)
                {
                    case 0:
                        errorCode = Loadport.LedOn(index);

                        break;
                    case 1:
                        errorCode = Loadport.LedOff(index);

                        break;
                    case 2:
                        errorCode = Loadport.LedBlink(index);

                        break;
                }

                if (errorCode != ErrorCode.Success)
                {
                    _log.WriteLog("TDKGUI", LogHeadType.Error, $"user action Comboxbox method:Indicator {index} Select index{selectedIndex} error. Set LED-{index} error, error code = {(int)errorCode}");
                    MessageBox.Show($@"Set LED-{index} error, error code = {(int) errorCode}");
                }
            }
            catch (Exception ex)
            {
                _log.WriteLog("TDKGUI", LogHeadType.Error, $"user action Comboxbox method:Indicator {index} Select index{selectedIndex} error. Error message:{ex.Message}");
            }

            _log.WriteLog("TDKGUI", LogHeadType.Info, $"user action Comboxbox method:Indicator {index} Select index{selectedIndex} finish");
        }
        #endregion GUI Event

        #region Private methods

        private void DataBinding()
        {
            var signalPictureBoxList = GetAllPictureBoxes(ContentTLP)
                                   .OrderBy(tb => Convert.ToInt32(tb.Tag))
                                   .ToList();
            for (int i = 0; i < signalPictureBoxList.Count; i++)
            {
                var pb = signalPictureBoxList[i];
                var imageBs = new BindingSource { DataSource = _viewModel.SignalList };
                imageBs.Position = i;
                pb.DataBindings.Add(new Binding("Image", imageBs, "", true, DataSourceUpdateMode.OnPropertyChanged));
            }
        }

        private List<PictureBox> GetAllPictureBoxes(TableLayoutPanel tlp)
        {
            List<PictureBox> result = new List<PictureBox>();

            void SearchControls(Control parent)
            {
                foreach (Control c in parent.Controls)
                {
                    if (c is PictureBox tb)
                    {
                        result.Add(tb);
                    }
                    else if (c.HasChildren)
                    {
                        SearchControls(c);
                    }
                }
            }
            SearchControls(tlp);
            return result;
        }
        #endregion Private methods

        #region Public methods
        public void UpdateStatus()
        {
            if (Loadport.ModuleInitialize)
            {
                IndicatorsGB.Enabled = true;
            }
            else
            {
                IndicatorsGB.Enabled = false;
            }
            
        }
        public void CloseConnection()
        {
            IndicatorsGB.Enabled = false;
        }
        public void InitData(ILoadPortActor loadport)
        {
            var ctx = SynchronizationContext.Current;
            _log = LogUtilityClient.GetUniqueInstance("", 0);
            Loadport = loadport;
            _viewModel = new LoadportIndicatorPageViewModel(Loadport, ctx);
            DataBinding();
            if (Loadport.ModuleInitialize)
            {
                IndicatorsGB.Enabled = true;
            }
            else
            {
                IndicatorsGB.Enabled = false;
            }
        }
        #endregion Public methods
    }
}
