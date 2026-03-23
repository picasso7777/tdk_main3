using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EFEM.DataCenter;
using EFEM.FileUtilities;

namespace Communication.GUI.ViewModels
{
    class TCPConfigGuiViewModel : ViewModelBase
    {
        private TCPConfig _config;
        public TCPConfigGuiViewModel(TCPConfig config, SynchronizationContext ctx = null) : base(ctx)
        {
            _config = config;
            IpAddress = config.Ip;
            Port = config.Port;
            config.DataChanged += ConfigDataChanged;
        }

        private string _ipAddress;
        public string IpAddress
        {
            get => _ipAddress;
            set
            {
                if(_ipAddress == value) return;

                _config.Ip = value;
                SetProperty(ref _ipAddress, value);
            }
        }

        private string _port;
        public string Port
        {
            get => _port;
            set
            {
                if (_port == value) return;
                _config.Port = value;
                SetProperty(ref _port, value);
            }
        }

        private void ConfigDataChanged()
        {
            SetProperty(ref _ipAddress, _config.Ip);
            SetProperty(ref _port, _config.Port);
        }
    }
}
