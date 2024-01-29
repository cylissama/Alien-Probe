using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using AlphaScanCam.Functions.Implement;
using AlphaScanCam.Util;

namespace AlphaScanCam
{
    public partial class BlacklistControl : UserControl
    {
        

        public BlacklistControl()
        {
            InitializeComponent();
            
        }
        public void SetLabel(string message)
        {
           
            this.label1.Text = message;
        }
        protected virtual void TagAlert(TagEventArgs e)
        {

        }
        public event EventHandler<TagEventArgs> Setlabel;

    }
}
