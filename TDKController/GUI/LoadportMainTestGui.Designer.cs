namespace TDKController.GUI
{
    partial class LoadportMainTestGui
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.StatusPage = new TDKController.GUI.LoadportStatusPage();
            this.FOUPInfoPage = new TDKController.GUI.LoadportFOUPInfoPage();
            this.SlotInfoPage = new TDKController.GUI.ViewModels.LoadportSlotInfoPage();
            this.MapperStatusPage = new TDKController.GUI.LoadportMapperStatusPage();
            this.IndicatorPage = new TDKController.GUI.LoadportIndicatorPage();
            this.ConnectionPage = new TDKController.GUI.ConnectionPage();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 22F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.Controls.Add(this.StatusPage, 2, 3);
            this.tableLayoutPanel1.Controls.Add(this.FOUPInfoPage, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.SlotInfoPage, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.MapperStatusPage, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.IndicatorPage, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.ConnectionPage, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 7;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28414F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.99971F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.57959F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28414F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28414F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28414F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28414F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1120, 780);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // StatusPage
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.StatusPage, 2);
            this.StatusPage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.StatusPage.Loadport = null;
            this.StatusPage.Location = new System.Drawing.Point(450, 336);
            this.StatusPage.Name = "StatusPage";
            this.tableLayoutPanel1.SetRowSpan(this.StatusPage, 3);
            this.StatusPage.Size = new System.Drawing.Size(667, 327);
            this.StatusPage.TabIndex = 5;
            // 
            // FOUPInfoPage
            // 
            this.FOUPInfoPage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FOUPInfoPage.Loadport = null;
            this.FOUPInfoPage.Location = new System.Drawing.Point(3, 114);
            this.FOUPInfoPage.Name = "FOUPInfoPage";
            this.tableLayoutPanel1.SetRowSpan(this.FOUPInfoPage, 5);
            this.FOUPInfoPage.Size = new System.Drawing.Size(240, 549);
            this.FOUPInfoPage.TabIndex = 1;
            // 
            // SlotInfoPage
            // 
            this.SlotInfoPage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SlotInfoPage.Loadport = null;
            this.SlotInfoPage.Location = new System.Drawing.Point(249, 3);
            this.SlotInfoPage.Name = "SlotInfoPage";
            this.tableLayoutPanel1.SetRowSpan(this.SlotInfoPage, 7);
            this.SlotInfoPage.Size = new System.Drawing.Size(195, 774);
            this.SlotInfoPage.TabIndex = 2;
            // 
            // MapperStatusPage
            // 
            this.MapperStatusPage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MapperStatusPage.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MapperStatusPage.Loadport = null;
            this.MapperStatusPage.Location = new System.Drawing.Point(843, 5);
            this.MapperStatusPage.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MapperStatusPage.Name = "MapperStatusPage";
            this.tableLayoutPanel1.SetRowSpan(this.MapperStatusPage, 2);
            this.MapperStatusPage.Size = new System.Drawing.Size(273, 194);
            this.MapperStatusPage.TabIndex = 4;
            // 
            // IndicatorPage
            // 
            this.IndicatorPage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.IndicatorPage.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.IndicatorPage.Loadport = null;
            this.IndicatorPage.Location = new System.Drawing.Point(451, 5);
            this.IndicatorPage.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.IndicatorPage.Name = "IndicatorPage";
            this.tableLayoutPanel1.SetRowSpan(this.IndicatorPage, 3);
            this.IndicatorPage.Size = new System.Drawing.Size(384, 323);
            this.IndicatorPage.TabIndex = 3;
            // 
            // ConnectionPage
            // 
            this.ConnectionPage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ConnectionPage.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ConnectionPage.Connector = null;
            this.ConnectionPage.Location = new System.Drawing.Point(4, 5);
            this.ConnectionPage.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ConnectionPage.Name = "ConnectionPage";
            this.ConnectionPage.Size = new System.Drawing.Size(238, 101);
            this.ConnectionPage.TabIndex = 6;
            // 
            // LoadportMainTestGui
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "LoadportMainTestGui";
            this.Size = new System.Drawing.Size(1120, 780);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private LoadportStatusPage StatusPage;
        private LoadportFOUPInfoPage FOUPInfoPage;
        private ViewModels.LoadportSlotInfoPage SlotInfoPage;
        private LoadportMapperStatusPage MapperStatusPage;
        private LoadportIndicatorPage IndicatorPage;
        private ConnectionPage ConnectionPage;
    }
}
