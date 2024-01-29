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
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using System.Timers;

namespace AlphaScan
{
    public partial class CamInterface : UserControl, IObserver<Bitmap>, IObservable<Bitmap>
    {
        
        private FilterInfoCollection videoDevices;
        private List<VideoCaptureDevice> VideoDeviceList;
        private List<CamControl> cameraControls = new List<CamControl>();
        private Dictionary<string, CamConfigSettings> configSettingsDict = new Dictionary<string, CamConfigSettings>();
       
        public CamInterface()
        {
            InitializeComponent();
            //load can setting
            LoadCamSettings();
            //itterate through system cameras
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoDevices.Count != 0)
            {
                
                foreach (FilterInfo device in videoDevices)
                {
                    //if there are settings associated with the cam, add it to a cam control and add it to the pannel 

                    if (ConfigSettingsDict.ContainsKey(device.MonikerString))
                    {
                        CamConfigSettings settings = ConfigSettingsDict[device.MonikerString];
                        CamControl camC  = new CamControl()
                                {
                                    VideoDevice = new VideoCaptureDevice(device.MonikerString)

                                };
                        if (camC.VideoDevice.VideoCapabilities != null)camC.VideoDevice.VideoResolution = camC.VideoDevice.VideoCapabilities[settings.VideoResolution];
                        camC.VideoDevice.SnapshotResolution = camC.VideoDevice.SnapshotCapabilities[settings.PhotoResolution];
                        CameraControls.Add(camC);
                        if (camC.VideoDevice != null)
                        {
                                    camC.Connect(settings);
                            flowLayoutPanel1.Controls.Add(camC);
                            //  camC.VideoDevice.Start();
                            
                        }
                              

                              
                                
                            

                       
                    }

                    //  videoDevice = new VideoCaptureDevice(videoDevices[devicesCombo.SelectedIndex].MonikerString);
                    
                }
            }
            else
            {
                // "No DirectShow devices found"
            }
            
            // associate the cameras with the antennas 








        }

        private void NewSnap(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public List<CamControl> CameraControls { get => CameraControls1; set => CameraControls1 = value; }
        public List<CamControl> CameraControls1 { get => cameraControls; set => cameraControls = value; }
        public Dictionary<string, CamConfigSettings> ConfigSettingsDict { get => configSettingsDict; set => configSettingsDict = value; }

        public void CloseCams()
        {
            foreach (CamControl cc in flowLayoutPanel1.Controls.OfType<CamControl>())
            {
                cc.Disconnect();
            }
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(Bitmap value)
        {
            throw new NotImplementedException();
        }

        public IDisposable Subscribe(IObserver<Bitmap> observer)
        {
            throw new NotImplementedException();
        }

        private void LoadCamSettings()
        {
            //deserialize cameras
            List<CamConfigSettings> Clist = new List<CamConfigSettings>();
            if (Properties.Settings.Default.CamSettingsList != null)
            {
                
                XmlSerializer deserializer = new XmlSerializer(typeof(List<CamConfigSettings>));
                try
                {
                   // Properties.Settings.Default.CamSettingsList = "";
                  //  Properties.Settings.Default.Save();
                    string s = Properties.Settings.Default.CamSettingsList;
                    Clist = (List<CamConfigSettings>)deserializer.Deserialize(XmlReader.Create(new StringReader(Properties.Settings.Default.CamSettingsList)));

                    foreach (CamConfigSettings ccs in Clist)
                    {
                        ConfigSettingsDict.Add(ccs.Moniker, ccs);
                    }

                }
                catch (Exception e) { Console.WriteLine(e.Message); }

            }
          
           
            
        }
    }
}
