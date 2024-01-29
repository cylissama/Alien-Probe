using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaScanCam.Entities.Implement
{
    public class M6RFIDAntenna : Antenna
    {
        private int _PowerLevel;

        public int PowerLevel
        {
            get { return _PowerLevel; }
            set { _PowerLevel = value; }
        }
        public M6RFIDAntenna(string ID)
        {
            this.ID = ID;
        }
    }
}
