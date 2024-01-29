using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlphaScan
{
    class TagUtilites
    {
    }
    public interface ITagFunction  //A tag function prosesses the Tag information

    {
        TagFunctionResponse CheckTag(RFIDTag TagData); //a function to check if a tag meets the functions criteria. Returns true if the tag needs attention, false if it does not.
        void AddMenuItem(MenuStrip menustrip1); //a function that can add needed menu items to the form. Simply use a blank method if not needed.
        UserControl GetControl();
        UserControl GetSettingsControl();
        Dictionary<object, object> GetAppSettings();
        string GetDisplayName();

    }
    [AttributeUsage(AttributeTargets.Class)]
    public class TagFunctionAttribute : Attribute
    {
        private string m_description;
        public TagFunctionAttribute(string description)
        {
            m_description = description;
        }
        public string Description
        {
            get { return m_description; }
            set { m_description = value; }
        }

    }
    [Serializable]
    public class TagFunctionResponse
    {
        bool _isValid;
        string _response;
        Color _color;
        bool _isIgnored;
        bool _handled;
       

        public bool IsValid { get { return _isValid; } set { _isValid = value; } }
        public string Response { get { return _response; } set { _response = value; } }
        public Color Color { get { return _color; } set { _color = value; } }
        public bool IsIgnored { get { return _isIgnored; } set { _isIgnored = value; } }


        public string Message { get; set; }
        public bool Handled { get => _handled; set => _handled = value; }
        public TagFunctionResponse() { }

        public TagFunctionResponse(bool isValid, string message, Color color)
        {
            _isValid = isValid;
            Message = message;
            _color = color;

        }
        public TagFunctionResponse(bool isValid, string Response)
        {
            _isValid = isValid;
            Message = Response;
            _color = Color.Gray;

        }
        public TagFunctionResponse(bool isValid, bool isIgnored)
        {
            _isValid = isValid;
            Message = "";
            _color = Color.Gray;
            _isIgnored = isIgnored;
        }
        public TagFunctionResponse(bool isValid)
        {
            _isValid = isValid;
            Message = "";
            _color = Color.Gray;

        }
    }

}
