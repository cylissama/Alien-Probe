using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;
using nsAlienRFID2;

namespace AlphaScan
{
    public partial class M6ReaderControl : UserControl
    {
        public static SerialPort iSerialPort = new SerialPort();


        nsAlienRFID2.clsReader r = new clsReader();
        CAlienServer AlienServer = new CAlienServer();
        nsAlienRFID2.clsReaderMonitor monitor = new clsReaderMonitor();
        public string TagFormat;

        SortedList<string, AntennaView> AntennaList = new SortedList<string, AntennaView>();
        List<ITagFunction> _TagFunctions;
        Form1 ParentForm1;
        static nsAlienRFID2.CAlienServer AlienReaderserver;
        static int[] _antennas;
        string _ReaderURI = "";
        bool usesim;
        Dictionary<string, RFIDTag> ScannedTags = new Dictionary<string, RFIDTag>();
        System.Timers.Timer SimTimer;

        public void SetTagFunctions(ref List<ITagFunction> TagFunc)
        {
            _TagFunctions = TagFunc;

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
                InitializeAlienReader(_ReaderURI);
                foreach (AntennaView av in Controls.OfType<AntennaView>()) AntennaList.Add(av.AntennaID, av);


            }
            catch { Enabled = false; }

            string strException = string.Empty;
            string strComPort = "COM5";
            int nBaudrate = Convert.ToInt32(9600);
            iSerialPort.PortName = strComPort;
            iSerialPort.BaudRate = nBaudrate;
            iSerialPort.DataReceived += ISerialPort_DataReceived;
          //  iSerialPort.Open();
            





        }

        private void ISerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
           ReadListener( iSerialPort.ReadExisting());
        }

        private void R_DataReceived(string data)
        {
            MessageBox.Show(data);
        }

        private void R_Connected()
        {
            try { MessageBox.Show(r.TagList); }
            catch (Exception ex) { }
        }

        private void Monitor_ReaderAddedOnSerial(IReaderInfo data)
        {
           
          
        }

        private void R_MessageReceived(string data)
        {
            ReadListener(data);
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
        { _isScanning = true;
            if (AlienServer.IsListening) return;
           
            try
            {
                int port = Properties.Settings.Default.AlienServerPort;
                TagFormat = Properties.Settings.Default.TagFormat;
                if (port == 0) throw new Exception("Port is required to connect to the Server\n Please check settings.\n");

                AlienServer.Port = port;
                AlienServer.StartListening();
                if (!AlienServer.IsListening)
                {
                    throw new ArgumentException();
                }
            }
            catch (ArgumentException ax)
            {
                MessageBox.Show("Reader Not found");
                _isScanning = false;



            }

        }
        public void Stop()
        {
            _isScanning = false;





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

        public void InitializeAlienReader(string URI)
        {
            try
            {

                // usesim = Properties.Settings.Default.NoReaderMode;
                //AlienReaderserver = usesim ? SimulatedReader.Create(URI) : Reader.Create(URI);



                //Uncomment this line to add default transport listener.
                //TMReader.Transport += TMReader.SimpleTransportListener;


                //AlienReaderserver.Connect();
                //if (!usesim)
                //{
                //    if (Reader.Region.UNSPEC == (Reader.Region)AlienReaderserver.ParamGet("/reader/region/id"))
                //    {
                //        Reader.Region[] supportedRegions = (Reader.Region[])AlienReaderserver.ParamGet("/reader/region/supportedRegions");
                //        if (supportedRegions.Length < 1)
                //        {
                //            throw new FAULT_INVALID_REGION_Exception();
                //        }
                //        AlienReaderserver.ParamSet("/reader/region/id", supportedRegions[0]);
                //    }


                ////get the Antenna list.

                //// Create a simplereadplan which uses the antenna list created above
                //SimpleReadPlan plan = new SimpleReadPlan(null, TagProtocol.GEN2, null, null, 1000);
                //// Set the created readplan
                //AlienReaderserver.ParamSet("/reader/read/plan", plan);
                //_antennas = (int[])AlienReaderserver.ParamGet("/reader/antenna/connectedPortList");

                //}
                //else
                //{
                _antennas = new int[2];
                _antennas[0] = 1;
                _antennas[1] = 2;
                //}
                SetAnntennas();
                //    AlienServer.ServerConnectionEnded += AlienServer_ServerConnectionEnded;
                //    AlienServer.ServerConnectionEstablished += AlienServer_ServerConnectionEstablished;
                //    AlienServer.ServerListeningStarted += AlienServer_ServerListeningStarted;
                //   AlienServer.ServerListeningStopped += AlienServer_ServerListeningStopped;

                //  AlienServer.ServerSocketError += AlienServer_ServerSocketError;


                // Create and add tag listener
                AlienServer.ServerMessageReceived += ReadListener;
                // Create and add read exception listener
                // AlienReaderserver.ReadException += new EventHandler<ReaderExceptionEventArgs>(R_ReadException);
                // Search for tags in the background
                // TMReader.StartReading();



            }
            catch (Exception re)
            {
                Console.WriteLine("Error: " + re.Message);
                Console.Out.Flush();
            }

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
        public void ReadListener(string message)
        {
            if (!_isScanning) return;
            try
            {
                
                RFIDTag tag = new RFIDTag(AlienUtils.ParseCustomTag(TagFormat, message));
                // new RFIDTag(T.TagID.Split(' ')[1]);
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
                    catch (Exception er) { Console.WriteLine(er.Message); }
                }

                panel1.BackColor = tag.Tagresponse.Color;
                labelLastTagScanned.Invoke(new Action(() => labelLastTagScanned.Text = tag.Tagresponse.Message + " " + tag.PermitString));
                AddToList(tag.PermitString);
                AntennaView av = AntennaList.Values[tag.Antenna];
                av.TagRead(tag);







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
           
        }
        public void AddToList(string PermitString)
        {
            if (listBox1.Items.Contains(PermitString)) listBox1.Invoke(new Action(() => listBox1.Items.Remove(PermitString)));

            while (listBox1.Items.Count > 5) { listBox1.Invoke(new Action(() => listBox1.Items.RemoveAt(listBox1.Items.Count - 1))); }
            listBox1.Invoke(new Action(() => listBox1.Items.Insert(0, PermitString)));
        }

        private void AntennaView2_Load(object sender, EventArgs e)
        {

        }
    }
}

