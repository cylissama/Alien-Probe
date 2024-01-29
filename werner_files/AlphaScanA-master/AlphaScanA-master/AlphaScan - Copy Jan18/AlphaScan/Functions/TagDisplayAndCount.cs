using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Xml.Linq;
using System.Xml;
namespace AlphaScan
{
    [TagFunctionAttribute("This plug-in will list the tags and count unique tags in a lot")]
    public class TagDisplayAndCount : ITagFunction
    {
        static List<string> scannedtags = new List<string>();
        static DisplayAndCount _DisplayAndCountControl;
        static DisplayAndCountSettings _DisplayAndCountSettings;
    
        static XDocument PermitZoneDoc;
        public const int _tagLength = 8;
        static List<LotCount> LotCounts = new List<LotCount>();
        static LotCount currentCount;
   
        DateTime ZoneStartDT = new DateTime();
        DateTime ZoneEndDT = new DateTime();
        List<string> approvedPrefixes = new List<string>();
        string CurrentZone;
        public string GetDisplayName() { return "Dispaly and Count"; }
        
        public TagDisplayAndCount()
        {
            _DisplayAndCountControl = new DisplayAndCount(this);
            _DisplayAndCountControl.OnCountChanged += _DisplayAndCountControl_OnCountChanged;
            _DisplayAndCountControl.OnCountChanging += _DisplayAndCountControl_OnCountChanging;
            _DisplayAndCountSettings = new DisplayAndCountSettings(this);



        }
        public event EventHandler OnCountChanging;
        private void _DisplayAndCountControl_OnCountChanging(object sender, EventArgs e)
        {
            OnCountChanging(sender,e);
        }

        public event EventHandler<EventArgs> OnCountReset;
        private void _DisplayAndCountControl_OnCountChanged(object sender, EventArgs e)
        {
            OnCountReset(sender, e);
        }

        public DisplayAndCount GetDisplayAndCountControl { get { return _DisplayAndCountControl; } }
        public void SetZoneElement(string zoneString)
        {
            
            XElement ZoneElement = (from el in PermitZoneDoc.Root.Descendants("ZONES").Descendants("ZONE") where (el.Attribute("name").Value == zoneString) select el).First();
            IEnumerable<XElement> timezoneElements = ZoneElement.Descendants("TimeSpan");

            foreach (XElement tzEl in timezoneElements)
            {
                DateTime startDT = new DateTime();
                DateTime endDT = new DateTime(); 
                DateTime.TryParse(tzEl.Attribute("Start").Value, out startDT);
                DateTime.TryParse(tzEl.Attribute("End").Value, out endDT);
                if (DateTime.Now < startDT) continue;
                if (DateTime.Now > endDT) continue;
                approvedPrefixes.Clear();
                foreach( XElement prefixEl in tzEl.Descendants("Permit"))
                {
                    approvedPrefixes.Add(prefixEl.Attribute("prefix").Value.ToString());
                }
                ZoneStartDT = startDT;
                ZoneEndDT = endDT;
                CurrentZone = zoneString;
            }
        }
        public void LoadZoneInfo(XDocument doc)
        {
            PermitZoneDoc = doc;
            _DisplayAndCountControl.Refresh();
            
        }

        public TagFunctionResponse CheckTag(RFIDTag Tag)
        {
            if (CurrentZone == null) return Tag.Tagresponse;
            if (DateTime.Now < ZoneStartDT) SetZoneElement(CurrentZone);
            if (DateTime.Now > ZoneEndDT) SetZoneElement(CurrentZone);
            if (!_DisplayAndCountControl.IsCounting) Init();
            if (!_DisplayAndCountControl.IsCounting) return Tag.Tagresponse;
            if (!Tag.Tagresponse.IsValid || Tag.Tagresponse.IsIgnored) return Tag.Tagresponse;
            if (Tag.prefix == null) CalcPermit(Tag);
            if (Tag.Tagresponse.IsValid && !Tag.Tagresponse.IsIgnored)
            {
                if (currentCount.AddTagToCount(Tag))
                {
                    _DisplayAndCountControl.Invoke(new Action(() => _DisplayAndCountControl.UpdateCount(Tag.prefix, currentCount.PrefixCount(Tag.prefix).ToString())));   
                }
                Tag.Tagresponse.IsValid = approvedPrefixes.Contains(Tag.prefix);
                Tag.Lot = _DisplayAndCountControl.SelectedLotName;
                Tag.Zone = _DisplayAndCountControl.SelectedZoneName;
            }
            if (!Tag.Tagresponse.IsValid)
            {
                Tag.Tagresponse.Color = Color.Yellow;
                Tag.Tagresponse.Message = "Out Of Zone";
            }

            return Tag.Tagresponse;
        }
       
       

       






