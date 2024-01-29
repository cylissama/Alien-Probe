using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaScanCam.Entities.Implement
{
    public class M6RFIDResponse : ThingMagic.TagReadData, IComparable
    {

        public string EPC { get; set; }
        public string TID { get; set; }
        public string USER { get; set; }
        public string RESERVED { get; set; }
        public string HEX { get; set; }
        public int RSSI { get; set; }
        public string AntennaID { get; set; }
        
        public static implicit operator BaseIDResponse(M6RFIDResponse d)
        {
            BaseIDResponse e = new BaseIDResponse();
            e.ID = d.Tag.EpcString;
            return e;

        }
        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            M6RFIDResponse o = obj as M6RFIDResponse;
            if (o != null) return this.TID.CompareTo(o.TID); else throw new ArgumentException("Not a RFID Response");
        }


    }
    
}
