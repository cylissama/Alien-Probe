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
using System.Xml;
using System.Configuration;

namespace AlphaScan
{
    [Serializable]
    public partial class CamControl : UserControl
    {
        public delegate void TriggerPhoto();
       

        private CamConfigSettings settings;
       
        private Bitmap _CurrentFrame;
        private SnapshotForm snapshotForm = null;
        public CamControl()
        {
            InitializeComponent();
        }
        public Bitmap GetImage()
        {
            VideoDevice.SimulateTrigger();
            return _CurrentFrame;
        }
        public void Connect()
        {
            if (videoDevice != null)
            {






                if ((videoDevice.SnapshotCapabilities != null) && (videoDevice.SnapshotCapabilities.Length != 0))
                {
                    videoDevice.ProvideSnapshots = true;
                  //  videoDevice.SnapshotResolution = videoDevice.SnapshotCapabilities[settings.PhotoResolution];
                    videoDevice.SnapshotFrame += new NewFrameEventHandler(VideoDevice_SnapshotFrame);
                }



                videoSourcePlayer.VideoSource = videoDevice;
                videoSourcePlayer.Start();
            }

        }
        public void Disconnect()
        {
            if (videoSourcePlayer.VideoSource != null)
            {
                // stop video device
                videoSourcePlayer.SignalToStop();
                videoSourcePlayer.WaitForStop();
                videoSourcePlayer.VideoSource = null;

                if (videoDevice.ProvideSnapshots)
                {
                    videoDevice.SnapshotFrame -= new NewFrameEventHandler(VideoDevice_SnapshotFrame);
                }

               
            }
        }
        private void VideoDevice_SnapshotFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Console.WriteLine(eventArgs.Frame.Size);
            _CurrentFrame = (Bitmap)eventArgs.Frame.Clone();
            ShowSnapshot(_CurrentFrame);
        }
        private void ShowSnapshot(Bitmap snapshot)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<Bitmap>(ShowSnapshot), snapshot);
            }
            else
            {
                if (snapshotForm == null)
                {
                    snapshotForm = new SnapshotForm();
                    snapshotForm.FormClosed += new FormClosedEventHandler(SnapshotForm_FormClosed);
                    snapshotForm.Show();
                }

                snapshotForm.SetImage(snapshot);
            }
        }
        private void SnapshotForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            snapshotForm = null;
        }
        public VideoCaptureDevice VideoDevice { get => videoDevice; set => videoDevice = value; }
        public SnapshotForm SnapshotForm { get => snapshotForm; set => snapshotForm = value; }



        public string CamSource { get { return videoDevice.Source; } private set { } }
        public int VideoResoultion { get => settings.VideoResolution; set => settings.VideoResolution = value; }
        public int PhotoResolution { get => settings.PhotoResolution; set => settings.PhotoResolution = value; }
     
    }
    [Serializable]
    public class CamConfigSettings
    {
        private string _Moniker;
        private int _VideoResolution;
        private int _PhotoResolution;
        private string _Source;

       public CamConfigSettings() { }

        public string Moniker { get => _Moniker; set => _Moniker = value; }
        public int VideoResolution { get => _VideoResolution; set => _VideoResolution = value; }
        public int PhotoResolution { get => _PhotoResolution; set => _PhotoResolution = value; }
        public string Source { get => _Source; set => _Source = value; }
    }
    
}
