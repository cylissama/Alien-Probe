namespace AlphaScan
{
    partial class Reader
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
            this.listViewAlerts = new System.Windows.Forms.ListView();
            this.key = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.message = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.panel1 = new System.Windows.Forms.Panel();
            this.labelLastTagScanned = new System.Windows.Forms.Label();
            this.checkBoxScan = new System.Windows.Forms.CheckBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // listViewAlerts
            // 
            this.listViewAlerts.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.key,
            this.message});
            this.listViewAlerts.Location = new System.Drawing.Point(3, 79);
            this.listViewAlerts.Name = "listViewAlerts";
            this.listViewAlerts.Size = new System.Drawing.Size(360, 103);
            this.listViewAlerts.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.listViewAlerts.TabIndex = 25;
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
            // panel1
            // 
            this.panel1.Controls.Add(this.labelLastTagScanned);
            this.panel1.Location = new System.Drawing.Point(3, 26);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(360, 47);
            this.panel1.TabIndex = 24;
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
            // checkBoxScan
            // 
            this.checkBoxScan.AutoSize = true;
            this.checkBoxScan.Location = new System.Drawing.Point(3, 3);
            this.checkBoxScan.Name = "checkBoxScan";
            this.checkBoxScan.Size = new System.Drawing.Size(71, 17);
            this.checkBoxScan.TabIndex = 28;
            this.checkBoxScan.Text = "Scanning";
            this.checkBoxScan.UseVisualStyleBackColor = true;
            this.checkBoxScan.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // Reader
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.checkBoxScan);
            this.Controls.Add(this.listViewAlerts);
            this.Controls.Add(this.panel1);
            this.Name = "Reader";
            this.Size = new System.Drawing.Size(573, 185);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ListView listViewAlerts;
        private System.Windows.Forms.ColumnHeader key;
        private System.Windows.Forms.ColumnHeader message;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label labelLastTagScanned;
        private System.Windows.Forms.CheckBox checkBoxScan;
    }
}
