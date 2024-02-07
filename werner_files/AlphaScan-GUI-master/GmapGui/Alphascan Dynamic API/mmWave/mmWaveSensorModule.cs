using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.ComponentModel; // for INotifyPropertyChanged interface
using System.Runtime.CompilerServices; // for [CallerMemberName] in NotifyPropertyChanged
using System.Threading.Tasks;
using System.Threading;
using CES.AlphaScan.Base;
using CES.AlphaScan.Acquisition;
using System.Threading.Channels;

namespace CES.AlphaScan.mmWave
{
    /// <summary>
    /// struct of mmWave settings that are needed
    /// </summary>
    public struct mmWaveSettings
    {
        public string UARTPortName;
        public string DATAPortName;
        public IOutputManager outputManager;
        public bool isSaving;
        public string configDirectory;
        public VehicleSide vehicleSide;
    }

    /// <summary>
    /// Module for controlling connection and communication with a mmWave sensor. Receives data the sensor reads.
    /// </summary>
    public class mmWaveSensorModule: ImmWaveSensorModule, INotifyPropertyChanged, ILogMessage
    {
        /*
            Functionality for fully controlling the sensors. Data analyzation begins here as this is where the serial port reading
            functionality begins. This module can set important details of the sensor such as the settings, control the baseline level of the sensors functionality
            and contains error handling and checking for the sensors should an error occur. These functions are not normally accessed due to their lower level nature
            and should mostly be accessed from functions in the mmWave manager.
        */ 

        #region Logging

        /// <summary>
        /// Name of the mmWave sensor module.
        /// </summary>
        public string Name { get; protected set; } = "mmWaveSensor";

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

        public mmWaveSettings mmWaveSetting = new mmWaveSettings();

        public SerialPort UARTPort { get; protected set; }

        public SerialPort DATAPort { get; protected set; }

