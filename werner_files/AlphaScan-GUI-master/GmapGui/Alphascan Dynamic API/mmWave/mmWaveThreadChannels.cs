using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using CES.AlphaScan.Base;

namespace CES.AlphaScan.mmWave
{
    /// <summary>
    /// Class to process mmWave packets into mmWave cluster objects.
    /// </summary>
    public class mmWaveThreadChannels : ILogMessage
    {
        /*
            These functions contain controls for the data channels started for the mmWave sensor, including controlling of number of processing threads that can occur.
            Controlling of these threads allow for multiple mmWave packets of data to be analyzed and outputted simultaneuously while also preventing too many resources
            from being used when processing the data.
        */

        private readonly static mmWaveDataProcessor dataProcessor = new mmWaveDataProcessor();

        #region Logging

        /// <summary>
        /// Name of the class.
        /// </summary>
        public string Name { get; protected set; } = "mmWaveThreadChannels";

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

        #region Settings
        /// <summary>
        /// Name of the file to save processed mmWave clusters to.
        /// </summary>
        private readonly string mmWaveClusterFileName = "mmWaveClusters";

        /// <summary>
        /// Input channel of mmWave packets to process.
        /// </summary>
        private ChannelReader<PacketData> packetChannelIn;

        /// <summary>
        /// Output channel of mmWave clustered objects.
        /// </summary>
        private Channel<ClusteredObject> clusterChannel;

        /// <summary>
        /// Sets the input and output channel to a new values. Fails if processing loop is running.
        /// </summary>
        /// <param name="packetChannel">New input channel.</param>
        /// <param name="clusterChannel_">New output channel.</param>
        /// <returns>Whether new channels were successfully set.</returns>
        public bool SetChannel(Channel<PacketData> packetChannel, Channel<ClusteredObject> clusterChannel_)
        {
            // Check that not starting up.
            if (startingUpSem.Wait(0))
            {
                try
                {
                    // Check that no threads still processing.
                    if (threadsProcessingCount.CountNotZero)
                    {
                        LogMessage("Failed to set channel: loops running.");
                        return false;
                    }
                    // Set channels
                    packetChannelIn = packetChannel.Reader;
                    this.clusterChannel = clusterChannel_;
                    return true;
                }
                finally
                {
                    startingUpSem.Release();
                }
            }
            else
            {
                LogMessage("Failed to set channel: loops running.");
                return false;
            }
        }

        #endregion

        #region Threading
        private readonly List<Thread> postProcessThreads = new List<Thread>();

        /// <summary>
        /// Returns in threadsafe way whether any threads are still processing.
        /// </summary>
        /// <returns>Boolean value of whether any threads are still processing data.</returns>
        public bool IsProcessing => threadsProcessingCount.CountNotZero;

        /// <summary>
        /// A threadsafe count of how many processing threads are currently running.
        /// </summary>
        private readonly SafeCounter threadsProcessingCount = new SafeCounter();

        //$$? Do we want an upper limit?
        /// <summary>
        /// Maximum number of processing threads allowed.
        /// </summary>
        private const int MaxNumThreads = 30;

        /// <summary>
        /// cancels background processing loop
        /// </summary>
        private CancellationTokenSource stopmmWaveBackgroundProcessing = new CancellationTokenSource();

        /// <summary>
        /// Semaphore to prevent multiple concurrent calls to <see cref="StartUp"/>.
        /// </summary>
        private readonly SemaphoreSlim startingUpSem = new SemaphoreSlim(1, 1);

        #endregion

        /// <summary>
        /// Starts a number of threads that process the incoming mmWave data and output it.
        /// </summary>
        /// <param name="n">Number of processing threads to start.</param>
        /// <param name="outputManager"></param>
        /// <param name="isSaving"></param>
        /// <param name="vehicleSide"></param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the number of threads is invalid.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the loops are already running or this 
        /// method has already been called.</exception>
        public void StartUp(int n, IOutputManager outputManager, bool isSaving, VehicleSide vehicleSide)
        {
            if (n <= 0 || n > MaxNumThreads)
                throw new ArgumentOutOfRangeException(nameof(n), n, "Invalid number of mmWave processing threads.");

            //Lock startup to prevent starting up twice.
            if (!startingUpSem.Wait(0))
                throw new InvalidOperationException("Failed to start up mmWave processing threads: Startup already running.");

            try
            {
                // Check all threads stopped
                if (threadsProcessingCount.CountNotZero)
                    throw new InvalidOperationException("Failed to start up mmWave processing threads: Threads are not finished processing.");

                postProcessThreads.Clear();

                // Create processing threads
                Thread ttt;
                for (int i = 0; i < n; i++)
                {
                    ttt = new Thread(() => StartNew(outputManager, isSaving, vehicleSide))
                    {
                        Name = "mmWaveBackgroundThread_" + i,
                        IsBackground = true,
                        Priority = ThreadPriority.Highest
                    };

                    postProcessThreads.Add(ttt);
                }

                stopmmWaveBackgroundProcessing = new CancellationTokenSource();

                // Start threads
                foreach (Thread t in postProcessThreads)
                {
                    t.Start();
                }
            }
            finally
            {
                startingUpSem.Release();
            }
        }

