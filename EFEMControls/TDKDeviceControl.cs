using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using Communication.GUI;
using EFEM.FileUtilities;
using EFEM.GUIControls.GeneralControls.Models;
using Microsoft.VisualBasic.ApplicationServices;
using DIO.GUI;
using TDKController;
using TDKController.GUI;
using TDKLogUtility.GUI;

namespace EFEM.GUIControls
{
    public partial class TDKDeviceControl : UserControl
    {
        private UserControl _currentView;
        private readonly Dictionary<NodeViewKind, Func<UserControl>> _viewFactory;
        private List<(string, int)> _commList = new List<(string, int)>();
        private List<string> _loadPortList = new List<string>();
        private List<string> _dioList = new List<string>();
        private List<string> _n2NozzleList = new List<string>();
        private List<(string, UserControl)> _userControlList = new List<(string, UserControl)>();
        private Dictionary<string, ILoadPortActor> _loadPortModule = new Dictionary<string, ILoadPortActor>();

        public TDKDeviceControl()
        {
            InitializeComponent();

            try
            {

                deviceTreeView1.ViewRequested += Nav_ViewRequested;
                AbstractFileUtilities _fu = FileUtility.GetUniqueInstance();
                _commList = _fu.GetCommList();
                _loadPortList = _fu.GetLoadPortList();
                _dioList = _fu.GetDIOList();
                _n2NozzleList = _fu.GetN2NozzleList();

            }

            catch (Exception ex)
            {

            }


        }

        public void InitAll(Dictionary<string, ILoadPortActor> loadPortModule)
        {
            if (this.InvokeRequired)
            {
                MethodInvoker del = delegate { InitAll(loadPortModule); };
                this.Invoke(del);
            }
            else
            {
                RefreshGUI(loadPortModule);
            }
        }

        public void RefreshGUI(Dictionary<string, ILoadPortActor> loadPortModule)
        {
            _loadPortModule = loadPortModule;
            deviceTreeView1.CreateTreeNode(loadPortModule);
        }


        private void Nav_ViewRequested(object sender, ViewRequestEventArgs e)
        {
            ShowView(e.Kind, e.Context);
        }

        private void ShowView(NodeViewKind kind, object context)
        {
            if(_currentView != null)
                _currentView.Visible = false;
            UserControl newView;
            newView = CreateByRule(kind, context);
            if (kind != NodeViewKind.None)
            {
                newView.Visible = true;
                var r = newView as IRefreshable;
                if (r != null)
                    r.RefreshData(context);
            }

            _currentView = newView;
        }


        private UserControl CreateByRule(NodeViewKind kind, object context)
        {
            var selection = context as string;
            var found = _userControlList.FirstOrDefault(x => x.Item1.Equals(selection));
            if (found.Item1 != null)
            {
                return found.Item2;
            }
            else
            {
                UserControl newView;
                switch (kind)
                {
                    //case NodeViewKind.DIO:
                    //    newView = new DIOSettingGUI(selection);
                    //    break;
                    case NodeViewKind.LoadPort:
                        newView = new LoadportMainTestGui();
                        ((LoadportMainTestGui)newView).InitData(_loadPortModule[selection]);
                        break;
                    //case NodeViewKind.N2Nozzle:
                    //    newView = new N2NozzleSettingGUI(selection);
                    //    break;
                    default:
                        return null;
                }
                newView.Visible = false; 
                newView.Dock = DockStyle.Fill;
                panel1.Controls.Add(newView);
                newView.BringToFront();
                _userControlList.Add((selection, newView));
                return newView;
            }
        }

    }
}
