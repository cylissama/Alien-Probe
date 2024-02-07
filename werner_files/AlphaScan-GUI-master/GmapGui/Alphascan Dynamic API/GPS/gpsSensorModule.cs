using System;
using System.Linq;
using System.ComponentModel; // for INotifyPropertyChanged interface
using System.Runtime.CompilerServices; // for [CallerMemberName] in NotifyPropertyChanged
using System.IO.Ports;
using CES.AlphaScan.Base;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading;
using System.Threading.Tasks;

namespace CES.AlphaScan.Gps
{
    /// <summary>
    /// Contains data pertaining to the GPS sensor.
    /// </summary>
    public struct GPSSettings
    {
        public string COMPort;
        public int Rate;
        public bool RTKEnabled;
        public string SaveDirectory;
        public bool isSaving;
    }

    /// <summary>
    /// Module for controlling connection and communication with a GPS sensor. Receives data the sensor reads.
    /// </summary>
    public class GpsSensorModule : IGpsSensorModule, INotifyPropertyChanged, ILogMessage
    {
        /*
            Module for GPS functionality control. This sets up all channels and data processing methods for the GPS sensor. This also creates event triggers
            for when certain things happen on the sensor. This also creates any logging for any data processing errors should they occur. This also includes all set up functionality
            including config reading for sending the user defined sensor parameters and other configured GPS settings.
        */ 

        #region Logging

        /// <summary>
        /// Name of the GPS sensor.
        /// </summary>
        public string Name { get; protected set; } = "GPSSensor";

        /// <summary>
        /// Logs message string.
        /// </summary>
        /// <param name="message">Message to log.</param>
        private void LogMessage(string message)
        {
            MessageLogged?.Invoke(this, new LogMessageEventArgs(message, Name));
        }

        /// <summary>
        /// Re-logs a message from event arguments and sender object.
        /// </summary>
        /// <param name="sender">Object that raised the event.</param>
        /// <param name="messageArgs">Event arguments of message to log.</param>
        private void LogMessage(object sender, LogMessageEventArgs messageArgs)
        {
            MessageLogged?.Invoke(this, new LogMessageEventArgs(messageArgs, Name));
        }

        public event EventHandler<LogMessageEventArgs> MessageLogged;
        #endregion

        public GPSSettings gpsSettings = new GPSSettings();

        /// <summary>
        /// Serial port of the GPS sensor.
        /// </summary>
        private SerialPort gpsPort;

        private Channel<GpsData> gpsOutputData;

        public event Action AddDataToMap
        {
            add { processor.AddDataToMap += value; }
            remove { processor.AddDataToMap -= value; }
        }

        #region Property Notification
        //Properties that notify of changes.

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Called to notify that a property has changed. Allows decoupling of objects without resource-heavy poll loops.
        /// </summary>
        /// <param name="propertyName">Name of the property that was updated.</param>
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            //[CallMemberName] attribute will get name of member that calls this function. Should be the property being changed.

            // Raises PropertyChanged event.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Whether or not the sensor is currently connected.
        /// </summary>
        private bool _isConnected = false;
        public bool IsConnected
        {
            get
            {
                return _isConnected;
            }
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Whether GPS data is currently being processed.
        /// </summary>
        public bool IsProcessing
        {
            get
            {
                if (processor != null)
                    return processor.IsProcessing;
                else return false;
            }
        }

        /// <summary>
        /// Propogates property changes from subclass to this class.
        /// </summary>
        /// <param name="sender">Object that raised the event.</param>
        /// <param name="e">Data describing property change.</param>
        private void GPS_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string propName = e.PropertyName;

            switch (propName)
            {
                case nameof(processor.IsProcessing):
                    NotifyPropertyChanged(nameof(this.IsProcessing));
                    break;
                default:
                    break;
            }
        }

        #endregion

        /// <summary>
        /// Constructs new GPS sensor driver module.
        /// </summary>
        public GpsSensorModule(Channel<GpsData> outputChannel, System.Collections.Concurrent.ConcurrentQueue<GpsData> addToMapQueue = null)
        {
            gpsOutputData = outputChannel;
            processor = new GpsDataProcessing(gpsOutputData, addToMapQueue);
            processor.PropertyChanged += GPS_PropertyChanged;
            processor.MessageLogged += LogMessage;
        }

        /// <summary>
        /// Output manager for the GPS sensor.
        /// </summary>
        private IOutputManager GPSOutputManager { get; set; }

