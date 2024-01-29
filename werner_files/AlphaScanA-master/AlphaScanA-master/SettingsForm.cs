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
        public SettingsForm(Form1 parent)
        {
            InitializeComponent();
            _parentForm = parent;
            // add the settings controls to the form
            tabControl1.TabPages.Clear();
            TabPage tp = new TabPage("Readers");
            tp.Controls.Add(new ReaderSettingsControl());
            tabControl1.TabPages.Add(tp);
            foreach (ITagFunction tf in _parentForm.TagFunctionsList)
            {
                tp = new TabPage(tf.GetDisplayName());
                tp.Controls.Add(tf.GetSettingsControl());
                tabControl1.TabPages.Add(tp);
                
            }
           

        }

        private void Button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SettingsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Properties.Settings.Default.Save();
        }
    }
}
