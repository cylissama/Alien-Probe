using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace AlphaScan
{
    public partial class BlacklistControl : UserControl
    {
        BlacklistedTagFunction _BlacklistFunction;

        public BlacklistControl(BlacklistedTagFunction BlacklistFunction)
        {
            InitializeComponent();
            _BlacklistFunction = BlacklistFunction;
        }
        public void SetLabel(string lbl)
        {
            this.label1.Text = lbl;
        }

    }
}
