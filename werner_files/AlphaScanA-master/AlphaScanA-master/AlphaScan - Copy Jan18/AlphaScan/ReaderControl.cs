using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;

namespace AlphaScan
{
    public partial class ReaderControl : UserControl
    {
        List<ITagFunction> _TagFunctions;
        Form1 ParentForm1;
        public void SetTagFunctions( ref List<ITagFunction> TagFunc)
        {
            _TagFunctions = TagFunc;

        }
        List<RFIDTag> _Ignoredtags;
        public void SetIgnoredtags(ref List<RFIDTag> IgnoredTags)
        {
            _Ignoredtags = IgnoredTags;
        }
      
        public ReaderControl()
        {
            try
            {
                InitializeComponent();


                label1.Text = _ReaderID;
            }
            catch { Enabled = false; }
          
        }
        List<RFIDTag> _TagsOfInterest;
        public void SetTagsOfInterest(ref List<RFIDTag> TagsOfInterest)
        {
            _TagsOfInterest = TagsOfInterest;
        }

        static List<RFIDTag> _AlertTags = new List<RFIDTag>();

        public void SetAlertTags (List<RFIDTag> alertTags)
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
        public string SerialPortName
        {
            get
            {
                return this.serialPort1.PortName;
            }

            set
            {
                this.serialPort1.PortName = value;
            }
        }
        public void Start()
        {
            _isScanning = true;
            /*  if (_readerThread.IsAlive)
              {
                  _readerThread.Abort();
                  _readerThread.Join();
              }

              _readerThread = new Thread(Read);
              _readerThread.Start();
              */
            try
            {
                if (serialPort1.IsOpen)
                {
                    serialPort1.DiscardInBuffer();
                    serialPort1.Close();
                }
                serialPort1.Open();
            }
            catch
            {
                _isScanning = false;
                this.Enabled = false;
            }
        }
        public void Stop()
        {
            _isScanning = false;
            if (serialPort1.IsOpen)
            {
                serialPort1.DiscardInBuffer();
                serialPort1.Close();
            }
        }
        delegate int StringArgReturningIntDelegate(object obj);
        bool _isScanning = false;
       

        private void Button1_Click(object sender, EventArgs e)
        {
            if (_isScanning) Stop(); else Start();
        }

        private void SerialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string message = this.serialPort1.ReadLine();


                if (message.Substring(0, 4) == "4001")
                {
                    //convert the raw string into a tag.
                    RFIDTag tag = new RFIDTag(message);
                    //see if we are searching for a particular tag
                    if (_TagsOfInterest != null && _TagsOfInterest.Count > 0)
                    {

                        if (!_TagsOfInterest.Contains(tag)) return;

                    }

                    else if (_Ignoredtags.Contains(tag))return;
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
                        if (this.listBox.InvokeRequired)
                        {
                            StringArgReturningIntDelegate d = new StringArgReturningIntDelegate(listBox.Items.Add);
                            this.Invoke(d, new object[] { ostr });
                        }
                        
                    }
                    foreach (ITagFunction func in _TagFunctions)
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
                            System.Media.SystemSounds.Asterisk.Play();
                        }

                        panel1.BackColor = TagResponse.Color;
                        labelLastTagScanned.Invoke(new Action(() => labelLastTagScanned.Text = TagResponse.Message + " " + tag.PermitString));


                        


                    }
                }
            }


            catch (Exception r)
            {
                Console.WriteLine(r.Message);
                ParentForm1 = ((Form1)this.ParentForm);
                ParentForm1.StatusText = r.Message;
            }
        }

        private void LabelLastTagScanned_TextChanged(object sender, EventArgs e)
        {
            if (listBox.Items.Contains(labelLastTagScanned.Text)) { listBox.Items.Remove(labelLastTagScanned.Text); }
            if(listBox.Items.Count > 5) { listBox.Items.RemoveAt(5);}
            listBox.Items.Insert(0,labelLastTagScanned.Text);
        }
    }
}
