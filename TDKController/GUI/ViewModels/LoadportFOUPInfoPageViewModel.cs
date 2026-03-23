using LogUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TDKLogUtility.Module;

namespace TDKController.GUI.ViewModels
{
    public class LoadportFOUPInfoPageViewModel : ViewModelBase
    {
        private ILoadPortActor _loadport;
        private ILogUtility _log;
        public LoadportFOUPInfoPageViewModel(ILoadPortActor loadport, SynchronizationContext ctx = null) : base(ctx)
        {
            _loadport = loadport;
            _loadport.FoupStatusChanged += UpdateEvent;
            _log = LogUtilityClient.GetUniqueInstance("", 0);
        }

        private string _presence;

        public string Presence
        {
            get => _presence;
            set => SetProperty(ref _presence, value);
        }

        private string _clamp;

        public string Clamp
        {
            get => _clamp;
            set => SetProperty(ref _clamp, value);
        }

        private string _doorLatch;

        public string DoorLatch
        {
            get => _doorLatch;
            set => SetProperty(ref _doorLatch, value);
        }

        private string _vacuum;

        public string Vacuum
        {
            get => _vacuum;
            set => SetProperty(ref _vacuum, value);
        }

        private string _door;

        public string Door
        {
            get => _door;
            set => SetProperty(ref _door, value);
        }

        private string _waferProtrusion;

        public string WaferProtrusion
        {
            get => _waferProtrusion;
            set => SetProperty(ref _waferProtrusion, value);
        }

        private string _doorTopBottom;

        public string DoorTopBottom
        {
            get => _doorTopBottom;

            set => SetProperty(ref _doorTopBottom, value);
        }

        private string _docking;

        public string Docking
        {
            get => _docking;
            set => SetProperty(ref _docking, value);
        }

        public ILoadPortActor Loadport
        {
            get => _loadport;
            set
            {
                _loadport.FoupStatusChanged -= UpdateEvent;
                _loadport = value;
                _loadport.FoupStatusChanged += UpdateEvent;
            }
        }
        public void UpdateEvent()
        {
            try
            {
                Presence = _loadport.Status.FpPlace == '0' ? "absent" : _loadport.Status.FpPlace == '1' ? "placed" : "misplaced";
                Clamp = _loadport.Status.FpClamp == '0' ? "open" : _loadport.Status.FpClamp == '1' ? "clamped" : "undefined";
                DoorLatch = _loadport.Status.LtchKey == '0' ? "open" : _loadport.Status.FpClamp == '1' ? "close" : "undefined";
                Vacuum = _loadport.Status.Vacuum == '0' ? "off" : "on";
                Door = _loadport.Status.FpDoor == '0' ? "open" : _loadport.Status.FpDoor == '1' ? "close" : "undefined";
                WaferProtrusion = _loadport.Status.WfProtrusion == '0' ? "blocked" : "unblocked";
                DoorTopBottom = _loadport.Status.ZPos == '0' ? "up" : _loadport.Status.ZPos == '1' ? "down" : _loadport.Status.ZPos == '2' ? "Start Position" : _loadport.Status.ZPos == '3' ? "End Position" : "undefined";
                Docking = _loadport.Status.YPos == '0' ? "undock" : _loadport.Status.YPos == '1' ? "dock" : "undefined";
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _log.WriteLog("TDKGUI", ex.Message);
            }
        }
}
}
