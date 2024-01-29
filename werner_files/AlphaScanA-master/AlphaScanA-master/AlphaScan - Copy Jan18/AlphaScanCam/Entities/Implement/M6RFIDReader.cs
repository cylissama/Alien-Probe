using AlphaScanCam.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThingMagic;


namespace AlphaScanCam.Entities.Implement
{

    /// <summary>
    /// An implementation of the ObservableIDReader suited for the M6 RFID reader
    /// </summary>
    public class M6RFIDReader : ObservableIDReader<IDResponse>
    {
        bool _isScanning = false;
        private Reader _M6Reader;
        List<M6RFIDAntenna> _Antennas = new List<M6RFIDAntenna>();
        static int[] _antennas;
        static string ReaderURI;
        public M6RFIDReader(string URI)
        {
            ReaderURI = URI;
            try
            {
                _M6Reader = Reader.Create(URI);


                //Uncomment this line to add default transport listener.
                //TMReader.Transport += TMReader.SimpleTransportListener;


                _M6Reader.Connect();

                if (Reader.Region.UNSPEC == (Reader.Region)_M6Reader.ParamGet("/reader/region/id"))
                {
                    Reader.Region[] supportedRegions = (Reader.Region[])_M6Reader.ParamGet("/reader/region/supportedRegions");
                    if (supportedRegions.Length < 1)
                    {
                        throw new FAULT_INVALID_REGION_Exception();
                    }
                    _M6Reader.ParamSet("/reader/region/id", supportedRegions[0]);
                }

                //get the Antenna list.

                // Create a simplereadplan which uses the antenna list created above
                SimpleReadPlan plan = new SimpleReadPlan(null, TagProtocol.GEN2, null, null, 1000);
                // Set the created readplan
                _M6Reader.ParamSet("/reader/read/plan", plan);
                _antennas = (int[])_M6Reader.ParamGet("/reader/antenna/connectedPortList");
                for (int i = 1; i <= _antennas.Length; i++)  _Antennas.Add(new M6RFIDAntenna(i.ToString()));

                // Create and add tag listener
                _M6Reader.TagRead += ReadListener;
                // Create and add read exception listener
                _M6Reader.ReadException += new EventHandler<ReaderExceptionEventArgs>(r_ReadException);
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
        /// <summary>
        /// Recieves the read event from the M6 reader
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ReadListener(Object sender, TagReadDataEventArgs e)
        {
            RFIDResponse response = new RFIDResponse();
            response.HEX = BytesToHex(e.TagReadData.Data);
            response.EPC = e.TagReadData.EpcString;
            response.TID = BytesToHex(e.TagReadData.TIDMemData);
            response.RESERVED = BytesToHex(e.TagReadData.RESERVEDMemData);
            response.RSSI = e.TagReadData.Rssi;
            response.USER = BytesToHex(e.TagReadData.USERMemData);
            response.AntennaID = e.TagReadData.Antenna.ToString();

            ProcessTag(response);

        }
        public void ProcessTag(RFIDResponse tag)
        {
            foreach (var observer in observers)
            {
                observer.OnNext(tag);
                if (tag.TagStatus == IDTagStatus.IGNORE) break;
            }
        }
        private string  BytesToHex(byte[] bytes)
        {
            StringBuilder hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in  bytes) hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
        static void r_ReadException(object sender, ReaderExceptionEventArgs e)
        {
            Reader r = (Reader)sender;
            Console.WriteLine("Exception reader uri {0}", (string)r.ParamGet("/reader/uri"));
            Console.WriteLine("Error: " + e.ReaderException.Message);
        }
        public void Start()
        {
            _isScanning = true;
            try
            {
                M6Reader.StartReading();
            }
            catch (ArgumentException ax)
            {
                Console.WriteLine("Reader Not found");
                _isScanning = false;
                M6Reader.Destroy();


            }
        }
        public void Stop()
        {
            _isScanning = false;
           

            M6Reader.Destroy();
           

        }
        public Reader M6Reader { get => _M6Reader; set => _M6Reader = value; }
        public List<M6RFIDAntenna> Antennas { get => _Antennas; set => _Antennas = value; }
        /// <summary>
        /// A funciton for testing
        /// </summary>
        public void FakeRead()
        {
           byte[] e = Enumerable.Range(0, hex.Length)
                                 .Where(x => x % 2 == 0)
                                 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                                 .ToArray();
            
            RFIDResponse response = new RFIDResponse();
            response.HEX = BytesToHex(e.TagReadData.Data);
            response.EPC = e.TagReadData.EpcString;
            response.TID = BytesToHex(e.TagReadData.TIDMemData);
            response.RESERVED = BytesToHex(e.TagReadData.RESERVEDMemData);
            response.RSSI = e.TagReadData.Rssi;
            response.USER = BytesToHex(e.TagReadData.USERMemData);
            response.AntennaID = e.TagReadData.Antenna.ToString();

            ProcessTag(response);
        }
    }
}
