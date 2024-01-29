namespace AlphaScan
{
    partial class M6ReaderControl
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.labelLastTagScanned = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.antennaView1 = new AlphaScan.AntennaView();
            this.antennaView2 = new AlphaScan.AntennaView();
            this.antennaView3 = new AlphaScan.AntennaView();
            this.antennaView4 = new AlphaScan.AntennaView();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.labelLastTagScanned);
            this.panel1.Location = new System.Drawing.Point(161, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(372, 35);
            this.panel1.TabIndex = 0;
            // 
            // labelLastTagScanned
            // 
            this.labelLastTagScanned.AutoSize = true;
            this.labelLastTagScanned.Location = new System.Drawing.Point(3, 11);
            this.labelLastTagScanned.Name = "labelLastTagScanned";
            this.labelLastTagScanned.Size = new System.Drawing.Size(95, 13);
            this.labelLastTagScanned.TabIndex = 0;
            this.labelLastTagScanned.Text = "Last Tag Scanned";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "label1";
            // 
            // antennaView1
            // 
            this.antennaView1.AntennaID = "Ant 1";
            this.antennaView1.Location = new System.Drawing.Point(474, 44);
            this.antennaView1.Name = "antennaView1";
            this.antennaView1.Size = new System.Drawing.Size(202, 46);
            this.antennaView1.TabIndex = 3;
            // 
            // antennaView2
            // 
            this.antennaView2.AntennaID = "Ant 2";
            this.antennaView2.Location = new System.Drawing.Point(474, 93);
            this.antennaView2.Name = "antennaView2";
            this.antennaView2.Size = new System.Drawing.Size(191, 46);
            this.antennaView2.TabIndex = 4;
            this.antennaView2.Load += new System.EventHandler(this.AntennaView2_Load);
            // 
            // antennaView3
            // 
            this.antennaView3.AntennaID = "Ant 3";
            this.antennaView3.Location = new System.Drawing.Point(0, 44);
            this.antennaView3.Name = "antennaView3";
            this.antennaView3.Size = new System.Drawing.Size(202, 46);
            this.antennaView3.TabIndex = 5;
            // 
            // antennaView4
            // 
            this.antennaView4.AntennaID = "Ant 4";
            this.antennaView4.Location = new System.Drawing.Point(0, 93);
            this.antennaView4.Name = "antennaView4";
            this.antennaView4.Size = new System.Drawing.Size(202, 46);
            this.antennaView4.TabIndex = 6;
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(208, 44);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(251, 95);
            this.listBox1.TabIndex = 7;
            // 
            // M6ReaderControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.antennaView4);
            this.Controls.Add(this.antennaView3);
            this.Controls.Add(this.antennaView2);
            this.Controls.Add(this.antennaView1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.panel1);
            this.Name = "M6ReaderControl";
            this.Size = new System.Drawing.Size(688, 289);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label labelLastTagScanned;
        private System.Windows.Forms.Label label1;
        private AntennaView antennaView1;
        private AntennaView antennaView2;
        private AntennaView antennaView3;
        private AntennaView antennaView4;
        private System.Windows.Forms.ListBox listBox1;
    }
}
