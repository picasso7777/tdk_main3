using DevExpress.ClipboardSource.SpreadsheetML;
using LogUtility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TDKLogUtility.Module;

namespace TDKController.GUI.ViewModels
{
    public class LoadportIndicatorPageViewModel : ViewModelBase
    {
        private ILoadPortActor _loadport;
        private ILogUtility _log;
        private const int LED_COUNT = 13;
        public BindingList<Bitmap> SignalList { get; } = new BindingList<Bitmap>();
        public LoadportIndicatorPageViewModel(ILoadPortActor loadport, SynchronizationContext ctx = null, int signalCount = 13) : base(ctx)
        {
            _loadport = loadport;
            _loadport.LedChanged += UpdateEvent;
            _log = LogUtilityClient.GetUniqueInstance("", 0);
            for (int i = 1; i <= signalCount; i++)
            {
                SignalList.Add(Properties.Resources.AxisInputOff);
            }
        }
        public void SetSingal(int index, Bitmap value)
        {
            SignalList[index] = value;
            SignalList.ResetItem(index);
        }
        public ILoadPortActor Loadport
        {
            get => _loadport;
            set
            {
                _loadport.LedChanged -= UpdateEvent;
                _loadport = value;
                _loadport.LedChanged += UpdateEvent;
            }
        }
        private void UpdateEvent(int ledNo, int status)
        {
            try
            {
                //for (int i = 1; i <= LED_COUNT; i++)
                //{
                //    var errorCode = Loadport.GetLedStatus(out var data, i);

                //    if (errorCode == ErrorCode.Success && !string.IsNullOrEmpty(data))
                //    {
                //        var image = data == "0" ? Properties.Resources.AxisInputOff : data == "1" ? Properties.Resources.AxisInputGreen : Properties.Resources.SignalInputGreenBlink;
                //        _viewModel.SetSingal(i - 1, image);
                //    }
                //}
                var image = status == 0 ? Properties.Resources.AxisInputOff : status == 1 ? Properties.Resources.AxisInputGreen : Properties.Resources.SignalInputGreenBlink;
                SetSingal(ledNo - 1, image);
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
