using DevExpress.Charts.Model;
using DevExpress.Export.Xl;
using DevExpress.XtraSpellChecker.Parser;
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
    public class LoadportStatusPageViewModel : ViewModelBase
    {
        private ILoadPortActor _loadport;
        private ILogUtility _log;

        
        public LoadportStatusPageViewModel(ILoadPortActor loadport, SynchronizationContext ctx = null) : base(ctx)
        {
            _loadport = loadport;
            _loadport.StatusChanged += UpdateEvent;
            _log = LogUtilityClient.GetUniqueInstance("", 0);
            _lpStatus = "OFF";
        }

        private string _initialStatus;

        public string InitialStatus
        {
            get=> _initialStatus;
            set => SetProperty(ref _initialStatus, value);
        }
        private string _lpStatus;

        public string LpStatus
        {
            get => _lpStatus;
            set => SetProperty(ref _lpStatus, value);
        }
        private string _eqpStatus;

        public string EqpStatus
        {
            get => _eqpStatus;
            set => SetProperty(ref _eqpStatus, value);
        }
        private string _eqpMode;

        public string EqpMode
        {
            get => _eqpMode;
            set => SetProperty(ref _eqpMode, value);
        }
        private string _operationStatus;

        public string OperationStatus
        {
            get => _operationStatus;
            set => SetProperty(ref _operationStatus, value);
        }
        private string _errorCode;

        public string ErrorCode
        {
            get => _errorCode;
            set => SetProperty(ref _errorCode, value);
        }
        private string _softwareInterlock;

        public string SoftwareInterlock
        {
            get => _softwareInterlock;
            set => SetProperty(ref _softwareInterlock, value);
        }
        private string _infoPad;

        public string InfoPad
        {
            get => _infoPad;
            set => SetProperty(ref _infoPad, value);
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
                InitialStatus = Loadport.Status.Inited == '0' ? "not init" : "Initialized";
                EqpStatus = Loadport.Status.EqpStatus == '0' ? "normal" : Loadport.Status.EqpStatus == 'A' ? "recoverable error" : "fatal error";
                EqpMode = Loadport.Status.Mode == '0' ? "online" : "undefined";
                OperationStatus = Loadport.Status.OpStatus == '0' ? "stopped" : "operating";
                ErrorCode = $"{(char)Loadport.Status.Ecode}";
                SoftwareInterlock = Loadport.Status.IntKey == '0' ? "enable" : "disable";
                InfoPad = Loadport.Status.InfoPad == '0' ? "no input" : Loadport.Status.InfoPad == '1' ? "A-pin on" : Loadport.Status.InfoPad == '2' ? "B-pin on" : Loadport.Status.InfoPad == '3' ? "both on" : "undefined";
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
