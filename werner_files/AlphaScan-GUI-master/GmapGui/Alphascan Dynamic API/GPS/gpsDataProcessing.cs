using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CES.AlphaScan.Base;
using System.ComponentModel; // for INotifyPropertyChanged interface
using System.Runtime.CompilerServices; // for [CallerMemberName] in NotifyPropertyChanged

namespace CES.AlphaScan.Gps
{
    /// <summary>
    /// Object that parses the incoming GPS data bytes and outputs a channel of GPS data objects.
    /// </summary>
    public class GpsDataProcessing : INotifyPropertyChanged, ILogMessage
    {
        /*
            Processes for analyzing GPS packet data using the read serial port data. This allows for processing of both location and time values for
            GPS data to get a lat long with an associated time stamp. This involvesreading the header data of a packet which includes a class ID that
            is used for determining what type of data is being used within the packet. Only two types of data is considered within this program for faster
            processing and simpler analysis

            Types of data analyzed:
                   CLASS1007: Time values for each packet, given in UTC 
                   CLASS1020: Lat/long values for each packet
        */         

        #region Logging

        /// <summary>
        /// Name of the GPS data processor.
        /// </summary>
        public string Name { get; protected set; } = "GPSDataProcessor";

        /// <summary>
        /// Logs message string.
        /// </summary>
        /// <param name="message">Message to log.</param>
        private void LogMessage(string message)
        {
            MessageLogged?.Invoke(this, new LogMessageEventArgs(message, Name));
        }

        public event EventHandler<LogMessageEventArgs> MessageLogged;
        #endregion

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

        private bool _isProcessing = false;
        /// <summary>
        /// Whether GPS data is currently being processed.
        /// </summary>
        public bool IsProcessing
        {
            get
            {
                return _isProcessing;
            }
            private set
            {
                if (_isProcessing != value)
                {
                    _isProcessing = value;
                    NotifyPropertyChanged();
                }
            }
        }

        #endregion

        // variables
        List<string> messageInputs = new List<string>();
        List<string> latInputs = new List<string>();        //$$ maybe make local var in classID1020
        List<string> longInputs = new List<string>();       //$$ maybe make local var in classID1020

        /// <summary>
        /// list for storing GPS data received from sensor
        /// </summary>
        private List<byte> byteVec = new List<byte>();

        /// <summary>
        /// lock to make modification of byteVec threadsafe
        /// </summary>
        private readonly object byteVecLock = new object();

        /// <summary>
        /// Thread for GPS data processing loop.
        /// </summary>
        private Thread processThread;

        /// <summary>
        /// A queue of GPS data points to plot to a map.
        /// </summary>
        private readonly System.Collections.Concurrent.ConcurrentQueue<GpsData> addToMapQueue;

        public event Action AddDataToMap;

        /// <summary>
        /// Output channel for processed GPS data.
        /// </summary>
        private Channel<GpsData> gpsOutputData;

        /// <summary>
        /// Constructor for GPS data processing
        /// </summary>
        /// <param name="outputChannel">channel to output processed GPS data to</param>
        /// <param name="addToMapQueue">queue to add GPS data to the GUI map</param>
        public GpsDataProcessing(Channel<GpsData> outputChannel, System.Collections.Concurrent.ConcurrentQueue<GpsData> addToMapQueue = null)
        {
            gpsOutputData = outputChannel;
            this.addToMapQueue = addToMapQueue;
            if (addToMapQueue == null)
                throw new ArgumentNullException( nameof(addToMapQueue) + " is null.");
        }

        /// <summary>
        /// creates thread for the processor of GPS serial port data
        /// </summary>
        /// <param name="outputManager">Output manager to save data to.</param>
        /// <param name="isSaving">Whether or not to save data.</param>
        public void CreateProcessorThread(IOutputManager outputManager, bool isSaving)
        {
            processThread = new Thread(() =>
            {
                try
                {
                    StartProcessor(outputManager, isSaving);
                }
                catch (Exception e)
                {
                    LogMessage("GPS processor thread failed. Exception: " + e.Message);
                }
            });
            processThread.Name = "GPSPacketProcessThread";
            processThread.IsBackground = true;
            processThread.Priority = ThreadPriority.Lowest;
            processThread.Start();
        }

