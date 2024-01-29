using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlphaScanCam.UserControls
{
    public partial class ValidateIDFormatSettingsControl : UserControl
    {
        public delegate void UpdateRegExEventHandler(string NewRegEx);
        public event UpdateRegExEventHandler UpdateRegex;
        public ValidateIDFormatSettingsControl()
        {
            InitializeComponent();
            textBox1.Text = Properties.Settings.Default.IDFormatRegEx;

        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.IDFormatRegEx = textBox1.Text;
            Properties.Settings.Default.Save();
            UpdateRegex(textBox1.Text);
        }
    }
}
