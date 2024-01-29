using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ThingMagic;
using System.Threading;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Device.Location;

namespace AlphaScan
{
    public partial class M6ReaderControl : UserControl
    {
        List<CamControl> CamList = new List<CamControl>();
        SortedList<string,AntennaView> AntennaList = new SortedList<string,AntennaView>();
        List<ITagFunction> _TagFunctions;
        Form1 ParentForm1;
        static Reader TMReader;
        static int[] _antennas;
        string _ReaderURI = "";
        bool usesim;
        Dictionary<string, RFIDTag> ScannedTags = new Dictionary<string, RFIDTag>();
        System.Timers.Timer SimTimer;
        GeoCoordinate coord;
        GeoCoordinateWatcher watcher = new GeoCoordinateWatcher();
        ITagFunction CommFunction;
        public void SetTagFunctions(ref List<ITagFunction> TagFunc)
        {
            _TagFunctions = TagFunc;
            CommFunction = _TagFunctions.OfType<AlphaScan.Functions.TagCommFunction>().Single<AlphaScan.Functions.TagCommFunction>();
            _TagFunctions.Remove(CommFunction);
        }
        static List<RFIDTag> _Ignoredtags;
        public void SetIgnoredtags(ref List<RFIDTag> IgnoredTags)
        {
            _Ignoredtags = IgnoredTags;
        }

        public M6ReaderControl()
        {
            try
            {
                InitializeComponent();

                _ReaderURI = Properties.Settings.Default.ReaderURI;
                label1.Text = _ReaderURI;
                CreateTMReader(_ReaderURI);
                foreach (AntennaView av in Controls.OfType<AntennaView>()) AntennaList.Add(av.AntennaID, av);
               
                SimTimer = new System.Timers.Timer(100);
                SimTimer.Elapsed += SimTimer_Elapsed;
                

                // Do not suppress prompt, and wait 1000 milliseconds to start.
                watcher = new GeoCoordinateWatcher();
                watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
                bool started = watcher.TryStart(false, TimeSpan.FromMilliseconds(2000));
                if (!started)
                {
                    Console.WriteLine("GeoCoordinateWatcher timed out on start.");
                }


            }
            catch { Enabled = false; }
            
}

void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
{
    coord = e.Position.Location;
}

private void SimTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {  SimTimer.Stop();
            TagReadDataEventArgs ex = new TagReadDataEventArgs(new SimTagReadData());
            ReadListener(sender, ex);
            
        }

        static List<RFIDTag> _TagsOfInterest;
        public void SetTagsOfInterest(ref List<RFIDTag> TagsOfInterest)
        {
            _TagsOfInterest = TagsOfInterest;
        }

        static List<RFIDTag> _AlertTags = new List<RFIDTag>();

        public void SetAlertTags(List<RFIDTag> alertTags)
        {
            _AlertTags = alertTags;
        }
        [Category("Reader")]
        [Browsable(true)]
        private string _ReaderID = "Reader1";
        public string ReaderID
        {
            get
            {
                return _ReaderID;
            }

            set
            {
                _ReaderID = value;
                label1.Text = _ReaderID;
            }
        }


        [Category("Reader")]
        [Browsable(true)]
        public string ReaderURI
        {
            get
            {
                return this._ReaderURI;
            }

            set
            {
                this._ReaderURI = value;
            }
        }
        public void Start()
        {
            foreach (CamInterface CI in Controls.OfType<CamInterface>())
            {
                foreach (CamControl CC in CI.CameraControls)
                {
                    CamConfigSettings CS = CI.ConfigSettingsDict[CC.VideoDevice.Source];
                    for (int i = 0; i < CS.Antennas.Length; i++)
                    {
                        if (CS.Antennas[i])
                        {
                            CC.Subscribe(AntennaList.Values[i]);
                            AntennaList.Values[i].HasCam = true;
                        }
                    }
                }

            }
            _isScanning = true;
            try
            {
                TMReader.StartReading();
            }catch(ArgumentException ax)
            {
                MessageBox.Show("Reader Not found");
                _isScanning = false;
                TMReader.Destroy();
                
                
            }
            if (usesim) SimTimer.Start();
        }
        public void Stop()
        {
            _isScanning = false;

           if (!usesim) ReaderURI = TMReader.ParamGet("/reader/uri").ToString();
            
            TMReader.Destroy();
            CreateTMReader(ReaderURI);
            TMReader.Connect();
            SimTimer.Stop();
            SimTimer.Interval = 1000;
            foreach (CamInterface CI in Controls.OfType<CamInterface>())
            {
                foreach (CamControl CC in CI.CameraControls)
                {
                    CamConfigSettings CS = CI.ConfigSettingsDict[CC.VideoDevice.Source];
                    for (int i = 0; i < CS.Antennas.Length; i++)
                    {
                        if (CS.Antennas[i])
                        {
                            CC.Unsubscribe(AntennaList.Values[i]);
                        }
                    }
                }

            }

        }
        delegate int StringArgReturningIntDelegate(object obj);
        bool _isScanning = false;


        private void Button1_Click(object sender, EventArgs e)
        {
            if (_isScanning) Stop(); else Start();
        }



        private void LabelLastTagScanned_TextChanged(object sender, EventArgs e)
        {
           
        }