        public void Init()
        {

            if (_DisplayAndCountControl.SelectedLotIndex < 0 || _DisplayAndCountControl.SelectedZoneIndex < 0) return;
            currentCount = new LotCount(_DisplayAndCountControl.SelectedLotName, _DisplayAndCountControl.SelectedZoneName);
            LotCounts.Add(currentCount);
            _DisplayAndCountControl.IsCounting = true;

        }



        public UserControl GetControl()
        {
            return _DisplayAndCountControl;
        }

        public XElement GetLots()
        {

            if (PermitZoneDoc != null) return PermitZoneDoc.Root.Descendants("LOTNAMES").First(); else return new XElement("LOTNAMES");
        }
        public XElement GetZones()
        {

            if (PermitZoneDoc != null) return PermitZoneDoc.Element("PERMITZONES").Element("ZONES"); else return new XElement("ZONES");


        }
        public void SaveCount()
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                FileName = currentCount.LotName + " " + String.Format("{0:dd-MM-yyyy HH mm ss}", DateTime.Now) + ".dat"
            };
            DialogResult dr =sfd.ShowDialog(); 
            
            if (dr == DialogResult.Cancel) return;
            
            
            currentCount.IsOpen = false;
            currentCount.Endtime = DateTime.Now;

            currentCount.GetCountXml().Save(sfd.FileName);
           
        }

        public Dictionary<object, object> GetAppSettings()
        {
            return new Dictionary<object, object>();
        }
        public UserControl GetSettingsControl()
        {
            return _DisplayAndCountSettings;
        }
        public void AddMenuItem(MenuStrip menustrip1)
        {
            ToolStripMenuItem functionMenuItem;
            ToolStripMenuItem BlacklistMenuItem = new ToolStripMenuItem();

            foreach (ToolStripMenuItem item in menustrip1.Items)
            {
                if ("functionsToolStripMenuItemFunctions" == item.Name)
                {
                    functionMenuItem = item;
                    break;
                }
            }


        }
        public static  void CalcPermit(RFIDTag tag)
        {

            int startInt = -1;
            int endint = -1;
            //get all of the prefixes with this facility codes
            IEnumerable<XElement> PrefixElements = (from el in PermitZoneDoc.Root.Descendants("PERMITPREFIXES").Descendants("Permit") where (el.Attribute("fac").Value == tag.FAC.ToString()) select el);
            foreach (XElement PrefixElement in PrefixElements)
            {
                if (!int.TryParse(PrefixElement.Attribute("start").Value, out startInt)) continue;
                if (tag.Card < startInt) continue;
                if (!int.TryParse(PrefixElement.Attribute("end").Value, out endint)) continue;
                if (tag.Card > endint) continue; 
                tag.prefix = PrefixElement.Attribute("prefix").Value;
                int.TryParse(PrefixElement.Attribute("offset").Value, out int offset);
                int srt = tag.Card;
                if(tag.FAC == 117) srt = srt-offset;
                tag.PermitString = (tag.prefix + srt.ToString());
                break;
                
            }


            if (tag.prefix == null)
            {
                tag.Tagresponse.IsIgnored = true;

            }
            
            
        }
        public class PrefixCount
        {
            List<RFIDTag> tags = new List<RFIDTag>();
            public string Prefix;
            public int Count()
            {
                return tags.Count();
            }
            public PrefixCount(string prefix)
            {
                Prefix = prefix;
            }
            public bool AddTag(RFIDTag tag)
            {

                if (!tags.Contains(tag))
                {
                    tags.Add(tag);
                    return true;
                }
                return false;
            }

        }
        public class LotCount
        {
            public string LotName;
            public string Zonename;
            public DateTime StartTime;
            public DateTime Endtime;
            public Dictionary<string, PrefixCount> Counts;
            public bool IsOpen;
            
            public LotCount(string lot, string zone)
            {
                StartTime = DateTime.Now;
                LotName = lot;
                Zonename = zone;
                Counts = new Dictionary<string, PrefixCount>();
                IsOpen = true;
            }
            public bool AddTagToCount(RFIDTag tag)
            {


                if (!Counts.Keys.Contains(tag.prefix))
                {
                    Counts.Add(tag.prefix, new PrefixCount(tag.prefix));
                }
               return Counts[tag.prefix].AddTag(tag);

            }
            public int Count()
            {
                int ret = 0;
                foreach (PrefixCount lc in Counts.Values)
                {
                    ret += lc.Count();
                }
                return ret;
            }
            public int PrefixCount(string prefix)
            {
              return Counts[prefix].Count();
            

            }
            public XDocument GetCountXml()
            {
                XElement root = new XElement("LotCount", new XAttribute("LotName",LotName),new XAttribute("Zone", Zonename), new XAttribute("StartTime", StartTime), new XAttribute("EndTime", Endtime));
                foreach (PrefixCount lc in Counts.Values)
                {
                    XElement pcelement = new XElement("Prefix", new XAttribute("name", lc.Prefix), lc.Count());
                    root.Add(pcelement);
                }

                 return new XDocument(root);
                
            }


            


        }
    }
}



