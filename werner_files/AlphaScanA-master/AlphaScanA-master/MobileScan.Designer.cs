namespace AlphaScan
{
    partial class MobileScan
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
            this.components = new System.ComponentModel.Container();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.buttonScanPause = new System.Windows.Forms.Button();
            this.labelLastTagScanned = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.paneluserControls = new System.Windows.Forms.Panel();
            this.listViewAlerts = new System.Windows.Forms.ListView();
            this.key = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.message = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.buttonFind = new System.Windows.Forms.Button();
            this.buttonIgnore = new System.Windows.Forms.Button();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.serialPort1 = new System.IO.Ports.SerialPort(this.components);
            this.menuStrip1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.settingsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(569, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.settingsToolStripMenuItem.Text = "Settings";
           
            // 
            // buttonScanPause
            // 
            this.buttonScanPause.Location = new System.Drawing.Point(223, 90);
            this.buttonScanPause.Name = "buttonScanPause";
            this.buttonScanPause.Size = new System.Drawing.Size(75, 23);
            this.buttonScanPause.TabIndex = 1;
            this.buttonScanPause.Text = "Scan";
            this.buttonScanPause.UseVisualStyleBackColor = true;
            this.buttonScanPause.Click += new System.EventHandler(this.buttonScanPause_Click);
            // 
            // labelLastTagScanned
            // 
            this.labelLastTagScanned.AutoSize = true;
            this.labelLastTagScanned.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelLastTagScanned.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelLastTagScanned.Location = new System.Drawing.Point(36, 15);
            this.labelLastTagScanned.Name = "labelLastTagScanned";
            this.labelLastTagScanned.Size = new System.Drawing.Size(54, 20);
            this.labelLastTagScanned.TabIndex = 2;
            this.labelLastTagScanned.Text = "Permit";
            this.labelLastTagScanned.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.labelLastTagScanned);
            this.panel1.Location = new System.Drawing.Point(12, 37);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(545, 47);
            this.panel1.TabIndex = 3;
            // 
            // paneluserControls
            // 
            this.paneluserControls.Location = new System.Drawing.Point(12, 249);
            this.paneluserControls.Name = "paneluserControls";
            this.paneluserControls.Size = new System.Drawing.Size(545, 239);
            this.paneluserControls.TabIndex = 5;
            // 
            // listViewAlerts
            // 
            this.listViewAlerts.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.key,
            this.message});
            this.listViewAlerts.Location = new System.Drawing.Point(12, 140);
            this.listViewAlerts.Name = "listViewAlerts";
            this.listViewAlerts.Size = new System.Drawing.Size(328, 103);
            this.listViewAlerts.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.listViewAlerts.TabIndex = 20;
            this.listViewAlerts.UseCompatibleStateImageBehavior = false;
            this.listViewAlerts.View = System.Windows.Forms.View.Details;
            // 
            // key
            // 
            this.key.Text = "Permit";
            this.key.Width = 118;
            // 
            // message
            // 
            this.message.Text = "Message";
            this.message.Width = 194;
            // 
            // buttonFind
            // 
            this.buttonFind.Location = new System.Drawing.Point(346, 166);
            this.buttonFind.Name = "buttonFind";
            this.buttonFind.Size = new System.Drawing.Size(118, 23);
            this.buttonFind.TabIndex = 21;
            this.buttonFind.Text = "Find Selected";
            this.buttonFind.UseVisualStyleBackColor = true;
            this.buttonFind.Click += new System.EventHandler(this.buttonFind_Click);
            // 
            // buttonIgnore
            // 
            this.buttonIgnore.Location = new System.Drawing.Point(347, 207);
            this.buttonIgnore.Name = "buttonIgnore";
            this.buttonIgnore.Size = new System.Drawing.Size(117, 23);
            this.buttonIgnore.TabIndex = 22;
            this.buttonIgnore.Text = "Ignore Selected";
            this.buttonIgnore.UseVisualStyleBackColor = true;
            this.buttonIgnore.Click += new System.EventHandler(this.buttonIgnore_Click);
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(52, 511);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(426, 95);
            this.listBox1.TabIndex = 23;
            // 
            // MobileScan
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(569, 698);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.buttonIgnore);
            this.Controls.Add(this.buttonFind);
            this.Controls.Add(this.listViewAlerts);
            this.Controls.Add(this.paneluserControls);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.buttonScanPause);
            this.Controls.Add(this.menuStrip1);
            
            this.Name = "MobileScan";
            this.Text = "Mobile Tag Scan";
            
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.Button buttonScanPause;
        private System.Windows.Forms.Label labelLastTagScanned;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel paneluserControls;
        private System.Windows.Forms.ListView listViewAlerts;
        private System.Windows.Forms.ColumnHeader key;
        private System.Windows.Forms.ColumnHeader message;
        private System.Windows.Forms.Button buttonFind;
        private System.Windows.Forms.Button buttonIgnore;
        private System.Windows.Forms.ListBox listBox1;
        private System.IO.Ports.SerialPort serialPort1;
    }

}