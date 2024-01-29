using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlphaScan.Functions
{
    [TagFunctionAttribute("This plug-in will see if a tag is being ignored")]
    public class IgnoredTagsFunction : ITagFunction
    {
        static List<string> _Ignoredtags = new List<string>();
        static IgnoredTagsControl _IgnoredTagsControl;
        static IgnoredTagsSettingsControl _IgnoredtagsSettingsControl;
        static private string _IgnoredListFileName;
        public string GetDisplayName() { return "Ignored Tags"; }
        public string IgnoreListFileName
        {
            get { return _IgnoredListFileName; }
            set
            {
                _IgnoredListFileName = value;
                Properties.Settings.Default.IgnoreListFile = _IgnoredListFileName;
                Properties.Settings.Default.Save();
            }
        }
        public IgnoredTagsFunction()
        {
            _IgnoredtagsSettingsControl = new IgnoredTagsSettingsControl(this);
            _IgnoredTagsControl = new IgnoredTagsControl();

        }
        public TagFunctionResponse CheckTag(RFIDTag TagData)
        {
            TagData.Tagresponse.IsIgnored = _Ignoredtags.Contains(TagData.FAC.ToString()+TagData.Card.ToString());
            return TagData.Tagresponse;
        }
        public void AddMenuItem(System.Windows.Forms.MenuStrip menustrip1)
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
        public  List<string> Ignorelist
        {
            get { return _Ignoredtags; }
            set { _Ignoredtags = value; }
        }
        public UserControl GetControl()
        {
            return _IgnoredTagsControl;

        }
        public UserControl GetSettingsControl()
        {
            return _IgnoredtagsSettingsControl;
        }
        public Dictionary<object, object> GetAppSettings()
        {
            return new Dictionary<object, object>();
        }
        
    }
}
