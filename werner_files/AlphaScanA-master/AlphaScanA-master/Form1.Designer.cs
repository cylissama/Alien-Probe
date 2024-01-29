namespace AlphaScan
{
    partial class Form1
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.button1 = new System.Windows.Forms.Button();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.listViewAlerts = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.label1 = new System.Windows.Forms.Label();
            this.panelSearchMode = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.m6ReaderControl1 = new AlphaScan.M6ReaderControl();
            this.panelCam = new System.Windows.Forms.Panel();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.panelSearchMode.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.settingsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(780, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.settingsToolStripMenuItem.Text = "Settings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.SettingsToolStripMenuItem_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(617, 27);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 3;
            this.button1.Text = "Scan";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.Button1_Click);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Location = new System.Drawing.Point(12, 65);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(537, 185);
            this.flowLayoutPanel1.TabIndex = 5;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 713);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(780, 22);
            this.statusStrip1.TabIndex = 6;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(39, 17);
            this.toolStripStatusLabel1.Text = "Status";
            // 
            // listViewAlerts
            // 
            this.listViewAlerts.AutoArrange = false;
            this.listViewAlerts.CheckBoxes = true;
            this.listViewAlerts.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.listViewAlerts.GridLines = true;
            this.listViewAlerts.Location = new System.Drawing.Point(558, 81);
            this.listViewAlerts.MultiSelect = false;
            this.listViewAlerts.Name = "listViewAlerts";
            this.listViewAlerts.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.listViewAlerts.Size = new System.Drawing.Size(213, 169);
            this.listViewAlerts.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.listViewAlerts.TabIndex = 7;
            this.listViewAlerts.UseCompatibleStateImageBehavior = false;
            this.listViewAlerts.View = System.Windows.Forms.View.Details;
            this.listViewAlerts.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.ListViewAlerts_ColumnClick);
            this.listViewAlerts.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.ListViewAlerts_ItemCheck);
            this.listViewAlerts.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.ListViewAlerts_ItemChecked);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Alert Permits";
            this.columnHeader1.Width = 255;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(555, 65);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(33, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "Alerts";
            // 
            // panelSearchMode
            // 
            this.panelSearchMode.BackColor = System.Drawing.Color.Yellow;
            this.panelSearchMode.Controls.Add(this.label2);
            this.panelSearchMode.Location = new System.Drawing.Point(70, 256);
            this.panelSearchMode.Name = "panelSearchMode";
            this.panelSearchMode.Size = new System.Drawing.Size(589, 23);
            this.panelSearchMode.TabIndex = 11;
            this.panelSearchMode.Visible = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(228, 5);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(120, 17);
            this.label2.TabIndex = 0;
            this.label2.Text = "SEARCH MODE";
            // 
            // m6ReaderControl1
            // 
            this.m6ReaderControl1.Location = new System.Drawing.Point(12, 281);
            this.m6ReaderControl1.Name = "m6ReaderControl1";
            this.m6ReaderControl1.ReaderID = "Reader1";
            this.m6ReaderControl1.ReaderURI = "tmr://pts-ticket-1.dyn.wku.edu";
            this.m6ReaderControl1.Size = new System.Drawing.Size(768, 162);
            this.m6ReaderControl1.TabIndex = 12;
            // 
            // panelCam
            // 
            this.panelCam.Location = new System.Drawing.Point(13, 461);
            this.panelCam.Name = "panelCam";
            this.panelCam.Size = new System.Drawing.Size(755, 235);
            this.panelCam.TabIndex = 13;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(780, 735);
            this.Controls.Add(this.panelCam);
            this.Controls.Add(this.m6ReaderControl1);
            this.Controls.Add(this.panelSearchMode);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listViewAlerts);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "WKU Scanner";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.panelSearchMode.ResumeLayout(false);
            this.panelSearchMode.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
      
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ListView listViewAlerts;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panelSearchMode;
        private System.Windows.Forms.Label label2;
        private M6ReaderControl m6ReaderControl1;
        private System.Windows.Forms.Panel panelCam;
    }
}