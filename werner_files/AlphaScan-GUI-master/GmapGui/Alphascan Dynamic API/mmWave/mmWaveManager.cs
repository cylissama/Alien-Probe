using System.Collections.Generic;
using System.Threading.Channels;
using System.ComponentModel; // for INotifyPropertyChanged interface
using System.Runtime.CompilerServices; // for [CallerMemberName] in NotifyPropertyChanged
using CES.AlphaScan.Base;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CES.AlphaScan.mmWave
{
    public class mmWaveManager: ISensorManager, INotifyPropertyChanged
    {
        /*
            Manager for controlling the mmWave sensor. This allows for starting, stopping, and configuring the sensor whic the commadns here will be sent to the sensor module.
            This has some error handling for upper level errors that could occur for the sensor, but most errors that could occur will more than likely happen within the sensor
            module. This manager does set up channels that mmWave sensor data will be sent to for further processing or exporting for future processes such as gelocation.
            Sensor settings are also collected within this class.
        */ 

        // classes needed to acccess
        private readonly mmWaveSensorModule sensorModule = new mmWaveSensorModule();
        private readonly mmWavePacketParsing parsePacket = new mmWavePacketParsing();
        private readonly mmWaveThreadChannels threadChannel = new mmWaveThreadChannels();

        /// <summary>
        /// Semaphore preventing select interactions with the class from occurring at the same time. Specifically Start and Stop.
        /// </summary>
        private readonly SemaphoreSlim interactionSem = new SemaphoreSlim(1,1);

        #region Logging

        /// <summary>
        /// Name of the mmWave manager.
        /// </summary>
        public string Name { get; } = "mmWave";

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

        private bool _isConnected = false;
        /// <summary>
        /// Whether or not the mmWave sensor is currently connected.
        /// </summary>
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

        private bool _isRunning = false;
        /// <summary>
        /// Whether or not the mmWave sensor is currently running.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }
            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _isProcessing = false;
        /// <summary>
        /// Whether the mmWave manager is currently processing data.
        /// </summary>
        public bool IsProcessing
        {
            get
            {
                return _isProcessing;
            }
            set
            {
                if (_isProcessing != value)
                {
                    _isProcessing = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Propogates property changes from reader module to manager.
        /// </summary>
        /// <remarks>
        /// Currently only changes in <see cref="mmWaveSensorModule.IsConnected"/> and 
        /// <see cref="mmWaveSensorModule.IsRunning"/> are propogated.
        /// </remarks>
        /// <param name="sender">Object that raised the event.</param>
        /// <param name="e">Data describing property change.</param>
        private void Sensor_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string propName = e.PropertyName;

            switch (propName)
            {
                case nameof(sensorModule.IsConnected):
                    IsConnected = sensorModule.IsConnected;
                    break;
                case nameof(sensorModule.IsRunning):
                    IsRunning = sensorModule.IsRunning;
                    break;
                default:
                    break;
            }
        }
        #endregion

        /// <summary>
        /// channels for data to use in multi-threaded exhange
        /// </summary>
        private Channel<ClusteredObject> clusterChannel;
        private Channel<PacketData> packetChannel = null;
        private Channel<byte[]> rawByteChannel;

        /// <summary>
        /// Number of threads to create in <see cref="threadChannel"/>.
        /// </summary>
        private readonly int numberProcessingThreads = 6;

        /// <summary>
        /// Reader for the output data channel.
        /// </summary>
        public ChannelReader<ClusteredObject> ClusterOut { get { return clusterChannel.Reader; } }

        /// <summary>
        /// Constructs new mmWave manager.
        /// </summary>
        public mmWaveManager()
        {
            sensorModule.PropertyChanged += Sensor_PropertyChanged;
            sensorModule.MessageLogged += LogMessage;
            parsePacket.MessageLogged += LogMessage;
            threadChannel.MessageLogged += LogMessage;
        }

        /// <summary>
        /// Sets up mmWave manager.
        /// </summary>
        private bool SetUp()
        {
            // configure all of the channels needed, unbounded to make sure there is plenty of space for data
            clusterChannel = Channel.CreateUnbounded<ClusteredObject>();
            packetChannel = Channel.CreateUnbounded<PacketData>();
            rawByteChannel = Channel.CreateUnbounded<byte[]>();
            // if the channels cannot be confitured, set up has failed and notify
            if (!sensorModule.SetChannel(rawByteChannel))
                return false;

            if (!parsePacket.SetChannel(packetChannel, rawByteChannel.Reader))
                return false;

            if (!threadChannel.SetChannel(packetChannel, clusterChannel))
                return false;

            return true;
        }

        /// <summary>
        /// Starts the mmWave.
        /// </summary>
        public bool Start()
        {
            if (!isSetUp.IsSet)
            {
                LogMessage("Failed to start: mmWave sensor settings not set.");
                return false;
            }

            if (!interactionSem.Wait(0))
            {
                LogMessage("Failed to start mmWave manager. Manager already being accessed.");
                return false;
            }
            try
            {
                //Set up manager (resets channels)
                if (!SetUp())
                    return false;

                //Start parsing loop
                parsePacket.StartProcessorThread();

                //Start processing loop
                try
                {
                    threadChannel.StartUp(numberProcessingThreads, sensorModule.mmWaveSetting.outputManager, sensorModule.mmWaveSetting.isSaving, sensorModule.mmWaveSetting.vehicleSide);
                }
                catch (Exception e)
                {
                    LogMessage("Failed to start up mmWave processing loops. " + e.GetType().FullName + ": " + e.Message);
                    return false;
                }

                IsProcessing = true;

                //Start sensor
                sensorModule.Connect();
                if (sensorModule.IsRunning) sensorModule.Stop().Wait();
                return sensorModule.Start();
            }
            finally
            {
                interactionSem.Release();
            }
        }

        /// <summary>
        /// Stops the mmWave manager. Is not guaranteed to finish processing buffered data.
        /// </summary>
        /// <returns>Whether mmWave system was aborted successfully.</returns>
        public async Task<bool> Abort()
        {
            if (!isSetUp.IsSet)
            {
                LogMessage("Failed to stop: mmWave sensor settings not set.");
                return false;
            }

            if (!interactionSem.Wait(0))
            {
                LogMessage("Failed to stop mmWave manager. Manager already being accessed.");
                return false;
            }
            try
            {
                return await StopManager();
            }
            finally
            {
                interactionSem.Release();
            }
        }

        /// <summary>
        /// Stops the mmWave manager, but finishes processing buffered data.
        /// </summary>
        /// <returns>Whether mmWave system was stopped successfully.</returns>
        public async Task<bool> StopAndProcess()
        {
            if (!isSetUp.IsSet)
            {
                LogMessage("Failed to stop: mmWave sensor settings not set.");
                return false;
            }

            if (!interactionSem.Wait(0))
            {
                LogMessage("Failed to stop mmWave manager. Manager already being accessed.");
                return false;
            }
            try
            {
                // Stop sensor and data port read loop.
                if (!await sensorModule.Stop())
                {
                    LogMessage("Failed to stop mmWave sensor.");
                    if (sensorModule.IsProcessing)
                        return false;
                }

                // Wait for all data to be parsed, then stop parsing loop
                await rawByteChannel.Reader.Completion;
                await parsePacket.StopAndWait();

                // Wait for all packets to be clustered, then stop clustering threads
                await packetChannel.Reader.Completion;
                if (!await threadChannel.StopAndWait())
                {
                    LogMessage("Failed to stop mmWave processing threads: Not all threads stopped.");
                    if (threadChannel.IsProcessing)
                        return false;
                }

                // These should all be false.
                IsProcessing = sensorModule.IsProcessing || parsePacket.IsProcessing || threadChannel.IsProcessing;

                return !IsRunning && !IsProcessing;
            }
            finally
            {
                interactionSem.Release();
            }
        }

        /// <summary>
        /// Actually stops the mmWave manager. Prevents direct public access to this functionality.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> StopManager()
        {
            if (!await sensorModule.Stop() || !sensorModule.Disconnect() || sensorModule.IsRunning)
            {
                LogMessage("Failed to stop mmWave sensor.");
            }

            await parsePacket.StopAndWait();

            if (!await threadChannel.StopAndWait())
                LogMessage("Failed to stop mmWave processing threads: Not all threads stopped.");

            IsProcessing = false;

            return !IsRunning && !IsProcessing;
        }

        /// <summary>
        /// Whether manager has been setup yet.
        /// </summary>
        private readonly ManualResetEventSlim isSetUp = new ManualResetEventSlim(false);

        /// <summary>
        /// Sets settings for the mmWave.
        /// </summary>
        /// <param name="mmWaveSettings">Settings for the mmWave.</param>
        /// <param name="globalSettings">Global settings.</param>
        /// <param name="outputManager">Output manager to save output data.</param>
        /// <returns></returns>
        public bool SetSettings(IDictionary<string, object> mmWaveSettings, IDictionary<string, object> globalSettings, IOutputManager outputManager)
        {
            isSetUp.Reset();

            if (IsRunning || IsProcessing)
            {
                if (!StopManager().Result) return false;
            }

            try
            {
                if (!sensorModule.SetSensorSettings(mmWaveSettings, globalSettings, outputManager))
                {
                    LogMessage("Failed to set mmWave sensor settings.");
                    return false;
                }
            }
            catch (Exception e)
            {
                LogMessage("Failed to set mmWave sensor settings. " + e.GetType().FullName + ": " + e.Message);
                return false;
            }
 
            if (!SetUp())
            {
                LogMessage("Failed to set up mmWave manager.");
                return false;
            }

            isSetUp.Set();
            return true;
        }
    }
}
