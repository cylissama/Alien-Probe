using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;

namespace AlphaScan
{
    public partial class ReaderSettings : UserControl
    {
        Reader _parent;
        static int rowcount = 0;
        static int colcount = 0;
        private bool _ActiveReader;

        private string _Port;

        public string Port
        {
            get { return _Port; }
            set { _Port = value; }
        }

        public bool ActiveReader
        {
            get { return _ActiveReader; }
            set { _ActiveReader = value; }
        }

        public ReaderSettings(SerialPort PortToOpen, Reader parent)
        {
            InitializeComponent();
            _Port = PortToOpen;
            this.label1.Text = PortToOpen;
            _parent = parent;
            domainUpDown1.Text = rowcount.ToString();
            domainUpDown2.Text = colcount.ToString();
            rowcount += 1;
            if (rowcount > 1)
            {
                colcount += 1;
                rowcount = 0;
            }
        }

        private void checkBoxUseThisReader_CheckedChanged(object sender, EventArgs e)
        {
            _ActiveReader = checkBoxUseThisReader.Checked;
        }

        private void domainUpDown1_SelectedItemChanged(object sender, EventArgs e)
        {

        }
    }
}
