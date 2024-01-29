using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
namespace AlphaScan
{
    public partial class SettingsFormX 
    {
        private FlowLayoutPanel flowLayoutPanel1;
        private FlowLayoutPanel flowLayoutPanel2;
        private Button button1;
        Form1 _parentForm;
      
       
        public SettingsFormX(Form1 parent)
        {
            InitializeComponent();
            _parentForm = parent;
            // add the settings controls to the form

            foreach( TagFunction tf in _parentForm.TagFunctionsList)
            {
                this.flowLayoutPanel1.Controls.Add(tf.getSettingsControl());
            }
        }
      
            
            /* if (_parentForm.ReaderList.Count < 1)
            {
                MessageBox.Show("No Readers Found. Ensure readers are connected and powered on.");
                _parentForm.Close();
                this.Close();
                Application.Exit();
            }
          */
      
           // string port = Properties.Settings.Default.SelectedPort;
           // comboBoxPorts.Text = port;
       

        private void Button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SettingsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            
            Properties.Settings.Default.Save();
            
        }

        private void InitializeComponent()
        {
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Location = new System.Drawing.Point(78, 98);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(200, 100);
            this.flowLayoutPanel1.TabIndex = 0;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Location = new System.Drawing.Point(78, 439);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(200, 100);
            this.flowLayoutPanel2.TabIndex = 1;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(603, 75);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // SettingsForm
            // 
            this.ClientSize = new System.Drawing.Size(803, 706);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.flowLayoutPanel2);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Name = "SettingsForm";
            this.ResumeLayout(false);

        }
    }
}
