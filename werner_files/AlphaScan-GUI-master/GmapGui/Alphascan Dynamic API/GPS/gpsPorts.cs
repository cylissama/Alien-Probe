using System.IO.Ports;

namespace CES.AlphaScan.Gps
{
    public static class GpsPorts
    {
        /// <summary>
        /// sets up the GPS port with valid settings
        /// </summary>
        /// <param name="portName">string name of COM port</param>
        /// <returns></returns>
        public static SerialPort SetupGPSPort(string portName)
        {
            SerialPort gpsCOMPort = new SerialPort
            {
                PortName = portName,
                BaudRate = 921600,
                DtrEnable = true
            };
            
            return gpsCOMPort;
        }
    }
}
