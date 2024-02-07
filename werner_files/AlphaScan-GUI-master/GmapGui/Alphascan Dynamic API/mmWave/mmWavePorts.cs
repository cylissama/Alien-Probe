using System.IO.Ports;

namespace CES.AlphaScan.mmWave
{
    public class mmWavePorts
    {
        /// <summary>
        /// sets up the UART port for mmWave sensor, uses default port vals
        /// </summary>
        /// <param name="UARTName">COM port for the UART port</param>
        /// <returns>Serial port object created</returns>
        /// <exception cref="System.ArgumentException">Invalid port name.</exception>
        /// <exception cref="System.ArgumentNullException">Port name null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Baud rate is out of range.</exception>
        /// <exception cref="System.InvalidOperationException">Port is already open.</exception>
        /// <exception cref="System.IO.IOException">Unable to set baud rate or enable dtr.</exception>
        public static SerialPort SetUpUART(string UARTName)
        {
            SerialPort UARTPort = new SerialPort
            {
                PortName = UARTName,
                BaudRate = 115200,
                DtrEnable = true
            };
            return UARTPort;
        }

        /// <summary>
        /// sets up the DATA port for mmWave sensor, uses default port vals
        /// </summary>
        /// <param name="DATAName">COM port for the DATA port</param>
        /// <returns>Serial port object created</returns>
        /// /// <exception cref="System.ArgumentException">Invalid port name.</exception>
        /// <exception cref="System.ArgumentNullException">Port name null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Baud rate is out of range.</exception>
        /// <exception cref="System.InvalidOperationException">Port is already open.</exception>
        /// <exception cref="System.IO.IOException">Unable to set baud rate or enable dtr.</exception>
        public static SerialPort SetupDATA(string DATAName)
        {
            SerialPort DATAPort = new SerialPort
            {
                PortName = DATAName,
                BaudRate = 921600,
                DtrEnable = true
            };
            return DATAPort;
        }
    }
}
