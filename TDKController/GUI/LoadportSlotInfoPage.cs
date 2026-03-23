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
using TDKController.Interface;
using TDKLogUtility.Module;

namespace TDKController.GUI.ViewModels
{
    public partial class LoadportSlotInfoPage : UserControl, ILoadportActorPage
    {
        #region Fields
        private LoadportSlotInfoPageViewModel _viewModel;
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
        public LoadportSlotInfoPage()
        {
            InitializeComponent();
        }
        #endregion Constructors

        #region GUI Event
        private void LoadportSlotInfoPage_Load(object sender, EventArgs e)
        {
            
        }
        #endregion GUI Event

        #region Private methods
        /// <summary>
        /// MVVM data binding method.
        /// </summary>
        private void DataBinding()
        {
            //get all textbox
            var slotsTextBoxList = GetAllTextBoxes(ContentTLP)
                                   .OrderBy(tb => Convert.ToInt32(tb.Tag))
                                   .ToList();
            //binding view model value to textbox
            for (int i = 0; i < slotsTextBoxList.Count; i++)
            {
                var tb = slotsTextBoxList[i];

                // use binding source bind data to gui.
                var statusBs = new BindingSource { DataSource = _viewModel.SlotsStatus };
                statusBs.Position = i;
                tb.DataBindings.Add(new Binding("Text", statusBs, "", true, DataSourceUpdateMode.OnPropertyChanged));

                var colorBs = new BindingSource { DataSource = _viewModel.SlotsColor };
                colorBs.Position = i;
                tb.DataBindings.Add(new Binding("BackColor", colorBs, "", true, DataSourceUpdateMode.OnPropertyChanged));

            }

        }
        /// <summary>
        /// Get all textbox from table layout panel.
        /// </summary>
        /// <param name="tlp">control table layout</param>
        /// <returns>all textbox list</returns>
        private List<TextBox> GetAllTextBoxes(TableLayoutPanel tlp)
        {
            List<TextBox> result = new List<TextBox>();
            // Search text box control from parent
            void SearchControls(Control parent)
            {
                foreach (Control c in parent.Controls)
                {
                    if (c is TextBox tb)
                    {
                        result.Add(tb);
                    }
                    else if (c.HasChildren) //if have child, should research again.
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
        public void InitData(ILoadPortActor loadport)
        {
            var ctx = SynchronizationContext.Current;
            _log = LogUtilityClient.GetUniqueInstance("", 0);
            Loadport = loadport;
            _viewModel = new LoadportSlotInfoPageViewModel(Loadport, ctx);
            DataBinding();
        }
        #endregion Public methods
    }
}
