using System.Collections.Generic;
using System.IO.Ports;

namespace CES.AlphaScan.mmWave
{
    public class mmWaveStartUp
    {
        /// <summary>
        /// reads and writes mmWave config file to sensor for startup
        /// </summary>
        /// <param name="UARTPort">UART port to write to</param>
        /// <param name="configDirectory">directory of mmWave config file</param>
        public static void ReadCfgFile(SerialPort UARTPort, string configDirectory)    // creates config stream for UART port to read from config file
        {
            string line;
            List<string> configStream = new List<string>();

            System.IO.StreamReader config = new System.IO.StreamReader(@configDirectory);

            UARTPort.Open();
            // write each line from the config.cfg file for the sensor to set up the sensor
            while ((line = config.ReadLine()) != null)
            {
                UARTPort.WriteLine(line);
                System.Threading.Thread.Sleep(10);
            }
            config.Close();
        }
    }
}
