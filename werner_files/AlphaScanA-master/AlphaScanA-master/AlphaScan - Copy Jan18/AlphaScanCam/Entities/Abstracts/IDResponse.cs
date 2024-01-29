using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaScanCam.Entities
{
    public abstract class IDResponse
    {
        //A unique identifier
        public string ID { get; set; }
        public IDTagStatus TagStatus { get => tagStatus; set => tagStatus = value; }

        private IDTagStatus tagStatus = IDTagStatus.NEW;
    }
    public enum IDTagStatus
    {
        NEW, READ, NORMAL, WARNING, ALERT, IGNORE
    }

}
