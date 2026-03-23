using EFEM.DataCenter;
using EFEM.FileUtilities;
using EFEM.GUIControls.GeneralControls.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using TDKController;

namespace EFEM.GUIControls.GeneralControls
{
    public partial class DeviceTreeView : UserControl
    {

        public event EventHandler<ViewRequestEventArgs> ViewRequested;
        public DeviceTreeView()
        {
            InitializeComponent();
            treeView1.AfterSelect += TreeView1_AfterSelect;

        }

        public void CreateTreeNode(Dictionary<string, ILoadPortActor> loadPortModule)
        {
            //AbstractFileUtilities _fu = FileUtility.GetUniqueInstance();
            List<string> dioList = new List<string>();
            List<string> loadPortList = loadPortModule.Keys.ToList();
            List<string> n2NozzleList = new List<string>();
            var node = new TreeNode();
            var loadPort = new TreeNode("LoadPort") { Tag = new ViewRequestEventArgs(NodeViewKind.None) };
            //loadPortList = GetModuleLoadPortList();
            foreach (var item in loadPortList)
            {
                node = new TreeNode(item) { Tag = new ViewRequestEventArgs(NodeViewKind.LoadPort) };
                loadPort.Nodes.Add(node);
            }
            treeView1.Nodes.Add(loadPort);
        }



        private void TreeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            ViewRequestEventArgs args;

            if (e.Node != null && e.Node.Tag is ViewRequestEventArgs ve)
            {
                if (ve.Context == null)
                    args = new ViewRequestEventArgs(ve.Kind, e.Node.Text);
                else
                    args = ve;
            }
            else
            {
                args = new ViewRequestEventArgs(NodeViewKind.LoadPort, e.Node != null ? e.Node.Text : null);
            }

            var handler = ViewRequested;
            if (handler != null)
                handler(this, args);
        }



    }
}
