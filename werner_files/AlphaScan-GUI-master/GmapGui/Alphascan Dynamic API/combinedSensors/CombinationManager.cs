using System;
using System.Collections.Generic;
using System.Threading;
using CES.AlphaScan.Rfid;
using System.Threading.Channels;
using CES.AlphaScan.Base;
using CES.AlphaScan.Acquisition;
using CES.AlphaScan.mmWave;
using CES.AlphaScan.Gps;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace CES.AlphaScan.CombinedSensors
{
    public class CombinationManager : ICombinationManager, ILogMessage
    {
        #region Logging

        /// <summary>
        /// name for the Combination manager, probably shouldn't change
        /// </summary>
        public string Name { get; private set; } = "Combination Manager";

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

        /// <summary>
        /// thread for combining mmWave and GPS data to create LocationObjects
        /// </summary>
        public Thread mmWaveGPSThread { get; protected set; }

        /// <summary>
        /// bool for whether the LocationObjects thread is running
        /// </summary>
        public bool IsmmWaveGPSThreadProcessing
        {
            get
            {
                if (mmWaveGpsCombinationRunning.Wait(0))
                {
                    mmWaveGpsCombinationRunning.Release();
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// bool for whether the LocationObjects thread is processing
        /// </summary>
        bool ICombinationManager.IsmmWaveGPSThreadProcessing { get; }

        /// <summary>
        /// thread for combining LocationObject and RFID data to create TagLocationObjects
        /// </summary>
        public Thread ClusterObjRFIDThread { get; protected set; }

        /// <summary>
        /// bool for whether TagLocationObjects thread is running
        /// </summary>
        public bool IsClusterObjRFIDThreadProcessing
        {
            get
            {
                if (rfidObjectCombinationRunning.Wait(0))
                {
                    rfidObjectCombinationRunning.Release();
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// bool for whether the TagLocationObjects thread is processing
        /// </summary>
        bool ICombinationManager.IsClusterObjRFIDThreadProcessing { get; }

        /// <summary>
        /// output manager for saving data
        /// </summary>
        public OutputManager outputManager;

        private RfidManager rfidManager;

        //$$ Added to stop errors. RfidManager has no static memebers.
        public CombinationManager(RfidManager rfidManager)
        {
            this.rfidManager = rfidManager;
        }

        /// <summary>
        /// creates thread for combining mmWave and GPS data
        /// </summary>
        /// <param name="outputManager">output manager to use for data saving</param>
        /// <returns>bool of whether thread was created</returns>
        public bool CreatemmWaveGPSThread(OutputManager outputManager, mmWaveManager mmWaveManager_, GPSManager gpsManager)
        {
            try
            {
                this.outputManager = outputManager;
                // create new thread for processing the mmWave and GPS data
                mmWaveGPSThread = new Thread(() => this.CombinemmWaveGPSData(mmWaveManager_, gpsManager))
                {
                    IsBackground = true,
                    Priority = ThreadPriority.BelowNormal,
                    Name = "Combine mmWave + GPS Thread"
                };
                mmWaveGPSThread.Start();

                return true;
            }
            catch (Exception e)
            {
                // do some error handling
                LogMessage("Failed to start mmWave + GPS combination thread. " + e.GetType().FullName + ": " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// creates thread for combining LocationObjects and RFID data
        /// </summary>
        /// <returns>bool of whether thread was created</returns>
        public bool CreateClusterObjRFIDThread()
        {
            try
            {
                ClusterObjRFIDThread = new Thread(() => CombineRFIDObjects())
                {
                    IsBackground = true,
                    Priority = ThreadPriority.BelowNormal,
                    Name = "Combine Cluster Object + RFID Thread"
                };
                ClusterObjRFIDThread.Start();

                return true;
            }
            catch
            {
                // do some error handling
                return false;
            }
        }

        /// <summary>
        /// Cancels combining of data, does not wait for data to finish processing.
        /// </summary>
        public async Task Stop()
        {
            CancelmmWaveGPSCombination?.Cancel();
            await WaitmmWaveGpsCombinationFinish();
            cancelClusterRFIDCombination?.Cancel();
            await WaitRfidObjectCombinationFinish();
        }

        /// <summary>
        /// Cancels combining of data, waits for data to finish processing.
        /// </summary>
        public async Task StopAndProcess()
        {
            CancelmmWaveGPSCombination?.Cancel(); //$? Cancel, then wait for completion.
            await WaitmmWaveGpsCombinationFinish();
            await (clusterObjectChannel?.Reader.Completion ?? Task.CompletedTask);

            cancelClusterRFIDCombination?.Cancel();
            await WaitRfidObjectCombinationFinish();
        }

        #region mmWave + GPS Combining

        /// <summary>
        /// list of geolocated objects to send to the map
        /// </summary>
        public static List<ObjectLocation> GeolocationMapObjects = new List<ObjectLocation>();

        /// <summary>
        /// channel for storing LocationObjects
        /// </summary>
        private Channel<ObjectLocation> clusterObjectChannel = null;

        /// <summary>
        /// Controls how many mmWave + GPS combination loops are running.
        /// </summary>
        private readonly SemaphoreSlim mmWaveGpsCombinationRunning = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Completes when mmWave + GPS combination loop finishes, or after timeout.
        /// </summary>
        /// <param name="timeout">Number of milliseconds to wait before timing out.</param>
        /// <returns>True if combination finished. False if timed out.</returns>
        public async Task<bool> WaitmmWaveGpsCombinationFinish(int timeout = -1)
        {
            return await mmWaveGpsCombinationRunning.WaitAsync(timeout);
        }

        // make sure class is present for combining data
        CombineSensorData combineData = new CombineSensorData();
        // create temporary list for storing object locations
        List<ObjectLocation> temp = new List<ObjectLocation>();

        /// <summary>
        /// cancellation token to ensure safe exit of the mmWave GPS geolcation processing loop
        /// </summary>
        public static CancellationTokenSource CancelmmWaveGPSCombination { get; set; }


        /// <summary>
        /// combines mmWave and GPS data from channels, also saves found data to channels
        /// </summary>
        private void CombinemmWaveGPSData(mmWaveManager mmWaveManager_, GPSManager gpsManager)
        {
            // Only allow one loop to run at a time.
            if (!mmWaveGpsCombinationRunning.Wait(0))
            {
                // Only one loop can run at once.
                throw new InvalidOperationException("Attempted to start mmWave + GPS combination loop when it was already running.");
            }
            try
            {
                if (mmWaveManager_ == null || gpsManager == null)
                {
                    throw new NullReferenceException("At least one sensor manager was null.");
                }

                // lists to put read gps/mmwave data from channels to cluster
                List<ClusteredObject> mmWaveData = new List<ClusteredObject>();
                List<GpsData> GPSData = new List<GpsData>();
                List<GpsBearingData> GPSBearing = new List<GpsBearingData>();

                // init here to make new one between runs
                clusterObjectChannel = Channel.CreateUnbounded<ObjectLocation>(new UnboundedChannelOptions() { SingleWriter = true, SingleReader = false }); //$? Idk if single reader should be true.
                isClusterChannelReady.Set();

                // make a cancellation token to end this
                CancelmmWaveGPSCombination = new CancellationTokenSource();

                while (!CancelmmWaveGPSCombination.IsCancellationRequested)
                {
                    // this is a way to escape the processing loop
                    // If both channels are completed before start reading, then no data will be read.
                    if (mmWaveManager_.ClusterOut.Completion.IsCompleted && gpsManager.GPSDataOut.Completion.IsCompleted)
                    {
                        break;
                    }

                    try
                    {
                        mmWaveManager_.ClusterOut.WaitToReadAsync(CancelmmWaveGPSCombination.Token).AsTask().Wait();
                        gpsManager.GPSDataOut.WaitToReadAsync(CancelmmWaveGPSCombination.Token).AsTask().Wait();
                    }
                    catch { continue; } // Exception thrown if channel closed or token cancelled.
                    // get mmWave data from the channel to preapre for procesiing
                    while (mmWaveManager_.ClusterOut.TryRead(out ClusteredObject mmWaveitem))
                    {
                        mmWaveData.Add(mmWaveitem);
                    }
                    // get GPS data from the channel to prepare for processing
                    while (gpsManager.GPSDataOut.TryRead(out GpsData GPSitem))
                    {
                        GPSData.Add(GPSitem);
                    }

                    // Get bearing data.
                    for (int i = 0; i < GPSData.Count-5; i++)
                    {
                        double bearing = 0;
                        if (i < GPSData.Count - 5) // get angle for bearing
                        {
                            bearing = GpsBearing.GetInitialAngle(GPSData[i].Lat, GPSData[i].Long,
                                GPSData[i + 5].Lat, GPSData[i + 5].Long);
                        }


                        // get the bearing of a GPS point, add to list
                        GpsBearingData t = new GpsBearingData(GPSData[i].Lat, GPSData[i].Long, Convert.ToDateTime(GPSData[i].Time), bearing);
                        GPSBearing.Add(t);
                        if (i == GPSData.Count - 6) // remove used points
                        {
                            GPSData.RemoveRange(0, i);
                        }
                    }

                    // Combine mmWave objects and GPS data.
                    temp = combineData.CombinemmWaveGPSChannels(mmWaveData, GPSBearing);
                    Debug.WriteLine("mmWave In: " + temp.Count.ToString());
                    foreach (ObjectLocation item in temp)
                    {
                        if (item != null)
                        {
                            clusterObjectChannel.Writer.TryWrite(item);
                        }
                    }
                }
            }
            finally
            {
                try
                {
                    // complete channel if stopped and allow to run again if needed
                    clusterObjectChannel?.Writer.TryComplete();
                    isClusterChannelReady.Reset();
                }
                finally
                {
                    // release resources used for geolocation
                    mmWaveGpsCombinationRunning.Release();
                }
            }
        }
        #endregion

        #region Combining Cluster Object + RFID

        public static ConcurrentQueue<TagObjectLocation> TagLocationToMap = new ConcurrentQueue<TagObjectLocation>();

        // lists read from channels to coorelate both geolocated objects and RFID tagpeaks
        public List<ObjectLocation> tempObjects = new List<ObjectLocation>();
        public List<TagPeak> RFIDTemp = new List<TagPeak>();

        /// <summary>
        /// channel for storing TagLocationObjects
        /// </summary>
        static Channel<TagObjectLocation> RFIDClusterChannel;

        private CancellationTokenSource cancelClusterRFIDCombination;

        /// <summary>
        /// Whether mmWave + GPS object channel is ready.
        /// </summary>
        private readonly ManualResetEventSlim isClusterChannelReady = new ManualResetEventSlim(false);

        /// <summary>
        /// Controls how many RFID + geolocated object combination loops are running.
        /// </summary>
        private readonly SemaphoreSlim rfidObjectCombinationRunning = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Completes when RFID + geolocated object combination loop finishes, or after timeout.
        /// </summary>
        /// <param name="timeout">Number of milliseconds to wait before timing out.</param>
        /// <returns>True if combination finished. False if timed out.</returns>
        public async Task<bool> WaitRfidObjectCombinationFinish(int timeout = -1)
        {
            return await rfidObjectCombinationRunning.WaitAsync(timeout);
        }

        // for saving, probably temp
        List<ICsvWritable> RFIDLocationToSave = new List<ICsvWritable>();

        public event Action updateTagLocationRecieved;

        // THIS IS HACKY, CONSIDER CHANGING, ACTIVATES THE PLOTTING OF NONES AT END OF RUN
        public static bool isRunOver = false;

        /// <summary>
        /// combines LocationObject and RFID data and saves data if combined
        /// </summary>
        private void CombineRFIDObjects()
        {
            // Only allow one loop to run at a time.
            if (!rfidObjectCombinationRunning.Wait(0))
            {
                // Only one loop can run at once.
                throw new InvalidOperationException("Attempted to start RFID + geolocated object combination loop when it was already running.");
            }
            try
            {
                // create channel to write combined RFID data to
                RFIDClusterChannel = Channel.CreateUnbounded<TagObjectLocation>(new UnboundedChannelOptions() { SingleWriter = true, SingleReader = false });
                
                // Create new cancellation token for this loop.
                cancelClusterRFIDCombination = new CancellationTokenSource();

                // create channel to read tag peaks from
                ChannelReader<TagPeak> rfidTagDataChannel = rfidManager?.TagPeaksOut;
                if (rfidTagDataChannel == null)
                {
                    throw new NullReferenceException("TagPeak channel was null.");
                }

                // lsit of objects that have tags
                List<TagObjectLocation> ObjLocWithTags = new List<TagObjectLocation>();

                // hack, fix later
                //$$$while (!isClusterChannelReady) ;
                isClusterChannelReady.Wait(cancelClusterRFIDCombination.Token);

                while (!cancelClusterRFIDCombination.IsCancellationRequested)
                {
                    // break out if unable to continue
                    if (clusterObjectChannel == null || clusterObjectChannel.Reader.Completion.IsCompleted)
                    {
                        break;
                    }
                    // read cluster object if available, keep going if not
                    clusterObjectChannel.Reader.WaitToReadAsync().AsTask().Wait(250);

                    if (rfidTagDataChannel != null)
                    {
                        rfidTagDataChannel.WaitToReadAsync().AsTask().Wait(250);
                    }

                    int loaded = 0;
                    // get a geolocated object and add for processing
                    while (clusterObjectChannel.Reader.TryRead(out ObjectLocation item))
                    {
                        if (item != null)
                        {
                            tempObjects.Add(item);
                            loaded++;
                        }
                    }
                    // get an RFID tag and add for processing
                    while (rfidTagDataChannel.TryRead(out TagPeak RFIDItem))
                    {
                        if (RFIDItem != null)
                            RFIDTemp.Add(RFIDItem);
                    }
                    // find all geolocated objects that can have a tag, will have None if a tag cannot be found
                    List<TagObjectLocation> tempCombinedObj = combineData.CombineObjectRFIDNew(tempObjects, RFIDTemp,false);

                    if (tempCombinedObj != null && tempCombinedObj.Count > 0)
                    {
                        // write to channel for display on the UI for saving
                        foreach (TagObjectLocation item in tempCombinedObj)
                        {
                            RFIDClusterChannel.Writer.TryWrite(item);
                            ObjLocWithTags.Add(item);
                            //$$$RFIDLocationToSave.Add(item);

                            TagLocationToMap.Enqueue(item);
                        }
                        updateTagLocationRecieved?.Invoke();

                        outputManager.TrySaveData("TagLocationObj", tempCombinedObj);
                    }
                }

                //clear out remainder of RFID Channel
                while (!rfidTagDataChannel.Completion.IsCompleted)
                {
                    while (rfidTagDataChannel.TryRead(out TagPeak RFIDItem))
                        if (RFIDItem != null)
                            RFIDTemp.Add(RFIDItem);
                }

                //combine remaining tags with remaining detected objects
                List<TagObjectLocation> combineObj = null;
                if (RFIDTemp.Count > 0)
                    combineObj = combineData.CombineObjectRFIDNew(tempObjects, RFIDTemp, true);

                //save & plot detected objects
                if (combineObj != null && combineObj.Count > 0)
                {
                    foreach (TagObjectLocation item in combineObj)
                    {
                        RFIDClusterChannel.Writer.TryWrite(item);
                        ObjLocWithTags.Add(item);

                        TagLocationToMap.Enqueue(item);
                    }
                    updateTagLocationRecieved?.Invoke();

                    outputManager.TrySaveData("TagLocationObj", combineObj);
                }

                //remove redundant tagObj for those with tag detected
                for (int j = 0; j < ObjLocWithTags.Count; j++)
                {
                    for (int i = 0; i < tempObjects.Count; i++)
                    {
                        if (EDist(ObjLocWithTags[j], tempObjects[i]) < 0.00002)
                        {
                            tempObjects.RemoveAt(i--);
                        }
                    }
                }

                //remove redundant tagObj for objects with no tags detected
                for (int j = 0; j < tempObjects.Count; j++)
                {
                    for (int i = j + 1; i < tempObjects.Count; i++)
                    {
                        //remove repeat tag as it is too close
                        if (EDist(tempObjects[j], tempObjects[i]) < 0.00002)
                        {
                            tempObjects.RemoveAt(i--);
                        }
                    }
                }
                
                //remove No-Tag obj likely the result of Tag peak timing error
                for(int i = 1; i < ObjLocWithTags.Count-1; i++)
                {
                    //distance between two tag obj.
                    double tagDist = EDist(ObjLocWithTags[i - 1], ObjLocWithTags[i]);
                    if (tagDist < 0.000023) //2 cars won't be this close, likely another object was detected where the car should be
                    {
                        for (int j = 0; j < tempObjects.Count; j++) //loop through objects until one between this tag and next is found
                        {
                            double noneDist = EDist(ObjLocWithTags[i], tempObjects[j]);
                            if (noneDist < EDist(ObjLocWithTags[i], ObjLocWithTags[i+1]) && noneDist < 0.000045)
                            {
                                tempObjects.RemoveAt(j);
                                break;
                            }
                        }
                    }
                }

                List<TagObjectLocation> noneObjects = new List<TagObjectLocation>();
                // gets remaining non tag coorelated object with tagID NONE, invalid vehicles
                for (int i = tempObjects.Count - 1; i >= 0; i--)
                {
                    TagObjectLocation item = CombineFinalObjectDataNew(tempObjects[i]);
                    noneObjects.Add(item);
                    RFIDClusterChannel.Writer.WriteAsync(CombineFinalObjectDataNew(tempObjects[i]));
                    TagLocationToMap.Enqueue(item);
                    tempObjects.RemoveAt(i);
                }
                outputManager.TrySaveData("TagLocationObj", noneObjects);
            }
            finally
            {
                try
                {
                    RFIDClusterChannel?.Writer.TryComplete();

                    // plot the remaining none objects
                    Debug.WriteLine("ping");
                    isRunOver = true;
                    updateTagLocationRecieved?.Invoke();

                }
                finally
                {
                    rfidObjectCombinationRunning.Release();
                }
            }
        }

        /// <summary>
        /// Geolocated object without a tag
        /// </summary>
        /// <param name="obj">Object to add None to</param>
        /// <returns>Geolocated Object without a tag</returns>
        private TagObjectLocation CombineFinalObjectDataNew(ObjectLocation obj)
        {
            return new TagObjectLocation(obj.Lat, obj.Lng, obj.Time, obj.VehicleSide, obj.ClusterStrength, obj.ClusterSize, "None");
        }

        /// <summary>
        /// Find distance between two points
        /// </summary>
        /// <param name="found">Point that was found with tag</param>
        /// <param name="check">Point to compare it against</param>
        /// <returns>Distance between found and check</returns>
        private static double EDist(TagObjectLocation found, ObjectLocation check)
        {
            return Math.Sqrt((check.Lat - found.Lat) * (check.Lat - found.Lat) + (check.Lng - found.Lng) * (check.Lng - found.Lng));
        }
        /// <summary>
        /// Find distance between two points
        /// </summary>
        /// <param name="found">Point that was foundg</param>
        /// <param name="check">Point to compare it against</param>
        /// <returns>Distance between found and check</returns>
        private static double EDist(ObjectLocation found, ObjectLocation check)
        {
            return Math.Sqrt((check.Lat - found.Lat) * (check.Lat - found.Lat) + (check.Lng - found.Lng) * (check.Lng - found.Lng));
        }
        /// <summary>
        /// Find distance between two points
        /// </summary>
        /// <param name="found">Point that was found with tag</param>
        /// <param name="check">Point to compare it against with tag</param>
        /// <returns>Distance between found and check</returns>
        private static double EDist(TagObjectLocation found, TagObjectLocation check)
        {
            return Math.Sqrt((check.Lat - found.Lat) * (check.Lat - found.Lat) + (check.Lng - found.Lng) * (check.Lng - found.Lng));
        }

        #endregion
    }


}