        //Properties that notify of changes.
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
        /// Whether or not the reader is currently connected.
        /// </summary>
        private bool _isConnected = false;
        public bool IsConnected
        {
            get
            {
                return _isConnected;
            }
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Whether or not the reader is currently running.
        /// </summary>
        private bool _isRunning = false;
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

        /// <summary>
        /// Whether or not the readeing loop is currently running.
        /// </summary>
        private bool _isProcessing = false;
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

        #endregion

        /// <summary>
        /// Constructs new mmWave sensor driver.
        /// </summary>
        public mmWaveSensorModule()
        {

        }

        /// <summary>
        /// Constructs new mmWave sensor driver using the specified configuration settings.
        /// </summary>
        /// <param name="mmWaveSettings">IDictionary of mmWave specific settings.</param>
        /// <param name="globalSettings">IDictionary of global settings.</param>
        /// <param name="outputManager">Output Manager to save to.</param>
        public mmWaveSensorModule(IDictionary<string, object> mmWaveSettings, IDictionary<string, object> globalSettings, OutputManager outputManager)
        {
            SetSensorSettings(mmWaveSettings, globalSettings, outputManager);
        }

        /// <summary>
        /// Opens the connection to sensor.
        /// </summary>
        public bool Connect()
        {
            try
            {
                //Check if ports are already connected. If only one is connected, disconnect it.
                if (DATAPort != null)
                {
                    if (UARTPort != null)
                    {
                        if (DATAPort.IsOpen && UARTPort.IsOpen)
                            return true;
                    }

                    if (DATAPort.IsOpen)
                        DATAPort.Close();
                }
                if (UARTPort != null && UARTPort.IsOpen)
                    UARTPort.Close();
                IsConnected = false;
            }
            catch (Exception e) when (e is System.IO.IOException || e is ArgumentNullException || e is ArgumentException)
            {
                LogMessage("Failed to close mmWave ports. " + e.GetType().FullName + ": " + e.Message);
            }
            // set up UART and DATA ports
            try
            {
                UARTPort = mmWavePorts.SetUpUART(mmWaveSetting.UARTPortName);
                DATAPort = mmWavePorts.SetupDATA(mmWaveSetting.DATAPortName);
            }
            catch (Exception e) when (e is System.IO.IOException || e is ArgumentException 
            || e is ArgumentNullException || e is InvalidOperationException || e is ArgumentOutOfRangeException)
            {
                LogMessage("Failed to set up mmWave ports. " + e.GetType().FullName + ": " + e.Message);
                IsConnected = false;
                return false;
            }
            // open UART and DATA port to ensure connection of the ports
            try
            {
                UARTPort.Open();
                DATAPort.Open();
                IsConnected = true;
                return true;
            }
            catch
            {
                // If fail to open ports, not connected.
                IsConnected = false;
                return false;
            }
        }

        /// <summary>
        /// Closes the connection to sensor.
        /// </summary>
        public bool Disconnect()
        {
            // wait until the sensor stops reading
            if (!readingDataPortLoop.Wait(0))
            {
                LogMessage("mmWave data port reading loop is running.");
                return false;
            }
            // see if ports CAN be closed (e.g. port is open, is initiaized)
            try
            {
                //$? Should this be an error case?
                if (UARTPort == null || DATAPort == null)
                {
                    LogMessage("Failed to disconnect mmWave: Port(s) are null.");
                    IsConnected = false;
                    return false;
                }
                // close port if it is able to
                try
                {
                    if (!UARTPort.IsOpen && !DATAPort.IsOpen)
                    {
                        IsConnected = false;
                        return true;
                    }

                    UARTPort.Close();
                    DATAPort.Close();
                }
                catch (Exception e)
                {
                    LogMessage("Failed to close mmWave ports. Exception: " + e.Message);
                    return false;
                }
                IsConnected = false;
                return true;
            }
            finally // free the resources from reading data if the ports have closed
            {
                readingDataPortLoop.Release();
            }
        }

        /// <summary>
        /// Begins sensor reading session.
        /// </summary>
        /// <returns>True if successfully began reading.</returns>
        public bool Start()
        {
            try
            {
                //Disconnect from sensor if already connected.
                Disconnect();
                // read the mmWave config file to start the sensor, then open the data port to begin reveiving data
                mmWaveStartUp.ReadCfgFile(UARTPort, mmWaveSetting.configDirectory);
                DATAPort.Open();

                IsRunning = true;
                // cancel port reading if needed using a cancellation token
                cancelPortReading = new CancellationTokenSource();
                // start a new thead for reading the data port thread
                dataPortReadingThread = new Thread(() =>
                {
                    try
                    {
                        ReadPortLoop();
                    }
                    catch (Exception e)
                    {
                        LogMessage("Read port loop error. Exception: " + e.Message);
                    }
                })
                {
                    Name = "mmWaveDataPortReaderThread",
                    IsBackground = true
                };
                dataPortReadingThread.Start();

                return true;
            }
            catch (Exception e)
            {
                LogMessage("Failed to start mmWave sensor. Exception: " + e.Message);
                IsRunning = false;
                rawByteOutChannel?.Writer.TryComplete();
                return false;
            }
        }

        /// <summary>
        /// Ends sensor reading session.
        /// </summary>
        /// <returns>True if successfully ended sensor reading session.</returns>
        public async Task<bool> Stop()
        {
            // Try to connect to sensor if not connected.
            if (Connect())
            {
                // Send stop command
                if (SendMessage("sensorStop"))
                    IsRunning = false;
                else
                    LogMessage("Failed to stop mmWave: Failed to send message to mmWave sensor.");

                // Stop port reading loop
                cancelPortReading?.Cancel();
                await readingDataPortLoop.WaitAsync();
                readingDataPortLoop.Release();

                return !IsRunning;
            }
            else
            {
                LogMessage("Failed to connect to mmWave sensor.");

                // Stop port reading loop
                cancelPortReading?.Cancel();
                await readingDataPortLoop.WaitAsync();
                readingDataPortLoop.Release();

                return false;
            }

        }

        /// <summary>
        /// Sends a string message to the sensor if possible.
        /// </summary>
        /// <param name="message">Message to be sent to the sensor.</param>
        /// <exception cref="Exception">Exception thrown if fails to write to port.</exception>
        public bool SendMessage(string message)
        {
            // use the UART port to write a message to the sensor
            if (UARTPort == null) return false;
            try
            {
                UARTPort.WriteLine(message);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Uses read settings from config to set mmWave settings
        /// </summary>
        /// <param name="mmWaveSettings">IDictionary of mmWave specific settings</param>
        /// <param name="globalSettings">IDictionary of global settings</param>
        /// <param name="outputManager">Output Manager to save to</param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">Setting key not found in settings list.</exception>
        /// <exception cref="FormatException">Setting value is not in a format that can be converted.</exception>
        /// <exception cref="InvalidCastException">Setting value is not a type that can be converted.</exception>
        /// <exception cref="OverflowException">Setting value is too large for the data type it is to be converted to.</exception>
        public bool SetSensorSettings(IDictionary<string, object> mmWaveSettings, IDictionary<string, object> globalSettings, IOutputManager outputManager)
        {
            foreach (KeyValuePair<string, object> setting in mmWaveSettings)
            {
                if (setting.Value.ToString() == "")
                    return false;
            }

            mmWaveSetting.UARTPortName = mmWaveSettings["UART Port"].ToString().Split(' ')[0];
            mmWaveSetting.DATAPortName = mmWaveSettings["DATA Port"].ToString().Split(' ')[0];
            mmWaveSetting.isSaving = Convert.ToBoolean(mmWaveSettings["Save Data"]);
            mmWaveSetting.outputManager = outputManager;
            mmWaveSetting.configDirectory = mmWaveSettings["Config Directory"].ToString();
            mmWaveSetting.vehicleSide = (VehicleSide)Convert.ToInt16(globalSettings["Vehicle Side"]);
            return true;
        }

        #region Reading Data Port

        private Thread dataPortReadingThread;

        private Channel<byte[]> rawByteOutChannel = null;

        /// <summary>
        /// Cancels the loop for reading mmWave bytes.
        /// </summary>
        private CancellationTokenSource cancelPortReading;

        /// <summary>
        /// Prevents multiple instances of the read port loop from running simultaneously.
        /// </summary>
        private readonly SemaphoreSlim readingDataPortLoop = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Starts a loop that reads raw data from the data port and writes the byte arrays to the output channel.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if loop is already running.</exception>
        private void ReadPortLoop()
        {
            if (!readingDataPortLoop.Wait(0))
            {
                throw new InvalidOperationException("mmWave data port reading loop already running.");
            }

            try
            {
                // numbers of bytes needed to read nad the buffer of the byte data read
                int numToRead;
                byte[] buffer;
                // cancellation token for the port when needed to be stopped
                cancelPortReading = new CancellationTokenSource();
                IsProcessing = true;

                while (!cancelPortReading.IsCancellationRequested)
                {
                    try
                    {
                        // find number of bytes to currently read from the DATA port
                        numToRead = DATAPort.BytesToRead;
                        if (numToRead < 64) continue; //This should prevent intializing array unnecessarily.
                        buffer = new byte[numToRead];
                        // collect all bytes from the DATA port
                        DATAPort.Read(buffer, 0, numToRead);

                        //Add data buffer to queue which will then get added to byteVec.
                        if (!rawByteOutChannel.Writer.TryWrite(buffer))
                        {
                            LogMessage("Failed to write bytes to channel.");
                        }
                    }
                    // DATA port errors
                    catch (InvalidOperationException ioe)
                    {
                        if (DATAPort == null || !DATAPort.IsOpen)
                        {
                            LogMessage("Data port is not open.");
                            return;
                        }
                        else
                        {
                            LogMessage("Data port read loop failed. Exception: " + ioe.Message);
                        }
                    }
                    catch(Exception e)
                    {
                        LogMessage("Data port read loop failed. Exception: " + e.Message);
                    }
                }

                // Finish reading data; Complete rawData channel
                numToRead = DATAPort.BytesToRead;
                if (numToRead > 0)
                {
                    buffer = new byte[numToRead];
                    DATAPort.Read(buffer, 0, numToRead);
                    if (!rawByteOutChannel.Writer.TryWrite(buffer))
                    {
                        LogMessage("Failed to write bytes to channel.");
                    }
                }
                
            }
            // complete chanel and release resources after the loop is finished
            finally
            {
                IsProcessing = false;
                rawByteOutChannel?.Writer.TryComplete();

                //Release semaphore after loop completes.
                readingDataPortLoop.Release();
            }
        }

        /// <summary>
        /// Sets the output channel to a new channel. Fails if processing loop is running.
        /// </summary>
        /// <param name="rawByteChannel">New output channel.</param>
        /// <returns>Whether new output channel was successfully set.</returns>
        public bool SetChannel(Channel<byte[]> rawByteChannel)
        {
            if (readingDataPortLoop.Wait(0))
            {
                try
                {
                    try
                    {
                        rawByteOutChannel = rawByteChannel;
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                    
                }
                finally
                {
                    readingDataPortLoop.Release();
                }
            }
            else
            {
                LogMessage("Failed to set channel: loop running.");
                return false;
            }

        }

        #endregion

    }
}