        /// <summary>
        /// Starts a loop that processes mmWave packet and outputs it. Also saves packet to file if set to.
        /// </summary>
        /// <param name="outputManager">OutputManager to handle saving to file.</param>
        /// <param name="isSaving">Whether to save raw packet to file.</param>
        /// <param name="vehicleSide"></param>
        private void StartNew(IOutputManager outputManager, bool isSaving, VehicleSide vehicleSide)
        {
            if (stopmmWaveBackgroundProcessing.IsCancellationRequested)
                return;

            threadsProcessingCount.Increment();
            try
            {
                PacketData item = null;
                List<ClusteredObject> output = null;
                while (!stopmmWaveBackgroundProcessing.IsCancellationRequested)
                {
                    //Read data packet from file
                    try
                    {
                        item = packetChannelIn.ReadAsync(stopmmWaveBackgroundProcessing.Token).AsTask().Result;
                    }
                    catch
                    {
                        if (packetChannelIn == null || packetChannelIn.Completion.IsCompleted)
                            return;
                    }

                    if (item == null)
                        continue;

                    //Save packet to file
                    if (isSaving)
                    {
                        Task.Run(() =>
                        {
                            try
                            {
                                if (!mmWaveSaveData.SaveData(item.FullPacket, item.Time, outputManager))
                                {
                                    LogMessage("Failed to save mmWave data packet.");
                                }
                            }
                            catch (Exception e)
                            {
                                LogMessage("Failed to save mmWave data packet. Exception: " + e.Message);
                            }
                        });
                    }

                    output = dataProcessor.FindmmWaveClusters(item, vehicleSide);
                    if (output != null)
                        OnClusterThreadFinish(output, outputManager);
                }
            }
            finally
            {
                threadsProcessingCount.Decrement();
            }
        }

        /// <summary>
        /// Requests for the processing loop to stop, if not already requested.
        /// </summary>
        public void Stop(int timeout = -1)
        {
            stopmmWaveBackgroundProcessing.Cancel();
            Task.Run(async () =>
            {
                await startingUpSem.WaitAsync();
                try
                {
                    if (await threadsProcessingCount.WaitForZero(timeout))
                        clusterChannel?.Writer.TryComplete();
                }
                finally
                {
                    startingUpSem.Release();
                }
            });

        }

        /// <summary>
        /// Requests for the processing loop to stop if not already requested. Waits for the loop to stop.
        /// </summary>
        /// <remarks>Also prevents starting new loops until already stopped.</remarks>
        /// <returns>Awaitable task that completes when the processing loop stops.</returns>
        public async Task<bool> StopAndWait(int timeout = -1)
        {
            // Prevent starting new threads while waiting for old threads to stop.
            await startingUpSem.WaitAsync().ConfigureAwait(true);
            try
            {
                // Cancel processing and wait for all threads to stop.
                stopmmWaveBackgroundProcessing.Cancel();
                if (!await threadsProcessingCount.WaitForZero(timeout).ConfigureAwait(true))
                {
                    return false;
                }
                clusterChannel?.Writer.TryComplete();
                return true;
            }
            finally
            {
                startingUpSem.Release();
            }
        }

        /// <summary>
        /// Handle outputting and saving to file of processed mmWave data.
        /// </summary>
        /// <param name="input">List of processed mmWave data.</param>
        /// <param name="outputManager">OutputManager that handles saving data to file.</param>
        private void OnClusterThreadFinish(IList<ClusteredObject> input, IOutputManager outputManager)
        {
            List<ICsvWritable> ClustersToSave = new List<ICsvWritable>();

            if (input != null && input.Count != 0)
            {
                foreach (ClusteredObject item in input)
                {
                    ClustersToSave.Add(item);

                    try
                    {
                        clusterChannel.Writer.WriteAsync(item);
                    }
                    catch (Exception e)
                    {
                        LogMessage("Failed to write mmWave cluster object to output channel." + " Exception: " + e.Message);
                    }
                    
                }
            }

            try
            {
                if (ClustersToSave.Count > 0 && outputManager.TrySaveData(mmWaveClusterFileName, ClustersToSave))
                {
                    ClustersToSave.Clear();
                }
            }
            catch (Exception e)
            {
                LogMessage("Failed to save mmWave cluster object. Exception: " + e.Message);
            }
        }

    }
}
