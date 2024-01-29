using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ThingMagic;
using System.Collections.Specialized;
using System.Threading;

namespace AlphaScan
{
    public partial class ReaderSettingsControl : UserControl
    {
        Dictionary<string, Reader> _Readerlist;
        string _ReaderURI;

        public string ReaderURI { get => _ReaderURI; set => _ReaderURI = value; }

        public ReaderSettingsControl()
        {
            InitializeComponent();
        }
        public ReaderSettingsControl(Dictionary<string, Reader> Readerlist)
        {
            InitializeComponent();

            if (null == Properties.Settings.Default.ReaderURIs) return;
            ReaderURI = Properties.Settings.Default.ReaderURIs;
            this.textBox1.Text = ReaderURI;
            checkBoxUseSimulator.Checked = Properties.Settings.Default.NoReaderMode;
            
        }
      

      

        private void Button2_Click(object sender, EventArgs e)
        {
            ReaderURI = this.textBox1.Text;
            Properties.Settings.Default.ReaderURIs = ReaderURI;
            Properties.Settings.Default.Save();
        }

        private void CheckBoxUseSimulator_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.NoReaderMode = checkBoxUseSimulator.Checked;
            Properties.Settings.Default.Save();
        }
    }
}

