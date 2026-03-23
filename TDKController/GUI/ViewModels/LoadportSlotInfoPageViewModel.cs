using DevExpress.Charts.Model;
using DevExpress.Export.Xl;
using DevExpress.XtraSpellChecker.Parser;
using LogUtility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TDKLogUtility.Module;

namespace TDKController.GUI.ViewModels
{
    public class LoadportSlotInfoPageViewModel :ViewModelBase
    {
        private ILoadPortActor _loadport;
        private ILogUtility _log;
        public LoadportSlotInfoPageViewModel(ILoadPortActor loadport, SynchronizationContext ctx = null, int slotCount = 25) : base(ctx)
        {
            _loadport = loadport;
            _loadport.SlotMapScanned += UpdateEvent;
            _log = LogUtilityClient.GetUniqueInstance("", 0);
            for (int i = 1; i <= slotCount; i++)
            {
                SlotsStatus.Add("OCCUPIED");
                SlotsColor.Add(Color.Tomato);
            }
            
        }
        public BindingList<string> SlotsStatus { get; } = new BindingList<string>();
        public BindingList<Color> SlotsColor { get; } = new BindingList<Color>();

        public void SetSlotsStatus(int index, string value)
        {
            SlotsStatus[index] = value;
            SlotsStatus.ResetItem(index);
        }
        public void SetSlotsColor(int index, Color value)
        {
            SlotsColor[index] = value;
            SlotsColor.ResetItem(index);
        }
        public ILoadPortActor Loadport
        {
            get => _loadport;
            set
            {
                _loadport.SlotMapScanned -= UpdateEvent;
                _loadport = value;
                _loadport.SlotMapScanned += UpdateEvent;
            }
        }
        private void UpdateEvent(int[] slotMap)
        {
            try
            {
                for (int i = 0; i < slotMap.Length; i++)
                {
                    var respSlot = slotMap[i];

                    switch (respSlot)
                    {
                        case 0:
                            SetSlotsColor(i, Color.Tomato);
                            SetSlotsStatus(i, "UNDEFINED");
                            break;
                        case 1:
                            SetSlotsColor(i, Color.Gray);
                            SetSlotsStatus(i, "EMPTY");
                            break;
                        case 2:
                            SetSlotsColor(i, Color.Tomato);
                            SetSlotsStatus(i, "NOT EMPTY");
                            break;
                        case 3:
                            SetSlotsColor(i, Color.YellowGreen);
                            SetSlotsStatus(i, "OCCUPIED");
                            break;
                        case 4:
                            SetSlotsColor(i, Color.Tomato);
                            SetSlotsStatus(i, "DOUBLE SLOTTED");
                            break;
                        case 5:
                            SetSlotsColor(i, Color.Tomato);
                            SetSlotsStatus(i, "CROSS SLOTTED");
                            break;
                    }
                }
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
