using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaScanCam.Util
{
    public class TagEventArgs:EventArgs
    {
        public EventType AlertType { get; set; }
        public String Message { get; set; }
    }
  public  enum EventType
    {
        INFORMATION,
        WARNING,
        ALERT_LOW_PRITORITY,
        ALERT_MID_PRIORITY,
        ALERT_HIGH_PRIORITY
    }
}