        public void CreateTMReader(string URI)
        {
            try
            {
                usesim = false;
                TMReader = usesim ? SimulatedReader.Create(URI) : Reader.Create(URI);



                //Uncomment this line to add default transport listener.
                //TMReader.Transport += TMReader.SimpleTransportListener;


                TMReader.Connect();
                if (!usesim)
                {
                    if (Reader.Region.UNSPEC == (Reader.Region)TMReader.ParamGet("/reader/region/id"))
                    {
                        Reader.Region[] supportedRegions = (Reader.Region[])TMReader.ParamGet("/reader/region/supportedRegions");
                        if (supportedRegions.Length < 1)
                        {
                            throw new FAULT_INVALID_REGION_Exception();
                        }
                        TMReader.ParamSet("/reader/region/id", supportedRegions[0]);
                    }
               
            
                //get the Antenna list.

                // Create a simplereadplan which uses the antenna list created above
                SimpleReadPlan plan = new SimpleReadPlan(null, TagProtocol.GEN2, null, null, 1000);
                // Set the created readplan
                TMReader.ParamSet("/reader/read/plan", plan);
                _antennas = (int[])TMReader.ParamGet("/reader/antenna/connectedPortList");

                }
                else
                {
                    _antennas = new int[2];
                    _antennas[0] = 1;
                    _antennas[1] = 2;
                }
                SetAnntennas();
                // Create and add tag listener
                TMReader.TagRead += ReadListener;
                // Create and add read exception listener
                TMReader.ReadException += new EventHandler<ReaderExceptionEventArgs>(R_ReadException);
                // Search for tags in the background
                // TMReader.StartReading();



            }
            catch (ReaderException re)
            {
                Console.WriteLine("Error: " + re.Message);
                Console.Out.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        static void R_ReadException(object sender, ReaderExceptionEventArgs e)
        {
            Reader r = (Reader)sender;
            Console.WriteLine("Exception reader uri {0}", (string)r.ParamGet("/reader/uri"));
            Console.WriteLine("Error: " + e.ReaderException.Message);
        }
        public void SetAnntennas()
        {
            for (int i = 1; i <= _antennas.Length; i++)
            {
                foreach (AntennaView AV in this.Controls.OfType<AntennaView>())
                {
                    if (AV.Name == "antennaView" + i.ToString()) AV.SetActive();

                }
            }

        }
        public void SetCamToAntenna(int AntenaID, CamControl cam)
        {
            for (int i = 1; i <= _antennas.Length; i++)
            {
                foreach (AntennaView AV in this.Controls.OfType<AntennaView>())
                {
                  //  if (AV.Name == "antennaView" + i.ToString()) AV.AssocaitedCams.Add(cam);

                }
            }
        }
        public void ReadListener(Object sender, TagReadDataEventArgs e)
        {
            try
            {
                string message;
                if (usesim)
                {
                    SimTagReadData st = (SimTagReadData) e.TagReadData;

                    message = st.EpcString;
                }
               else   message = e.TagReadData.EpcString;

             if (message.Length < 24 || message.Substring(0, 4) != "4E20") return;

                    
               
                //convert the raw string into a tag.
                    RFIDTag tag = new RFIDTag(message);
                //have we seen this tag before?

                if (ScannedTags.Keys.Contains(tag.ToString()))
                    {
                        ScannedTags[tag.PermitString].UpdateTag(tag);
                        tag = ScannedTags[tag.ToString()];

                    }

                    //see if we are searching for a particular tag
                    if (_TagsOfInterest != null && _TagsOfInterest.Count > 0)
                    {

                        if (!_TagsOfInterest.Contains(tag)) return;

                    }

                    else if (_Ignoredtags.Contains(tag)) return;
                    Console.WriteLine(message);

                    string h = message.Substring(16, 8);
                    Console.WriteLine(h);
                tag.GPSData = coord == null ? " " : coord.ToString();
                    foreach (ITagFunction func in _TagFunctions)
                    {
                    try
                    {
                        TagFunctionResponse TagResponse = func.CheckTag(tag);
                        if (TagResponse.IsIgnored)
                        {

                            if (!_Ignoredtags.Contains(tag)) _Ignoredtags.Add(tag);
                            continue;
                        }
                        if (!TagResponse.IsValid) // Tag needs attention
                        {
                            ParentForm1 = ((Form1)this.ParentForm);
                            ParentForm1.AddAlertTag(tag);


                        }
                        else // Nothing special
                        {
                            if (labelLastTagScanned.Text != tag.Tagresponse.Message + " " + tag.PermitString) System.Media.SystemSounds.Asterisk.Play();
                        }
                    }
                    catch(Exception er) { Console.WriteLine(er.Message); }
                    }
               
                    panel1.BackColor = tag.Tagresponse.Color;
                    labelLastTagScanned.Invoke(new Action(() => labelLastTagScanned.Text = tag.Tagresponse.Message + " " + tag.PermitString));
                    AddToList(tag.PermitString);
                AntennaView av =  AntennaList.Values[usesim ? 1 : e.TagReadData.Antenna - 1];
                    av.TagRead(tag);
                     CommFunction.CheckTag(tag);


               


            }


            catch (Exception r)
            {
                Console.WriteLine(r.Message);
                ParentForm1 = ((Form1)this.ParentForm);
               // ParentForm1.StatusText = r.Message;
            }



        }
        public void CloseReader()
        {
            TMReader.Destroy();
            foreach(CamInterface ci in Controls.OfType<CamInterface>())
            {
                ci.CloseCams();
            }
        }
        public void AddToList(string PermitString)
        {
            if (listBox1.Items.Contains(PermitString)) listBox1.Invoke(new Action(() => listBox1.Items.Remove(PermitString)));
            
            while (listBox1.Items.Count > 5) { listBox1.Invoke(new Action(() => listBox1.Items.RemoveAt(listBox1.Items.Count -1))); }
            listBox1.Invoke(new Action(() => listBox1.Items.Insert(0,PermitString)));
        }

        private void AntennaView2_Load(object sender, EventArgs e)
        {

        }
    }
    }

