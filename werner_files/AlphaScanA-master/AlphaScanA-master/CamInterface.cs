using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;

namespace AlphaScan
{
    public partial class CamInterface : UserControl
    {
        private VideoCaptureDevice videoDeviceL;
        private VideoCaptureDevice videoDeviceR;


        public CamInterface()
        {
            InitializeComponent();
        }
    }
}
