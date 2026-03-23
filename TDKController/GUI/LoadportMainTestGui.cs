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
using TDKController.GUI.ViewModels;

namespace TDKController.GUI
{
    public partial class LoadportMainTestGui : UserControl
    {
        #region Fields
        #endregion Fields

        #region Properties
        #endregion Properties

        #region Constructors
        public LoadportMainTestGui()
        {
            InitializeComponent();
            ConnectionPage.ConnectedEvent += ConnectionPageOnConnectedEvent;
            ConnectionPage.DisconnectedEvent += DisonnectionPageOnConnectedEvent;
        }
        #endregion Constructors

        #region GUI Event

        #endregion GUI Event

        #region Private methods
        private void ConnectionPageOnConnectedEvent()
        {
            FOUPInfoPage.UpdateStatus();
            IndicatorPage.UpdateStatus();
            MapperStatusPage.UpdateStatus();
            StatusPage.UpdateStatus();
        }

        private void DisonnectionPageOnConnectedEvent()
        {
            FOUPInfoPage.CloseConnection();
            IndicatorPage.CloseConnection();
            MapperStatusPage.CloseConnection();
            StatusPage.CloseConnection();
        }
        #endregion Private methods

        #region Public methods
        public void InitData(ILoadPortActor loadport)
        {
            if (loadport == null)
                return;
            ConnectionPage.InitData(loadport.Connector);
            FOUPInfoPage.InitData(loadport);
            SlotInfoPage.InitData(loadport);
            IndicatorPage.InitData(loadport);
            MapperStatusPage.InitData(loadport);
            StatusPage.InitData(loadport);
        }
        #endregion Public methods
    }
}
