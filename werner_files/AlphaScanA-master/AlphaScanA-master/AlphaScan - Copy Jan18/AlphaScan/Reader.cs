using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
namespace AlphaScan
{
    public partial class Reader : UserControl
    {
       
        static Thread readThread;
        static bool _isScanning;
        static SerialPort _serialPort;
        static string _serialPortName;
        
        ReaderSettings _readerSettings;
 
               
      public  Reader(SerialPort PortToOpen)
        {
           
            // initialize variables
            _isScanning = false;
            _serialPort = PortToOpen;
            _serialPortName = PortToOpen.PortName;
            readThread = new Thread(Read);
            _readerSettings = new ReaderSettings(PortToOpen, this);
           
        }
        private int _row;
        public int row { get { return _row; } set { _row = value; } }
        private int _col;
        public int col { get { return _col; } set { _col = value; } }
        public ReaderSettings ThisReaderSettings { get { return _readerSettings; }  set { _readerSettings = value; } }


        private void Scan()

        {
                
                // Create a new SerialPort object with default settings.
                if (_serialPort == null) _serialPort = new SerialPort(_serialPortName);

                if (_serialPort.IsOpen) _serialPort.DiscardInBuffer(); else _serialPort.Open();
                if (readThread.IsAlive)
                {
                    readThread.Abort();
                    readThread.Join();
                }



                readThread = new Thread(Read); readThread.Start();
            


        }
        private void StopScan()
        {
            _isScanning = false;
        }
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
                        RFIDTag tag = _parent.ProcessFunctions(new RFIDTag(message));
                        panel1.BackColor = tag.Tagresponse.color;
                        labelLastTagScanned.Invoke(new Action(() => labelLastTagScanned.Text = tag.Tagresponse.Response));

                    }
                }


                catch (Exception r)
                {
                    string msg = r.Message;
                }
            }
        }


       
      

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            _isScanning = checkBoxScan.Checked;
        }
    }
}
