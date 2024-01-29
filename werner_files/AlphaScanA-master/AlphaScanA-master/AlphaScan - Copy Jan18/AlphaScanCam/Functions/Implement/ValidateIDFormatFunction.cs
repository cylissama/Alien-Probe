using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AlphaScanCam.Entities;
using AlphaScanCam.UserControls;
using AlphaScanCam.Functions;

namespace AlphaScanCam.Functions.Implement
{
   
    /// <summary>
    /// This is a filtering function to ensure that the ID read matches the expected format.
    /// </summary>
   public class ValidateIDFormatFunction : TagFunction
    {
        Regex _IDFormatRegex;
        public static Action<string> UpdateIDFormat;

        public Regex IDFormatRegex { get => _IDFormatRegex; set => _IDFormatRegex = value; }
public ValidateIDFormatFunction()
        {
            ValidateIDFormatSettingsControl validate = new ValidateIDFormatSettingsControl();
            validate.UpdateRegex += Validate_UpdateRegex;
            this.SettingsControl = new ValidateIDFormatSettingsControl();
           

        }

        private void Validate_UpdateRegex(string NewRegEx)
        {
            _IDFormatRegex = new Regex(NewRegEx);
        }

        public override void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public override void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public override void OnNext(IDResponse value)
        {
            if (!_IDFormatRegex.IsMatch(value.ID)) value.TagStatus = IDTagStatus.IGNORE;
        }
        
    }
}