        /// <summary>
        /// Adds data to the byte vector for processing. Also starts new processing loop if
        /// not already running.
        /// </summary>
        /// <param name="input"> data input in bytes to be added to list</param>
        /// <param name="outputManager">Output manager to save data to.</param>
        /// <param name="isSaving">Whether or not to save data.</param>
        public void AddToList(byte[] input, IOutputManager outputManager, bool isSaving)
        {
            if (cancelGPSProcessor.IsCancellationRequested)
                return;

            // ensure the port reading thread and processing is threadsafe
            lock (byteVecLock)
            {
                byteVec.AddRange(input.ToList());
            }

            if (loopRunning.Wait(0))
            {
                try
                {
                    CreateProcessorThread(outputManager, isSaving);
                }
                finally
                {
                    loopRunning.Release();
                }
            }
        }

        /// <summary>
        /// Handles the cancelling of the processing loop. Used in <see cref="Stop"/> and <see cref="StopAndWait"/>
        /// </summary>
        private CancellationTokenSource cancelGPSProcessor = new CancellationTokenSource();

        /// <summary>
        /// Prevents multiple instances of the loop started in <see cref="StartProcessor"/> from running simultaneously.
        /// </summary>
        private SemaphoreSlim loopRunning = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Starts a loop that uses collected GPS data to process the GPS data.
        /// </summary>
        /// <param name="outputManager">Output manager to save data to.</param>
        /// <param name="isSaving">Whether or not to save data.</param>
        /// <exception cref="InvalidOperationException">Thrown if attempting to start loop while loop is already running.</exception>
        public void StartProcessor(IOutputManager outputManager, bool isSaving)
        {
            // Only allow one loop to run at a time.
            if (!loopRunning.Wait(0))
            {
                // Only one loop can run at once.
                throw new InvalidOperationException("Attempted to start GPS processor loop when it was already running.");
            }

            try
            {
                IsProcessing = true;

                //Clear old data
                lock (dataToSaveLock)
                {
                    dataToSave.Clear();
                }

                cancelGPSProcessor = new CancellationTokenSource();

                LatLng latLng = default;
                int updateDelay = 0;

                //Processing loop
                while (true)
                {
                    //Lock needed for thread safety
                    lock (byteVecLock)
                    {
                        if (byteVec.Count <= 92)     // minimum possible length of desired packet to ensure no errors occur     
                            if (cancelGPSProcessor.IsCancellationRequested)
                                break;
                            else
                                continue;
                        // find magic number and see if packet is a valid size (minimum is 92)
                        int idx = IsMagic(byteVec);
                        if (idx == -1 || byteVec.Count <= idx + 92)
                            continue;

                        int classId = byteVec[idx + 2] * 1000 + byteVec[idx + 3];   // make sure theres no overlap
                        switch (classId)
                        {
                            case 1020:
                                latLng = ClassID1020(byteVec.GetRange(idx, 36), 36);
                                byteVec.RemoveRange(0, idx + 36);
                                break;
                            case 1007:
                                GpsData addData = ClassID1007(byteVec.GetRange(idx, 92), latLng, 92);
                                // this sends updates to the GUI for GPS location of sensor, currently at every 3 packets (300 ms)
                                if (updateDelay++ == 3)
                                {
                                    //$$ this is a whole thing. Static bad so we should use bubble up public...
                                    // properties or output channel. Channel good so why not use one.
                                    addToMapQueue?.Enqueue(addData);
                                    updateDelay = 0;

                                    AddDataToMap?.Invoke();
                                }

                                //Add data to channel (and save to file is set to)
                                Task.Run(() => this.AddToChannel(addData, outputManager, isSaving));
                                byteVec.RemoveRange(0, idx + 92);
                                break;
                            case 5001:
                                byteVec.RemoveRange(0, idx + 10);
                                break;
                            default:
                                byteVec.RemoveRange(idx, 3);
                                break;
                        }
                    }
                }
            }
            finally
            {
                IsProcessing = false;
                gpsOutputData?.Writer.TryComplete();

                //Release semaphore after loop completes.
                loopRunning.Release();
            }
        }

