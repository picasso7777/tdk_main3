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
using System.Windows.Forms.VisualStyles;
using Communication.Connector;
using Communication.Connector.Enum;
using Communication.Interface;
using Communication.Protocol;
using EFEM.FileUtilities;
using LogUtility;
using TDKController.GUI.ViewModels;
using TDKController.Interface;
using TDKLogUtility.Module;

namespace TDKController.GUI
{
    public delegate void ConnectedEventHandler();
    public delegate void DisonnectedEventHandler();
    public partial class ConnectionPage : UserControl
    {
        #region Fields
        private ConnectionPageViewModel _viewModel;
        private ILogUtility _log;
        #endregion Fields

        #region Event

        public event ConnectedEventHandler ConnectedEvent;
        public event DisonnectedEventHandler DisconnectedEvent;
        #endregion Event

        #region Property
        public IConnector Connector { get; set; }
        #endregion Property

        #region Constructors
        public ConnectionPage()
        {
            InitializeComponent();
        }
        
        #endregion Constructors

        #region GUI Event
        private void LoadportConnectionPage_Load(object sender, EventArgs e)
        {
            
        }

        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            try
            {
                var button = (Button)sender;
                HRESULT result = null;
                if (button.Text == @"Connect")
                {
                    result = Connector.Connect();
                }
                else
                {
                    Connector.Disconnect();
                }

                if (result == null)
                {
                    _viewModel.Connect = true;
                    _viewModel.Disconnect = false;

                    if (button.Text == @"Connect")
                    {
                        ConnectedEvent?.Invoke();
                    }
                    else
                    {
                        DisconnectedEvent?.Invoke();
                    }
                }
                else
                {
                    _viewModel.Connect = false;
                    _viewModel.Disconnect = true;
                }
            }
            catch (Exception ex)
            {
                _log.WriteLog("TDKGUI", ex.Message);
            }
        }
        #endregion GUI Event

        #region Public method
        public void InitData(IConnector connector)
        {
            var _ctx = SynchronizationContext.Current;
            _log = LogUtilityClient.GetUniqueInstance("", 0);
            Connector = connector;
            _viewModel = new ConnectionPageViewModel( _ctx);

            DataBinding();
            _viewModel.Connect = Connector.IsConnected;
            _viewModel.Disconnect = !Connector.IsConnected;

            if (Connector.IsConnected)
            {
                _viewModel.Connect = false;
                _viewModel.Disconnect = true;
            }
            else
            {
                _viewModel.Connect = true;
                _viewModel.Disconnect = false;
            }
        }
        #endregion Public method

        #region Private methods

        private void DataBinding()
        {
            ConnectBtn.DataBindings.Add(nameof(Button.Enabled), _viewModel, nameof(_viewModel.Connect));
            DisconnectBtn.DataBindings.Add(nameof(Button.Enabled), _viewModel, nameof(_viewModel.Disconnect));
            StatusTxt.DataBindings.Add(nameof(TextBox.ForeColor), _viewModel, nameof(_viewModel.StatusColor));
            StatusTxt.DataBindings.Add(nameof(TextBox.Text), _viewModel, nameof(_viewModel.Status));
        }
        #endregion Private methods

        
    }
}
