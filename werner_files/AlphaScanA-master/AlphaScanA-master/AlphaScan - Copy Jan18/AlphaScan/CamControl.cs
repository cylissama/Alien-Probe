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
using System.Xml.Serialization;
using System.Timers;

namespace AlphaScan
{
    [Serializable]
    public partial class CamControl : UserControl, IObservable<Bitmap> 
    {
        public delegate void TriggerPhoto();
        private VideoCaptureDevice videoDevice;

        private CamConfigSettings settings;
        private System.Timers.Timer SnapTimer = new System.Timers.Timer();
        private Bitmap _CurrentFrame;
        private SnapshotForm snapshotForm = null;
        private List<IObserver<Bitmap>> observers;
        public CamControl()
        {
            InitializeComponent();
           
            observers = new List<IObserver<Bitmap>>();
            SnapTimer.Interval = 500;
            SnapTimer.Elapsed += NewSnap;
            SnapTimer.Start();

        }

        private void NewSnap(object sender, ElapsedEventArgs e)
        {
            VideoDevice.SimulateTrigger();
            foreach (var observer in observers)
            {
                if (CurrentFrame == null)
                    observer.OnError(new Exception());
                else
                    observer.OnNext(CurrentFrame);

            }

        }

        public Bitmap GetImage()
        {
            
            return CurrentFrame;
        }
        public void GetImage(RFIDTag tag)
        {
           
            tag.ImageList.Add(GetImage());
            
        }
        public void Connect(CamConfigSettings Settings)
        {
            settings = Settings;
            if (videoDevice != null)
            {






                if ((videoDevice.SnapshotCapabilities != null) && (videoDevice.SnapshotCapabilities.Length != 0))
                {
                    videoDevice.ProvideSnapshots = true;
                    videoDevice.SnapshotResolution = videoDevice.SnapshotCapabilities[settings.PhotoResolution];
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
            CurrentFrame = (Bitmap)eventArgs.Frame.Clone();
           
        }

            public IDisposable Subscribe(IObserver<Bitmap> observer)
        {
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
            }

            return (AntennaView)observer;
        }
        public void Unsubscribe(IObserver<Bitmap> observer)
        {
            observers.Remove(observer);
        }
        public VideoCaptureDevice VideoDevice { get => videoDevice; set => videoDevice = value; }
        public SnapshotForm SnapshotForm { get => snapshotForm; set => snapshotForm = value; }



        public string CamSource { get { return videoDevice.Source; } private set { } }

        public Bitmap CurrentFrame { get => _CurrentFrame; set => _CurrentFrame = value; }
        //  public int VideoResoultion { get => settings.VideoResolution; set => settings.VideoResolution = value; }
        //  public int PhotoResolution { get => settings.PhotoResolution; set => settings.PhotoResolution = value; }

    }
   
    public class CamConfigSettings
    {
        private string _Moniker;
        private int _VideoResolution;
        private int _PhotoResolution;
        private string _Source;
        private bool[] antennas = new bool[4];

       public CamConfigSettings() { }

        public string Moniker { get => _Moniker; set => _Moniker = value; }
        public int VideoResolution { get => _VideoResolution; set => _VideoResolution = value; }
        public int PhotoResolution { get => _PhotoResolution; set => _PhotoResolution = value; }
        public string Source { get => _Source; set => _Source = value; }
        public bool[] Antennas { get => antennas; set => antennas = value; }
    }
    
}