        /// <summary>
        /// Requests for the processing loop to stop if not already requested. Waits for the loop to stop.
        /// </summary>
        /// <returns>Awaitable task that completes when the processing loop stops.</returns>
        public async Task StopAndWait()
        {
            cancelGPSProcessor.Cancel();
            await loopRunning.WaitAsync();
            loopRunning.Release();
        }

        /// <summary>
        /// Requests for the processing loop to stop, if not already requested.
        /// </summary>
        public void Stop()
        {
            cancelGPSProcessor.Cancel();
        }

        /// <summary>
        /// Attempts to abort the processing loop, and waits for it to finish.
        /// </summary>
        public async Task AbortAndWait()
        {
            cancelGPSProcessor.Cancel();
            lock (byteVecLock)
            {
                byteVec.Clear();
            }
            await loopRunning.WaitAsync();
            loopRunning.Release();
        }

        private List<ICsvWritable> dataToSave = new List<ICsvWritable>();

        private readonly object dataToSaveLock = new object();

        private readonly SemaphoreSlim savingData = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Writes data to output channel. Saves data to output manager if set to.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="outputManager"></param>
        /// <param name="isSaving"></param>
        public async void AddToChannel(GpsData input, IOutputManager outputManager, bool isSaving)
        {
            if (input.Lat == 0 && input.Long == 0)
                return;

            try
            {
                await gpsOutputData.Writer.WriteAsync(input);
            }
            catch (Exception e)
            {
                LogMessage("Failed to write GPS data to channel. " + e.GetType().FullName + ": " + e.Message);
                if (gpsOutputData == null || gpsOutputData.Reader.Completion.IsCompleted)
                    cancelGPSProcessor.Cancel();
                return;
            }
            

            if (isSaving)
            {
                // thread safe add data to the list for saving
                lock (dataToSaveLock)
                {
                    dataToSave.Add(input);
                }
                // wait until no data is currently being saved
                if (savingData.Wait(0))
                {
                    try
                    {
                        List<ICsvWritable> data = null;
                        lock (dataToSaveLock)
                        {
                            data = dataToSave.ToList();
                            dataToSave.Clear();
                        }
                        if (!outputManager.TrySaveData("GPS", data))
                        {
                            _ = Task.Run(() => LogMessage("Failed to save GPS data."));
                        }
                    }
                    finally
                    {
                        // release saving resoucres once done saving
                        savingData.Release();
                    }
                }
                
            }
        }

