using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlphaScan
{
    public partial class SettingsForm : Form
    {
        Form1 _parentForm;
        CamSettings cs = new CamSettings();
        public SettingsForm(Form1 parent)
        {
            InitializeComponent();
            _parentForm = parent;
            // add the settings controls to the form
            tabControl1.TabPages.Clear();
            TabPage tp = new TabPage("Readers");
            tp.Controls.Add(new ReaderSettingsControl( _parentForm.ReaderList));
            tabControl1.TabPages.Add(tp);
            foreach (ITagFunction tf in _parentForm.TagFunctionsList)
            {
                tp = new TabPage(tf.GetDisplayName());
                tp.Controls.Add(tf.GetSettingsControl());
                tabControl1.TabPages.Add(tp);
                
            }
            tp = new TabPage("Cam Settings");
            

            cs.ConfigSettings = _parentForm.Clist;
            
            tp.Controls.Add(cs);
            tabControl1.TabPages.Add(tp);


        }

        private void Button1_Click(object sender, EventArgs e)
        {
            cs.Disconnect();
            this.Close();
        }

        private void SettingsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Properties.Settings.Default.Save();
        }
    }
}
