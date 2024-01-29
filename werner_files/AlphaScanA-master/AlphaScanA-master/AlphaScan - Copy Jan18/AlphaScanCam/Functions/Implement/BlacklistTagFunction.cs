using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlphaScanCam.Entities;
using AlphaScanCam.Entities.Implement;
using AlphaScanCam.Functions;

namespace AlphaScanCam.Functions.Implement
{
    public  class BlacklistTagFunction : TagFunction
    {
        public  List<string> _Blacklist = new List<string>();

        public  List<string> Blacklist { get => _Blacklist; set => _Blacklist = value; }
        public bool IsListDirty { get => isListDirty; set => isListDirty = value; }
        public string BlaclistFileName { get => blaclistFileName; set => blaclistFileName = value; }

        private string blaclistFileName;

        private bool isListDirty;

        public BlacklistTagFunction()
        {
            DisplayControl = new BlacklistControl();
            SettingsControl = new BlacklistSettingsControl(this);
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
        
            if (_Blacklist.Contains(value.ID)) value.TagStatus = IDTagStatus.ALERT;
            
              
        }

    }
}
