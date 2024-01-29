using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using nsAlienRFID2;
namespace AlphaScan
{
    class RFIDUtilities
    {
    }
    [Serializable]
    public class RFIDTag : TagInfo, IEquatable<RFIDTag>, IComparable<RFIDTag>
    {
        public string RawHex;
        public string GPSData;
        public string Lot;
        public string Zone;
        public int FAC;
        public int Card;
        public int Range;
        public bool isWritten;
        public DateTime LastDetect;
        private string _permitString = "";
        public string prefix;
        public string TagHex;
        private int antenna;
        private List<Bitmap> _ImageList = new List<Bitmap>();

        

        public int Antenna { get => antenna; set => antenna = value; }
        public List<Bitmap> ImageList { get => _ImageList; set => _ImageList = value; }
        public string PermitString { get => _permitString.Length > 0? _permitString : FAC.ToString() + Card.ToString(); set => _permitString = value; }

     
        public TagFunctionResponse Tagresponse;

        public override string ToString()
        {
            if (_permitString.Length > 0) return _permitString;
            else if (FAC != 0) return FAC.ToString() + Card.ToString(); else return RawHex;
            
        }
        public RFIDTag()
        {
            this.Tagresponse = new TagFunctionResponse(true, false);
        }
      
        public RFIDTag(string Hex)
        {
            this.Tagresponse = new TagFunctionResponse(true, false);
            RawHex = Hex;
            if (!RawHex.Contains("4E20"))
            {
                this.Tagresponse.IsIgnored = true;
                return;
            }
            string t = RawHex.Substring(RawHex.Length - 5);
            int.TryParse(RawHex.Substring(RawHex.Length - 8, 3), out FAC);
            int.TryParse(RawHex.Substring(RawHex.Length - 5), out Card);
        }

        public RFIDTag(TagInfo tag) : base(tag)
        {
            this.Tagresponse = new TagFunctionResponse(true, false);
            RawHex = tag.TagID.Split(' ')[1];
            if (!RawHex.Contains("4E20"))
            {
                this.Tagresponse.IsIgnored = true;
                return;
            }
            string t = RawHex.Substring(RawHex.Length - 5);
            int.TryParse(RawHex.Substring(RawHex.Length - 8, 3), out FAC);
            int.TryParse(RawHex.Substring(RawHex.Length - 5), out Card);
        }

        public bool Equals(RFIDTag other)
        {
            if (other == null)
                return false;

            return this.RawHex.Equals(other.RawHex);
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            RFIDTag tagObj = obj as RFIDTag;
            if (tagObj == null)
                return false;
            else
                return Equals(tagObj);
        }

        public override int GetHashCode()
        {
            return (this.FAC & this.Card).GetHashCode();
        }

        public static bool operator ==(RFIDTag tag1, RFIDTag tag2)
        {
            if (((object)tag1) == null || ((object)tag2) == null)
                return Object.Equals(tag1, tag2);

            return tag1.Equals(tag2);
        }

        public static bool operator !=(RFIDTag tag1, RFIDTag tag2)
        {
            if (((object)tag1) == null || ((object)tag2) == null)
                return !Object.Equals(tag1, tag2);

            return !(tag1.Equals(tag2));
        }


        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            RFIDTag objTag = obj as RFIDTag;
            if (objTag.Card == 0 || objTag.FAC == 0) return 1;
            if (this.FAC == 0 || this.Card == 0) return -1;
            if (objTag.FAC < this.FAC) return 1;
            else if (objTag.FAC > this.FAC) return -1;
            return this.Card.CompareTo(objTag.Card);
        }
        public void UpdateTag(RFIDTag InTag)
        {
            this.Antenna = InTag.Antenna;
            this.GPSData = InTag.GPSData;
            this.LastDetect = InTag.LastDetect;
            this.Lot = InTag.Lot;
            this.Range = InTag.Range;
            this.Zone = InTag.Zone;
        }

        public int CompareTo(RFIDTag other)
        {
            throw new NotImplementedException();
        }
    }



}
