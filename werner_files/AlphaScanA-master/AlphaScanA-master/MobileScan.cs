using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlphaScan
{
    public partial class MobileScan : UserControl
    {
       // static PCSerialComs coms;
       // static SettingsForm _settingsForm;
        static List<TagFunction> TagFunctions;
        static Thread readThread;
        static bool _isScanning;
        static SerialPort _serialPort;
        static string _serialPortName;
        static List<RFIDTag> Ignoredtags = new List<RFIDTag>();
        static List<RFIDTag> AlertTags = new List<RFIDTag>();
        static RFIDTag TagOfInterest;
       /* List<Reader> _ReaderList;
        public List<Reader> ReaderList
        {
            get { return _ReaderList; }
            set { _ReaderList = value; }
        }
        */
        public MobileScan()
        {
            InitializeComponent();
            // initialize variables
            _isScanning = false;
            _serialPortName = "";
            readThread = new Thread(Read);
            // Set up the port coms
          //  coms = new PCSerialComs();
            //Load the TagFunctions

            LoadTagFunctions();

            //create and open the settings form to enter and verify the app settings
            //_settingsForm = new SettingsForm(this);
            // add the list of ports
            //_settingsForm.AddPorts(coms.GetConnections().ToArray());

            //_settingsForm.Show();
           // _settingsForm.Focus();
           // this.WindowState = FormWindowState.Minimized;

        }
        private void LoadTagFunctions()
        {
            /*In the future, this may load functiond dynamically, but for now they must be loaded
            TagFunctions = new List<TagFunction>();

            TagFunctions.Add(new BlacklistedTagFunction());
            TagFunctions.Add(new TagDisplayAndCount());
            foreach (TagFunction tf in TagFunctions)
            {
                this.paneluserControls.Controls.Add(tf.getControl());
            }
            */
        }
        public List<TagFunction> TagFunctionList { get { return TagFunctions; } set { } }

       
        private void buttonScanPause_Click(object sender, EventArgs e)
        {
            if (_isScanning)
            {
                this.buttonScanPause.Text = "Scan";
                _isScanning = false;

            }
            else
            {
                this.buttonScanPause.Text = "Pause";
                _isScanning = true;
            }

            if (_isScanning)
            {
                // Create a new SerialPort object with default settings.
               // if (_serialPort == null) _serialPort = coms.getSerialPort(_serialPortName);

                if (_serialPort.IsOpen) _serialPort.DiscardInBuffer(); else _serialPort.Open();
                if (readThread.IsAlive)
                {
                    readThread.Abort();
                    readThread.Join();
                }



                readThread = new Thread(Read); readThread.Start();
            }


        }
        delegate int StringArgReturningIntDelegate(object obj);
        public void Read()
        {
            while (_isScanning)
            {

                try
                {
                    string message = _serialPort.ReadLine();


                    if (message.Substring(0, 4) == "4001")
                    {
                        //convert the raw string into a tag.
                        RFIDTag tag = new RFIDTag(message);
                        //see if we are searching for a particular tag
                        if (TagOfInterest != null && TagOfInterest.Equals(tag))
                        {

                            Console.WriteLine(message);

                        }

                        else if (Ignoredtags.Contains(tag)) continue;
                        Console.WriteLine(message);
                        string h = message.Substring(36, 8);
                        Console.WriteLine(h);
                        if (h == "11731002")
                        {
                            string str = message.Substring(8, 1);
                            string ostr = String.Empty;
                            foreach (char c in str)
                            {
                                string bit = Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0');
                                ostr += bit;
                            }
                            Console.WriteLine(ostr);
                            if (this.listBox1.InvokeRequired)
                            {
                                StringArgReturningIntDelegate d = new StringArgReturningIntDelegate(listBox1.Items.Add);
                                this.Invoke(d, new object[] { ostr });
                            }

                        }
                        foreach (TagFunction func in TagFunctions)
                        {
                            TagFunctionResponse TagResponse = func.checkTag(tag);
                            if (TagResponse.IsIgnored)
                            {

                                if (!Ignoredtags.Contains(tag)) Ignoredtags.Add(tag);
                                continue;
                            }
                            if (!TagResponse.IsValid) // Tag needs attention
                            {

                                System.Media.SystemSounds.Asterisk.Play();
                                if (AlertTags.Contains(tag))
                                {

                                }
                                else
                                {
                                    AlertTags.Add(tag);
                                    ListViewItem lvi = new ListViewItem();
                                    lvi.Name = tag.TagHex;
                                    lvi.SubItems.Add("message").Text = tag.Tagresponse.Message;
                                    lvi.Tag = tag;



                                    lvi.SubItems[0] = new ListViewItem.ListViewSubItem(lvi, tag.permitString);
                                    //   lvi.SubItems[1] = new ListViewItem.ListViewSubItem(lvi,tag.permitString);
                                    if (!listViewAlerts.Items.Contains(lvi)) listViewAlerts.Invoke(new Action(() => listViewAlerts.Items.Add(lvi)));
                                }

                            }
                            else // Nothing special
                            {

                            }

                            panel1.BackColor = TagResponse.color;
                            labelLastTagScanned.Invoke(new Action(() => labelLastTagScanned.Text = TagResponse.Response));





                        }
                    }
                }


                catch (Exception r)
                {
                    Console.WriteLine(r.Message);
                }
            }
        }

        public string SerialPortName
        {
            get { return _serialPortName; }
            set { _serialPortName = value; }
        }

        private void MobileScan_FormClosing(object sender, FormClosingEventArgs e)
        {
            _isScanning = false;
        }

        private void buttonFind_Click(object sender, EventArgs e)
        {

            if (buttonFind.Text == "Find Selected")
            {

                if (listViewAlerts.SelectedItems.Count == 0) return;
                TagOfInterest = (RFIDTag)listViewAlerts.SelectedItems[0].Tag;
                buttonFind.Text = "Scan All";
                paneluserControls.Visible = false;
                listViewAlerts.Visible = false;
                if (!_isScanning) buttonScanPause_Click(sender, e);
            }
            else
            {
                TagOfInterest = null;
                buttonFind.Text = "Find Selected";
                paneluserControls.Visible = true;
                listViewAlerts.Visible = true;
                if (!_isScanning) buttonScanPause_Click(sender, e);
            }
        }

        private void buttonIgnore_Click(object sender, EventArgs e)
        {
            if (listViewAlerts.SelectedItems.Count == 0) return;
            foreach (ListViewItem lvi in listViewAlerts.SelectedItems)
            {
                Ignoredtags.Add((RFIDTag)lvi.Tag);
                lvi.BackColor = Color.Gray;
            }
        }
    }
}
