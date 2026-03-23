
using System.Drawing;
using System.Threading;
using EFEM.ExceptionManagements;

namespace TDKController.GUI.ViewModels
{
    public class ConnectionPageViewModel : ViewModelBase
    {
        public ConnectionPageViewModel(SynchronizationContext ctx = null) : base(ctx)
        {
        }
        private bool _disconnect;
        public bool Disconnect
        {
            get => _disconnect;
            set
            {
                StatusColor = value ? Color.Green : Color.Red;
                Status = value ? "Connected" : "Disconnected";
                SetProperty(ref _disconnect, value);
            }
        }

        private bool _connect;

        public bool Connect
        {
            get=> _connect;
            set
            {
                StatusColor = value == false ? Color.Green:Color.Red;
                Status = value == false ? "Connected" : "Disconnected";
                SetProperty(ref _connect, value);
            }
        }

        private Color _statusColor;

        public Color StatusColor
        {
            get => _statusColor;
            set
            {
                SetProperty(ref _statusColor, value);
            }
        }

        private string _status;

        public string Status
        {
            get => _status;
            set
            {
                SetProperty(ref _status, value);
            }
        }
    }
}