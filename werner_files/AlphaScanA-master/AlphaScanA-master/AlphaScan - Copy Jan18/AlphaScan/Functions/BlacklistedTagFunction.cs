using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
namespace AlphaScan
{
   [TagFunctionAttribute("This plug-in will see if a tag is on a blacklist")]
    public class BlacklistedTagFunction : ITagFunction
    {
        static List<string> _blacklist = new List<string>();
        static BlacklistControl _BlacklistControl;
        static BlacklistSettingsControl _BlacklistSettingscontrol;
      
        public const int _tagLength = 8;
        static private List<string> blackList;
        static private bool _IsListDirty;
        static private string _blaclistFileName;
        public string GetDisplayName() { return "Blacklisted Tags"; }
        public BlacklistedTagFunction()
        {
           blackList= new List<string>();
           _BlacklistControl = new BlacklistControl(this);
           _BlacklistSettingscontrol = new BlacklistSettingsControl(this);
            
        }
        public int Taglength { get { return _tagLength; } }
        public string BlaclistFileName
        {
            get { return _blaclistFileName; }
            set
            {
                _blaclistFileName = value;
                Properties.Settings.Default.BlacklistFile = _blaclistFileName;
                Properties.Settings.Default.Save();
            }
        }
        public bool IsListDirty { get { return _IsListDirty; } set { _IsListDirty = value; } }
        public List<string> Blacklist { 
            get{ return blackList;}
            set{blackList = value;}
        }
        public void AddMenuItem(MenuStrip menustrip1){
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

        public  TagFunctionResponse CheckTag(RFIDTag TagData)
        {
           
            if (IsBlacklisted(TagData))
            {
              TagData.Tagresponse.Color = Color.Red;
              TagData.Tagresponse.IsValid = false;
              TagData.Tagresponse.Message= "Lost/Stolen";
              AlphaScan.TagDisplayAndCount.CalcPermit(TagData);
               


            }
            else
            {
            }
           
            return TagData.Tagresponse;
        }
        public UserControl GetControl()
        {
            return _BlacklistControl;
        }
      
        public bool IsBlacklisted(RFIDTag tag)
        {
            


            return Blacklist.Contains(tag.FAC.ToString()+tag.Card.ToString());
        }

        public void SaveBlacklist(object sender, EventArgs e)
        {
           _BlacklistSettingscontrol.SaveBlacklist(sender, e);
        }
        public Dictionary<object, object> GetAppSettings()
        {
            return new Dictionary<object,object>();
        }
       public UserControl GetSettingsControl()
        {
            return _BlacklistSettingscontrol;
        }
    }
}

