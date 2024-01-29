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

namespace AlphaScan
{
    public partial class CamSettings : UserControl
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoDevice;
        private VideoCapabilities[] videoCapabilities;
        private VideoCapabilities[] snapshotCapabilities;
        private SnapshotForm snapshotForm = null;
        private Dictionary<string,CamConfigSettings> configSettings = new Dictionary<string, CamConfigSettings>();

        public List<CamConfigSettings> ConfigSettings
        {
            get => configSettings.Values.ToList<CamConfigSettings>(); set
            {
                configSettings.Clear();
                if (value == null) return;
                foreach( CamConfigSettings cs in value)
                {
                    configSettings.Add(cs.Moniker, cs);
                }
                
            }
        }

        public CamSettings()
        {
            InitializeComponent();
            
        }

        private void CamSettings_Load(object sender, EventArgs e)
        {
            // enumerate video devices
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoDevices.Count != 0)
            {
                // add all devices to combo
                foreach (FilterInfo device in videoDevices)
                {
                    devicesCombo.Items.Add(device.Name);
                }
            }
            else
            {
                devicesCombo.Items.Add("No DirectShow devices found");
            }

            devicesCombo.SelectedIndex = 0;

            EnableConnectionControls(true);
        
        }
        // Enable/disable connection related controls
        private void EnableConnectionControls(bool enable)
        {
            devicesCombo.Enabled = enable;
            videoResolutionsCombo.Enabled = enable;
            snapshotResolutionsCombo.Enabled = enable;
            connectButton.Enabled = enable;
            disconnectButton.Enabled = !enable;
            triggerButton.Enabled = (!enable) && (snapshotCapabilities.Length != 0);
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            if (videoDevice != null)
            {
                if ((videoCapabilities != null) && (videoCapabilities.Length != 0))
                {
                    videoDevice.VideoResolution = videoCapabilities[videoResolutionsCombo.SelectedIndex];
                }

                if ((snapshotCapabilities != null) && (snapshotCapabilities.Length != 0))
                {
                    videoDevice.ProvideSnapshots = true;
                    videoDevice.SnapshotResolution = snapshotCapabilities[snapshotResolutionsCombo.SelectedIndex];
                    videoDevice.SnapshotFrame += new NewFrameEventHandler(VideoDevice_SnapshotFrame);
                }

                EnableConnectionControls(false);

                videoSourcePlayer.VideoSource = videoDevice;
                videoSourcePlayer.Start();

                XmlSerializer serializer = new XmlSerializer(typeof(List<CamConfigSettings>));

                CamConfigSettings cs = configSettings.ContainsKey(videoDevice.Source)? cs = configSettings[videoDevice.Source]: new CamConfigSettings()
                {
                    Moniker = videoDevice.Source


                };
                cs.VideoResolution = videoResolutionsCombo.SelectedIndex;
                cs.PhotoResolution = snapshotResolutionsCombo.SelectedIndex;
                cs.Antennas[0] = checkBoxAnt1.Checked;
                cs.Antennas[1] = checkBoxAnt2.Checked;
                cs.Antennas[2] = checkBoxAnt3.Checked;
                cs.Antennas[3] = checkBoxAnt4.Checked;

                if (configSettings == null) configSettings = new Dictionary<string, CamConfigSettings>();
                if (!configSettings.ContainsKey(cs.Moniker)) configSettings.Add(cs.Moniker, cs); else configSettings[cs.Moniker] = cs;
                StringWriter sw = new StringWriter();

                serializer.Serialize(sw, configSettings.Values.ToList<CamConfigSettings>());

                Properties.Settings.Default.CamSettingsList = sw.ToString();
                Properties.Settings.Default.Save();
                sw.Close();

                
               
            }
        }
        private void VideoDevice_SnapshotFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Console.WriteLine(eventArgs.Frame.Size);

           ShowSnapshot((Bitmap)eventArgs.Frame.Clone());
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
        private void DisconnectButton_Click(object sender, EventArgs e)
        {
            Disconnect();
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

                EnableConnectionControls(true);
            }
        }
        private void TriggerButton_Click(object sender, EventArgs e)
        {

            if ((videoDevice != null) && (videoDevice.ProvideSnapshots))
            {
                videoDevice.SimulateTrigger();
            }
        }

        private void DevicesCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (videoDevices.Count != 0)
            {
                videoDevice = new VideoCaptureDevice(videoDevices[devicesCombo.SelectedIndex].MonikerString);
                EnumeratedSupportedFrameSizes(videoDevice);
                if (!configSettings.ContainsKey(videoDevice.Source)) return;
                
                    CamConfigSettings cs = configSettings[videoDevice.Source];
                    videoResolutionsCombo.SelectedIndex = cs.VideoResolution;
                    snapshotResolutionsCombo.SelectedIndex = cs.PhotoResolution;
                    checkBoxAnt1.Checked = cs.Antennas[0];
                    checkBoxAnt2.Checked = cs.Antennas[1];
                    checkBoxAnt3.Checked = cs.Antennas[2];
                    checkBoxAnt4.Checked = cs.Antennas[3];
                
            }
        }
        private void EnumeratedSupportedFrameSizes(VideoCaptureDevice videoDevice)
        {
            this.Cursor = Cursors.WaitCursor;
            videoResolutionsCombo.Items.Clear();
            snapshotResolutionsCombo.Items.Clear();
            try
            {
                videoCapabilities = videoDevice.VideoCapabilities;
                snapshotCapabilities = videoDevice.SnapshotCapabilities;
                foreach (VideoCapabilities capabilty in videoCapabilities)
                {
                    videoResolutionsCombo.Items.Add(string.Format("{0} x {1}", capabilty.FrameSize.Width, capabilty.FrameSize.Height));
                }
                foreach (VideoCapabilities capabilty in snapshotCapabilities)
                {
                    snapshotResolutionsCombo.Items.Add(string.Format("{0} x {1}", capabilty.FrameSize.Width, capabilty.FrameSize.Height));
                }
                if (videoCapabilities.Length == 0)
                {
                    videoResolutionsCombo.Items.Add("Not supported");
                }
                if (snapshotCapabilities.Length == 0)
                {
                    snapshotResolutionsCombo.Items.Add("Not supported");
                }
                videoResolutionsCombo.SelectedIndex = 0;
                snapshotResolutionsCombo.SelectedIndex = 0;
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }
    }
    
    

}