        /// <summary>
        /// Connects the GPS sensor if able to.
        /// </summary>
        /// <returns>bool value of whether the connection was completed</returns>
        public bool Start()
        {
            if (!isSetUp.IsSet)
            {
                LogMessage("Failed to connect: GPS sensor settings not set.");
                return false;
            }

            //Start GPS Processor
            processor.CreateProcessorThread(GPSOutputManager, gpsSettings.isSaving);

            //Connect GPS
            try
            {
                if (gpsPort != null)
                {
                    if (gpsPort.IsOpen)
                    {
                        IsConnected = true;
                        return true;
                    }
                }
            }
            catch 
            {
                if (processor.IsProcessing) processor.Stop();
                LogMessage("Failed to check if GPS port was open.");
                IsConnected = false;
                return false;
            }
            // attempt to set up GPS COM ports
            try 
            { 
                gpsPort = GpsPorts.SetupGPSPort(gpsSettings.COMPort); 
            }
            catch 
            {
                if (processor.IsProcessing) processor.Stop();
                LogMessage("Failed to set up GPS port.");
                IsConnected = false;
                return false;
            }
            // set up data received event and open the GPS port
            gpsPort.DataReceived += DataReceivedEvent;
            gpsPort.Open();

            // send the rate to the GPS
            SendMessage(gpsSettings.Rate);
            // send the RTK message to the GPS
            SetRTK(gpsSettings.RTKEnabled);
            // write start up configuration, will also start the sensor
            WriteStartUp();
            IsConnected = true;
            return true;
        }

        /// <summary>
        /// Closes the connection to sensor.
        /// </summary>
        public async Task<bool> Abort()
        {
            bool stoppedGps = true;
            // see if GPS sensor CAN stop
            if (!isSetUp.IsSet)
            {
                LogMessage("Failed to disconnect: GPS sensor settings not set.");
                if (IsProcessing)
                    await processor.AbortAndWait();
                return false;
            }

            try
            {
                if (!gpsPort.IsOpen)
                {
                    IsConnected = false;
                    if (IsProcessing)
                        await processor.AbortAndWait();
                    return true;
                }
            }
            catch (Exception e)
            {
                LogMessage("Error while checking GPS port open. Exception: " + e.Message);
                stoppedGps = false;
            }

            // attempt to close the GPS sensor 
            try
            {
                gpsPort.Close();
                IsConnected = false;
            }
            catch (Exception e)
            {
                LogMessage("Error while closing GPS port. Exception: " + e.Message);
                stoppedGps = false;
            }
            IsConnected = false;

            // Stop processing GPS data
            await processor.AbortAndWait();

            return stoppedGps;
        }

        /// <summary>
        /// Disconnects the GPS and waits for the processing to finish.
        /// </summary>
        /// <returns>Whether GPS was successfully disconnected.</returns>
        public async Task<bool> StopAndProcess()
        {
            // see if the sensor CAN stop
            bool stoppedGps = true;
            if (!isSetUp.IsSet)
            {
                LogMessage("Failed to disconnect: GPS sensor settings not set.");
                if (IsProcessing)
                    await processor.AbortAndWait();
                return false;
            }

            try
            {
                if (!gpsPort.IsOpen)
                {
                    IsConnected = false;
                    if (IsProcessing)
                        await processor.AbortAndWait();
                    return true;
                }
            }
            catch (Exception e)
            {
                LogMessage("Error while checking GPS port open. Exception: " + e.Message);
                stoppedGps = false;
            }
            // close the port if able
            try 
            { 
                gpsPort.Close();
                IsConnected = false;
            }
            catch (Exception e)
            {
                LogMessage("Error while closing GPS port. Exception: " + e.Message);
                stoppedGps = false;
            }
            IsConnected = false;

            // Stop processing GPS data when collected data is done processing
            await processor.StopAndWait();

            return stoppedGps;
        }

        /// <summary>
        /// Sends a string message to the sensor if possible.
        /// </summary>
        /// <param name="message">Message to be sent to the sensor.</param>
        public void SendMessage(int message)
        {
            int CKA = 0;
            int CKB = 0;

            byte rateNum1 = (byte)message;
            byte rateNum2 = (byte)(message >> 8);
            // hard corded data to send for the start up, payload is determined through the message
            byte[] headerNum = { 0xB5, 0x62 };
            byte[] classAndID = { 0x06, 0x08 };
            byte[] payload = { rateNum1, rateNum2, 0x01, 0x00, 0x01, 0x00 };

            byte[] lengthNum = { 0x06, 0x00 };
            // ensure correct checksum is sent
            byte[] checkSumStuff = (classAndID).Concat(lengthNum).Concat(payload).ToArray();
            for (int i = 0; i < checkSumStuff.Length; i++)
            {
                CKA = (CKA + checkSumStuff[i]) % 256;
                CKB = (CKA + CKB) % 256;
            }
            byte[] checkSum = { Convert.ToByte(CKA), Convert.ToByte(CKB) };

            // B5 62 06 08 06 00 C8 00 01 00 01 00 DE 6A
            byte[] bytesToSend = headerNum.Concat(classAndID).Concat(lengthNum).Concat(payload).Concat(checkSum).ToArray();
            // write the message in hex format
            gpsPort.Write(bytesToSend, 0, bytesToSend.Length);
        }