        /// <summary>
        /// finds the magic number from a buffer input
        /// </summary>
        /// <param name="buffer">list of bytes collected from the GPS sensor</param>
        /// <returns></returns>
        private static int IsMagic(List<byte> buffer)
        {
            for (int i = 0; i < buffer.Count - 2; i++)
            {
                if (buffer[i] == 181 && buffer[i + 1] == 98)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// parses message from sensor to find lat lng data that can be saved latter
        /// </summary>
        /// <param name="buffer">input list of data from the sensor</param>
        /// <param name="packetLength">length of data packet</param>
        private LatLng ClassID1020(List<byte> buffer, int packetLength)
        {
            double lat = -1;
            double lng = -1;
            LatLng output = new LatLng(lat, lng);
            for (int idx = 6; idx < packetLength; idx++)    // start at 7, equivalent to skipping header
            {
                if (idx >= 14 && idx < 18 || idx == 30)  // check if these numbers work
                {
                    longInputs.Add(buffer[idx].ToString("X"));      // convert long input to hex

                    for (int i = 0; i < longInputs.Count; i++)      // check if hex vals have two place values
                        if (longInputs[i].Length < 2)
                            longInputs[i] = longInputs[i].Insert(0, "0");
                    // convert the values from hex valeus into integers and convert into an int32 to get into their correct values
                    if (idx == 30 && longInputs.Count == 5)
                    {
                        Int32 lngPlusProcess = Convert.ToInt32(int.Parse(longInputs[4], System.Globalization.NumberStyles.HexNumber));
                        Int32 lngProcess = Convert.ToInt32(int.Parse(longInputs[3] + longInputs[2] + longInputs[1] + longInputs[0], System.Globalization.NumberStyles.HexNumber));
                        // will become a double and converted into an accurate lat/long value
                        output.Lng = ((Convert.ToDouble(lngProcess) * Math.Pow(10, -7)) + (Convert.ToDouble(lngPlusProcess) * Math.Pow(10, -9)));
                        longInputs.Clear();
                    }
                }

                messageInputs.Add(buffer[idx].ToString("X")); // convert data into hex
                for (int i = 0; i < messageInputs.Count; i++)
                {
                    if (messageInputs[i].Length < 2)
                        messageInputs[i] = messageInputs[i].Insert(0, "0"); // ensure the correct hex values will be displayed
                }

                if (idx >= 18 && idx < 22 || idx == 31) // add X string for the deivision of the hexidecimal values
                {
                    latInputs.Add(buffer[idx].ToString("X"));
                    for (int i = 0; i < latInputs.Count; i++)
                    {
                        if (latInputs[i].Length < 2)        // ensure correct hex values will be displayed
                            latInputs[i] = latInputs[i].Insert(0, "0");
                    }

                    if (idx == 31 && latInputs.Count == 5)  // find latitude values of the packet
                    {
                        // parse the hexidecimal values into the correct values
                        Int32 latPlusProcess = Convert.ToInt32(int.Parse(latInputs[4], System.Globalization.NumberStyles.HexNumber));
                        Int32 latProcess = Convert.ToInt32(int.Parse(latInputs[3] + latInputs[2] + latInputs[1] + latInputs[0], System.Globalization.NumberStyles.HexNumber));
                        // convert integer prased lat long valeus into the correct lat double value
                        output.Lat = (Convert.ToDouble(latProcess) * Math.Pow(10, -7)) + (Convert.ToDouble(latPlusProcess) * Math.Pow(10, -9));
                        latInputs.Clear();
                    }
                }
            }
            return output;
        }

        /// <summary>
        /// finds time, fix flag, fix type, and if RTK is enabled, combine with lat lng to get a complete packet of data
        /// </summary>
        /// <param name="buffer">input list of data received from the sensor</param>
        /// <param name="packetLength">length of packet to search through</param>
        /// <returns>output of combining data to be added to channel or saved</returns>
        private GpsData ClassID1007(List<byte> buffer, LatLng latLng, int packetLength)
        {
            string fixType = null;
            string fixFlagType = null;
            bool RTKEnabled = false;
            GpsData output = new GpsData(default, 0, 0, false, null, null);

            // consider changing this to a struct?
            int yearBuffer = -1;
            int monthBuffer = -1;
            int dayBuffer = -1;
            int hourBuffer = -1;
            int minuteBuffer = -1;
            int secondBuffer = -1;
            int miliSecBuffer = -1;

            
            List<string> utcInputs = new List<string>();
            List<string> fixInputs = new List<string>();

            for (int idx = 6; idx < packetLength; idx++)   // starts at 7 equivalent of skipping header
            {
                string inputCombination;
                int bufferCombination;

                if (idx >= 10 && idx < 17 | (idx >= 22 && idx < 26))
                {
                    utcInputs.Add(buffer[idx].ToString("X"));
                    int utcCount = utcInputs.Count;
                }

                switch (idx)     // for two index cases
                {
                    case 11:    //YEAR
                        inputCombination = utcInputs[1] + utcInputs[0];
                        bufferCombination = Convert.ToInt32(int.Parse(inputCombination, System.Globalization.NumberStyles.HexNumber));
                        yearBuffer = bufferCombination;
                        break;

                    case 12:    //MONTH
                        inputCombination = utcInputs[2];
                        bufferCombination = Convert.ToInt32(int.Parse(inputCombination, System.Globalization.NumberStyles.HexNumber));
                        monthBuffer = bufferCombination;

                        break;

                    case 13:    //DAY
                        inputCombination = utcInputs[3];
                        bufferCombination = Convert.ToInt32(int.Parse(inputCombination, System.Globalization.NumberStyles.HexNumber));
                        dayBuffer = bufferCombination;
                        break;

                    case 14:    //HOUR
                        inputCombination = utcInputs[4];
                        bufferCombination = Convert.ToInt32(int.Parse(inputCombination, System.Globalization.NumberStyles.HexNumber));
                        hourBuffer = bufferCombination;
                        break;

                    case 15:    //MINUTE
                        inputCombination = utcInputs[5];
                        bufferCombination = Convert.ToInt32(int.Parse(inputCombination, System.Globalization.NumberStyles.HexNumber));
                        minuteBuffer = bufferCombination;
                        break;

                    case 16:    //SECOND
                        inputCombination = utcInputs[6];
                        bufferCombination = int.Parse(inputCombination, System.Globalization.NumberStyles.HexNumber);
                        // this time seems to be slightly off by 4 seconds? check this
                        secondBuffer = bufferCombination;

                        break;

                    case 25:    //MILISECOND
                        inputCombination = utcInputs[10] + utcInputs[9] + utcInputs[8] + utcInputs[7];

                        bufferCombination = Convert.ToInt32(int.Parse(inputCombination, System.Globalization.NumberStyles.HexNumber));
                        miliSecBuffer = (int)Math.Abs(bufferCombination);
                        utcInputs.Clear();

                        break;

                    case 26:    //FIX TYPE
                        fixInputs.Add(buffer[idx].ToString("X"));
                        for (int i = 0; i < fixInputs.Count; i++)
                        {
                            if (fixInputs[i].Length < 2)
                                fixInputs[i] = fixInputs[i].Insert(0, "0");
                        }
                        {       // unsure what this bracket is for in original code
                            Int32 fixNum = Convert.ToInt32(int.Parse(fixInputs[0], System.Globalization.NumberStyles.HexNumber));
                            switch (fixNum)
                            {
                                case 0:
                                    fixType = "No Fix";
                                    break;
                                case 1:
                                    fixType = "dead reckoning only";
                                    break;
                                case 2:
                                    fixType = "2D-fix";
                                    break;
                                case 3:
                                    fixType = "3D-fix";
                                    break;
                                case 4:
                                    fixType = "GNSS + dead reckoning combined";
                                    break;
                                case 5:
                                    fixType = "Time Fix";
                                    break;
                            }
                            if (fixType == "3D-fix" || fixType == "GNSS + dead reckoning combined") // find RTK type
                                RTKEnabled = true;
                            else
                                RTKEnabled = false;

                            fixInputs.Clear();
                        }
                        break;

                    case 27:        // this is the last location needed to be searched within the packet, FIX FLAG
                        fixInputs.Add(buffer[idx].ToString("X"));

                        for (int i = 0; i < fixInputs.Count; i++)
                        {
                            if (fixInputs[i].Length < 2)
                                fixInputs[i] = fixInputs[i].Insert(0, "0");
                        }
                        int fixFlagNum = Convert.ToInt32(int.Parse(fixInputs[0], System.Globalization.NumberStyles.HexNumber));
                        if (fixFlagNum <= 63)
                            fixFlagType = "Just Fix";
                        else if (fixFlagNum <= 127)
                            fixFlagType = "Float";
                        else
                            fixFlagType = "Fixed";
                        // convert all of the calculated time values into a datetime object for use in data combination
                        DateTime dt = new DateTime(yearBuffer, monthBuffer, dayBuffer, hourBuffer, minuteBuffer, secondBuffer);
                        dt = dt.AddTicks(miliSecBuffer/100);

                        output = new GpsData(dt, latLng.Lat, latLng.Lng, RTKEnabled, fixType, fixFlagType);
                        return output;
                }
            }
            fixInputs.Clear();
            return output;
        }

        /// <summary>
        /// Sets the output channel to a new channel. Fails if processing loop is running.
        /// </summary>
        /// <param name="outputChannel">New output channel.</param>
        /// <returns>Whether new output channels was successfully set.</returns>
        public bool SetChannel(Channel<GpsData> outputChannel)
        {
            if (loopRunning.Wait(0))
            {
                try
                {
                    gpsOutputData = outputChannel;
                    return true;
                }
                finally
                {
                    loopRunning.Release();
                }
            }
            else
            {
                LogMessage("Failed to set channel: loop running.");
                return false;
            }
            
        }
    }
}
