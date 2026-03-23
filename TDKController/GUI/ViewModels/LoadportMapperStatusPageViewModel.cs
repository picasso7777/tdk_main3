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
    internal class LoadportMapperStatusPageViewModel : ViewModelBase
    {
        private ILoadPortActor _loadport;
        private ILogUtility _log;
        public LoadportMapperStatusPageViewModel(ILoadPortActor loadport, SynchronizationContext ctx = null) : base(ctx)
        {
            _loadport = loadport;
            _loadport.StatusChanged += UpdateEvent;
            _log = LogUtilityClient.GetUniqueInstance("", 0);
        }

        private string _armsPosition;

        public string ArmsPosition
        {
            get => _armsPosition;
            set => SetProperty(ref _armsPosition, value);
        }
        private string _zPosition;

        public string ZPosition
        {
            get => _zPosition;
            set => SetProperty(ref _zPosition, value);
        }
        private string _stoper;

        public string Stoper
        {
            get => _stoper;
            set => SetProperty(ref _stoper, value);
        }
        private string _mappingStatus;

        public string MappingStatus
        {
            get => _mappingStatus;
            set => SetProperty(ref _mappingStatus, value);
        }
        public ILoadPortActor Loadport
        {
            get => _loadport;
            set
            {
                _loadport.StatusChanged -= UpdateEvent;
                _loadport = value;
                _loadport.StatusChanged += UpdateEvent;
            }
        }
        private void UpdateEvent(LoadportStatus newStatus)
        {
            try
            {
                ArmsPosition = _loadport.Status.MpArmPos == '0' ? "open" : _loadport.Status.MpArmPos == '1' ? "close" : "undefined";
                ZPosition = _loadport.Status.MpZPos == '0' ? "retract" : _loadport.Status.MpZPos == '1' ? "mapping" : "undefined";
                Stoper = _loadport.Status.MpStopper == '0' ? "on" : _loadport.Status.MpStopper == '1' ? "off" : "undefined";
                MappingStatus = _loadport.Status.MappingStatus == '0' ? "unmapped" : _loadport.Status.MappingStatus == '1' ? "mapped" : "map failed";
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