        /// <summary>
        /// Enables or disables RTK for GPS.
        /// </summary>
        /// <param name="RTKEnable"></param>
        public void SetRTK(bool RTKEnable)
        {
            // $$ may only need to write first port and config, check and see if it works
            if (RTKEnable) // enable RTK
            {
                byte[] nav5 = { 0xb5, 0x62, 0x06, 0x24, 0x24, 0x00, 0xff, 0xff, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x10, 0x27,
                    0x00, 0x00, 0x0a, 0x00, 0xfa, 0x00, 0xfa, 0x00, 0x64, 0x00, 0x5e, 0x01, 0x00, 0x3c, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x83, 0xb4};
                byte[] cfg = { 0xb5, 0x62, 0x06, 0x24, 0x00, 0x00, 0x2a, 0x84 };
                gpsPort.Write(nav5, 0, nav5.Length);
                gpsPort.Write(cfg, 0, cfg.Length);
            }
            else // disable RTK
            {
                byte[] nav5 = { 0xb5, 0x62, 0x06, 0x24, 0x24, 0x00, 0xff, 0xff, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x10, 0x27,
                    0x00, 0x00, 0x0a, 0x00, 0xfa, 0x00, 0xfa, 0x00, 0x64, 0x00, 0x5e, 0x01, 0x00, 0x3c, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x81, 0x72 };
                byte[] cfg = { 0xb5, 0x62, 0x06, 0x24, 0x00, 0x00, 0x2a, 0x84 };
                gpsPort.Write(nav5, 0, nav5.Length);
                gpsPort.Write(cfg, 0, cfg.Length);
            }
        }

        /// <summary>
        /// Writes startup messages to the sensor.
        /// </summary>
        public void WriteStartUp()
        {
            byte[] startup1 = { 0xb5, 0x62, 0x06, 0x20, 0x14, 0x20, 0x02, 0x20, 0x20, 0x20, 0xc0, 0x08, 0x20, 0x20, 0x20, 0xc2, 0x01, 0x20, 0x20, 0x20, 0x02, 0x20, 0x20, 0x20, 0x20, 0x20, 0xc9, 0x54 };
            gpsPort.Write(startup1, 0, startup1.Length);
            byte[] startup2 = { 0xb5, 0x62, 0x06, 0x20, 0x14, 0x20, 0x03, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x23, 0x20, 0x01, 0x20, 0x20, 0x20, 0x20, 0x20, 0x41, 0xa2 };
            gpsPort.Write(startup2, 0, startup2.Length);
        }

        /// <summary>
        /// Whether manager has been setup yet.
        /// </summary>
        private readonly ManualResetEventSlim isSetUp = new ManualResetEventSlim(false);

        /// <summary>
        /// Sets settings read from config files to GPS.
        /// </summary>
        /// <param name="GPSSettings">IDictionary of GPS specific settings</param>
        /// <param name="globalSettings">IDictionary of global settings</param>
        /// <param name="outputManager">Output manager to save to</param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">Setting key not found in settings list.</exception>
        public bool SetSensorSettings(IDictionary<string, object> GPSSettings, IDictionary<string, object> globalSettings, IOutputManager outputManager)
        {
            foreach (KeyValuePair<string, object> setting in GPSSettings)
            {
                if (setting.Value.ToString() == "")
                    return false;
            }

            gpsSettings.COMPort = GPSSettings["COM Port"].ToString().Split(' ')[0];
            gpsSettings.Rate = Convert.ToInt16(GPSSettings["Rate"]);
            gpsSettings.RTKEnabled = Convert.ToBoolean(GPSSettings["RTK Enabled"]);
            gpsSettings.isSaving = Convert.ToBoolean(GPSSettings["Save Data"]);

            GPSOutputManager = outputManager;

            isSetUp.Set();
            return true;
        }

        /// <summary>
        /// Resets output channel to value specified.
        /// </summary>
        /// <param name="outputChannel">New output channel for GPS data.</param>
        /// <returns>Whether set channel successfully.</returns>
        public bool SetChannel(Channel<GpsData> outputChannel)
        {
            gpsOutputData = outputChannel;

            return processor.SetChannel(outputChannel);
        }

        #region Event

        private readonly GpsDataProcessing processor;

        /// <summary>
        /// Data received serial port event for the GPS sensor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataReceivedEvent(object sender, SerialDataReceivedEventArgs e)
        {
            int bytes = gpsPort.BytesToRead;
                byte[] buffer = new byte[bytes];

            gpsPort.Read(buffer, 0, bytes);
            processor.AddToList(buffer, GPSOutputManager,  gpsSettings.isSaving);   
        }

        #endregion
    }
}
