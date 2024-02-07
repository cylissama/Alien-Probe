using System.Collections.Generic;
using System.Threading.Channels;
using System.ComponentModel; // for INotifyPropertyChanged interface
using System.Runtime.CompilerServices; // for [CallerMemberName] in NotifyPropertyChanged
using CES.AlphaScan.Base;
using System.Threading.Tasks;
using System;

namespace CES.AlphaScan.Gps
{
    public class GPSManager: ISensorManager, INotifyPropertyChanged, ILogMessage
    {
        /*
                  Manager for controlling the GPS sensor. This allows for turning the sensor on/off or configuring the sensor to prepare for data input
                  This also includes other types of error handling for displaying errors to the user if any should occur. This does not do any data
                  processing, activates the sensor module which will begin setting up functionality for data collected/processing.
        */

        private GpsSensorModule sensorModule = null;

        private Channel<GpsData> GPSData = null;
        public ChannelReader<GpsData> GPSDataOut { get { return GPSData?.Reader; } }

        public readonly System.Collections.Concurrent.ConcurrentQueue<GpsData> DataToAddToMap = new System.Collections.Concurrent.ConcurrentQueue<GpsData>();

        public event Action AddDataToMap
        {
            add { sensorModule.AddDataToMap += value; }
            remove { sensorModule.AddDataToMap -= value; }
        }

        #region Logging

        /// <summary>
        /// Name of the GPS manager.
        /// </summary>
        public string Name { get; } = "GPS";

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

        #region Property Notification

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
        /// Whether or not the GPS sensor is currently connected.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (sensorModule != null)
                    return sensorModule.IsConnected;
                else return false;
            }
        }

        /// <summary>
        /// Whether or not the GPS sensor is currently running. Always running if connected.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                if (sensorModule != null)
                    return sensorModule.IsConnected;
                else return false;
            }
        }

        /// <summary>
        /// Whether GPS data is currently being processed.
        /// </summary>
        public bool IsProcessing
        {
            get
            {
                if (sensorModule != null)
                    return sensorModule.IsProcessing;
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
                case nameof(sensorModule.IsConnected):
                    NotifyPropertyChanged(nameof(this.IsConnected));
                    NotifyPropertyChanged(nameof(this.IsConnected));
                    break;
                case nameof(sensorModule.IsProcessing):
                    NotifyPropertyChanged(nameof(this.IsProcessing));
                    break;
                default:
                    break;
            }
        }
        #endregion

        public GPSManager()
        {
            SetUp();
        }

        /// <summary>
        /// Sets up manager.
        /// </summary>
        public void SetUp()
        {
            GPSData = Channel.CreateUnbounded<GpsData>();

            if (sensorModule == null)
            {
                sensorModule = new GpsSensorModule(GPSData, DataToAddToMap);
                sensorModule.PropertyChanged += GPS_PropertyChanged;
                sensorModule.MessageLogged += LogMessage;
            }
            else
            {
                if (!sensorModule.SetChannel(GPSData))
                    LogMessage("Failed to reset GPS output channel.");
            }
        }

        /// <summary>
        /// Starts the GPS and processing. If already running, aborts and restarts.
        /// </summary>
        /// <returns>Whether GPS successfully started.</returns>
        public bool Start()
        {
            if (sensorModule.IsConnected)
                if (!sensorModule.Abort().Result)
                    LogMessage("Failed to restart GPS; failed to disconnect GPS.");

            // Set up manager if channel is dead or something is null
            if (true || GPSData == null || GPSDataOut.Completion.IsCompleted || sensorModule == null)
            {
                // Disconnect to stop processing loop.
                if (sensorModule.IsProcessing)
                    sensorModule.Abort().Wait();
                SetUp();
            }

            return sensorModule.Start();
        }

        /// <summary>
        /// Disconnects the GPS and stops processing.
        /// </summary>
        /// <returns>Whether GPS successfully stopped.</returns>
        public async Task<bool> Abort()
        {
            if (sensorModule.IsConnected || sensorModule.IsProcessing)
            {
                if (!await sensorModule.Abort())
                    LogMessage("Failed to disconnect GPS.");
            }

            return !this.IsConnected;
        }

        /// <summary>
        /// Disconnects the GPS, but waits for processing to complete.
        /// </summary>
        /// <returns>Whether GPS successfully stopped.</returns>
        public async Task<bool> StopAndProcess()
        {
            if (sensorModule.IsConnected || sensorModule.IsProcessing)
                if (!await sensorModule.StopAndProcess())
                    LogMessage("Failed to disconnect GPS.");

            return !this.IsConnected;
        }

        /// <summary>
        /// Sets settings in the GPS manager with the values given.
        /// </summary>
        /// <param name="sensorSettings">Settings for the GPS specifically.</param>
        /// <param name="globalSettings">Global program settings.</param>
        /// <param name="outputManager">The output manager to use to output data.</param>
        /// <returns>Whether successful in setting settings.</returns>
        public bool SetSettings(IDictionary<string, object> sensorSettings, IDictionary<string, object> globalSettings, IOutputManager outputManager)
        {
            try
            {
                return sensorModule.SetSensorSettings(sensorSettings, globalSettings, outputManager);
            }
            catch (KeyNotFoundException e)
            {
                LogMessage("Failed to set GPS settings. " + e.GetType().FullName + ": " + e.Message);
                return false;
            }
        }
    }
}
