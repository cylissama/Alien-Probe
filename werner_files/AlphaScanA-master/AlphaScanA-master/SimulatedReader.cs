using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThingMagic;

namespace AlphaScan
{
    class SimulatedReader : Reader
    {
        string URI;
        public override void Connect()
        {
         
        }

        public override void Destroy()
        {
           
        }
        public static new Reader Create(string uriString)
        {
            SimulatedReader sr = new SimulatedReader()
            {
                URI = uriString
            };
            return sr;
        }

        public override object ExecuteTagOp(TagOp tagOP, TagFilter target)
        {
            throw new NotImplementedException();
        }
        public new object ParamGet(string key)
        {
            return URI;
        }
        public override void FirmwareLoad(Stream firmware)
        {
           
        }

        public override void FirmwareLoad(Stream firmware, FirmwareLoadOptions flOptions)
        {
            
        }

        public override GpioPin[] GpiGet()
        {
            throw new NotImplementedException();
        }

        public override void GpoSet(ICollection<GpioPin> state)
        {
            
        }

        public override void KillTag(TagFilter target, TagAuthentication password)
        {
            
        }

        public override void LockTag(TagFilter target, TagLockAction action)
        {
            
        }

        public override TagReadData[] Read(int timeout)
        {
            throw new NotImplementedException();
        }

        public override byte[] ReadTagMemBytes(TagFilter target, int bank, int byteAddress, int byteCount)
        {
            throw new NotImplementedException();
        }

        public override ushort[] ReadTagMemWords(TagFilter target, int bank, int wordAddress, int wordCount)
        {
            throw new NotImplementedException();
        }

        public override void Reboot()
        {
           
        }

        public override void ReceiveAutonomousReading()
        {
            
        }

        public override void StartReading()
        {

           
           
        }

        private void SimulatedReader_TagRead(object sender, TagReadDataEventArgs e)
        {
            
        }

        public override void StopReading()
        {
            throw new NotImplementedException();
        }

        public override void WriteTag(TagFilter target, TagData epc)
        {
            
        }

        public override void WriteTagMemBytes(TagFilter target, int bank, int address, ICollection<byte> data)
        {
           
        }

        public override void WriteTagMemWords(TagFilter target, int bank, int address, ICollection<ushort> data)
        {
            
        }
    }
    public class SimTagReadData :TagReadData
    {
        public static int TagIncrmentor =0;
        //
        // Summary:
        //     Number of times the Tag was read.
        public new int ReadCount;

        public SimTagReadData() {

        }

        //
        // Summary:
        //     Phase when tag was read
        public new int Phase { get { return 1; } }
        //
        // Summary:
        //     Time when tag was read
        public new DateTime Time { get { return DateTime.Now; } }
        //
        // Summary:
        //     Frequency at which the tag was read.
        public new int Frequency { get { return 1; } }
        //
        // Summary:
        //     Tag that was read
        public new TagData Tag { get; }
        //
        // Summary:
        //     EPC of tag, as human-readable string
        public new string EpcString {
            get {
                string ret = "4E20";
                int fac = 118;
                ret+= fac.ToString().PadLeft(15, '0');
                ret += TagIncrmentor.ToString().PadLeft(5, '0');
                TagIncrmentor += 1;
                fac = ret.Length;
                return ret; } }
        //
        // Summary:
        //     EPC of tag
        public new byte[] Epc { get { return new byte[0]; } }
        //
        // Summary:
        //     RSSI units
        public new int Rssi { get; set; }
        //
        // Summary:
        //     [1-based] numeric identifier of antenna that tag was read on
        public new int Antenna { get; }
        //
        // Summary:
        //     Read Reserved Bank Data Bytes
        public new byte[] RESERVEDMemData { get { return new byte[0]; } }
        //
        // Summary:
        //     Read User Bank Data Bytes
        public new byte[] USERMemData { get { return new byte[0]; } }
        //
        // Summary:
        //     Read Tid Bank Data Bytes
        public new byte[] TIDMemData { get { return new byte[0]; } }
        //
        // Summary:
        //     Read EPC Bank Data Bytes
        public new byte[] EPCMemData { get { return new byte[0]; } }
        //
        // Summary:
        //     Read Data Bytes
        public new byte[] Data { get { return new byte[0]; } }
        //
        // Summary:
        //     GPIO value when tag was read
        public new GpioPin[] GPIO { get; }
        //
        // Summary:
        //     Reader when tag was read
        public new Reader Reader { get; set; }

        //
        // Summary:
        //     Human-readable representation
        //
        // Returns:
        //     A string representing the current object
        public override string ToString() { return ""; }
    }
}
