using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using CES.AlphaScan.Base;
using System.Threading.Channels;

namespace CES.AlphaScan.Rfid
{
    /// <summary>
    /// Sorts TagData objects by tag ID and antenna ID, collates the data points in a 
    /// collection, and exports the collection for further processing once a certain amount 
    /// of time has passed.
    /// </summary>
    public class TagCollator : ILogMessage
    {
        /*
        The TagCollator receive TagData objects through an input channel. It then sorts and 
        collates the data into TagCollections. When it stops receiving data for a 
        TagCollection, it writes it to the output channel and removes it from its internal 
        list.

        When the Start method is called, it attempts to start a new processing loop. This 
        loop is intended to be a long-running process on a thread (so make sure you don't 
        run it on the GUI thread). This loop synchronously performs the different functions 
        it needs to. This means that we do not need to implement synchronization code for 
        these calls as long as only one loop is running at a time.
        To ensure that only one loop runs at a time, we use the SemaphoreSlim loopRunning. 
        When a loop starts, it enters loopRunning, and when it finishes, it releases the 
        semaphore. Only one thread can enter loopRunning at a time, so this prevents 
        multiple loops as well as providing a way to check if the loop is running.
        The loop is stopped by cancelling the CancellationTokenSource stopProcessing. This 
        is cancelled in the stop methods. A new CancellationTokenSource is created with 
        calls to the start method.

        The loop reads TagData objects from the input channel, adds the tag data to a 
        collection, compares the time stamp to check if any collections need to expire, and 
        exports any expired tag collections.

        When adding a TagData object, it first finds a TagCollection based on the tag ID. 
        Then it adds it to an AntennaCollection in the TagCollection based on the ID of the 
        antenna that read the tag. the TagData is just added to a data list in the 
        AntennaCollection.

        The collator exports TagCollections after it has collected all the data about a 
        particular tag. To do this, it compares the time stamp of the most recent tag added 
        to a collection to the current time. If it has been longer than the time specified 
        in the PersistTime setting, then that TagCollection is said to have expired. These 
        expired collections will then be exported and removed from the internal list.
        The collator compares time stamps every time it reads a new TagData object. It uses 
        the time stamp in the new object as the current time stamp. Because this only exports 
        lists when reading new data, the last tag read in a run will not be exported. To 
        combat this, we export all collections when the processing loop is stopped.

        When the loop finishes, it releases loopRunning and completes the output channel, so 
        you will need to call SetChannel each time you stop and start the loop.
        //*/

        #region Logging
        /// <summary>
        /// Name of the module.
        /// </summary>
        public string Name { get; private set; } = "TagCollator";

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

        #region Settings
        //$$? Does this need to be public?
        /// <summary>
        /// Amount of time in ms that it takes a data set to expire before exported for further processing.
        /// </summary>
        public int PersistTime { get { return _persistTime; } private set { _persistTime = value; persistTimeTicks = value * 10000; } }
        private int _persistTime;

        /// <summary>
        /// Sets the settings for the tag collator.
        /// </summary>
        /// <param name="newSettings">New settings to apply to the collator.</param>
        /// <exception cref="ArgumentNullException">Thrown if no settings are passed as argument.</exception>
        /// <exception cref="InvalidOperationException">Thrown if tries to set settings while still collating.</exception>
        /// <exception cref="FormatException">Thrown if settings values fail to parse.</exception>
        public void SetSettings(IDictionary<string, string> newSettings)
        {
            if (newSettings == null || newSettings.Count < 1)
            {
                //$$No settings received
                throw new ArgumentNullException(nameof(newSettings), "No settings were sent to " + nameof(TagCollator));
            }

            if (loopRunning.Wait(0))
            {
                try
                {
                    var settings = new Dictionary<string, string>(newSettings);

                    if (settings.ContainsKey("PersistTime") && !string.IsNullOrWhiteSpace(settings["PersistTime"]))
                    {
                        PersistTime = int.Parse(settings["PersistTime"]);
                    }
                }
                finally
                {
                    loopRunning.Release();
                }
            }
            else
            {
                //$$fail when system running
                throw new InvalidOperationException("Cannot change settings while system is running.");
            }
        }

        /// <summary>
        /// Sets the input and output channel to a new value. Fails if processing loop is running.
        /// </summary>
        /// <param name="tagDataChannel">New input channel.</param>
        /// <param name="tagCollectionChannel">New output channel.</param>
        /// <returns>Whether new channels were successfully set.</returns>
        public bool SetChannel(ChannelReader<TagData> tagDataChannel, Channel<TagCollection> tagCollectionChannel)
        {
            if (loopRunning.Wait(0))
            {
                try
                {
                    try
                    {
                        tagDataIn = tagDataChannel;
                        tagCollectionOut = tagCollectionChannel;
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
                finally
                {
                    loopRunning.Release();
                }
            }
            else
            {
                LogMessage("Failed to set channels: loop running.");
                return false;
            }

        }
        #endregion

        #region Time Comparison
        /// <summary>
        /// Persist time converted to ticks. For efficiency.
        /// </summary>
        private long persistTimeTicks;

        /// <summary>
        /// Compares the most recent time in each collection to the given time. Expires 
        /// collections if the difference is larger than the persist time.
        /// </summary>
        /// <param name="time">The time to compare against each collection. In ticks.</param>
        private void CompareTime(long time)
        {
            foreach (string id in TagCollections.Keys)
            {
                if (time - TagCollections[id].LastSeenTime > persistTimeTicks)
                {
                    expiredIds.Enqueue(id);
                }
            }
        }

        #endregion

        /// <summary>
        /// Whether the tag collating loop is running.
        /// </summary>
        public bool IsProcessing { get; private set; }

        /// <summary>
        /// Handles the cancelling of the processing loop. Used in <see cref="Stop"/> and <see cref="StopAndWait"/>
        /// </summary>
        private CancellationTokenSource stopProcessing = new CancellationTokenSource();

        /// <summary>
        /// Prevents multiple instances of the loop started in <see cref="Start"/> from running simultaneously.
        /// </summary>
        private readonly SemaphoreSlim loopRunning = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Channel of TagData objects to be collated and processed.
        /// </summary>
        private ChannelReader<TagData> tagDataIn;

        /// <summary>
        /// Channel of TagCollection objects being exported to further processing.
        /// </summary>
        private Channel<TagCollection> tagCollectionOut;

        /// <summary>
        /// Constructs new TagCollator.
        /// </summary>
        /// <param name="inputChannel">Channel of tag data to collate.</param>
        /// <param name="outputChannel">Channel of tagcollections to output.</param>
        /// <param name="persistTime">Persist time of tag collections.</param>
        public TagCollator(ChannelReader<TagData> inputChannel, Channel<TagCollection> outputChannel, int persistTime = 5000)
        {
            tagDataIn = inputChannel;
            tagCollectionOut = outputChannel;
            PersistTime = persistTime;
        }

        /// <summary>
        /// List of collections of data points separated by tag ID. One collection for each tag ID.
        /// </summary>
        public Dictionary<string, TagCollection> TagCollections { get; } = new Dictionary<string, TagCollection>();

        /// <summary>
        /// Queue of IDs of TagCollections that have expired and need to be processed.
        /// </summary>
        private readonly Queue<string> expiredIds = new Queue<string>();

        /// <summary>
        /// Starts loop that collates tag data and outputs tag collections.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if attempting to start loop while loop is already running.</exception>
        public void Start()
        {
            // Only allow one loop to run at a time.
            if (!loopRunning.Wait(0))
            {
                throw new InvalidOperationException("Attempted to start tag collator loop when it was already running.");
            }
            try
            {
                if (tagDataIn == null || tagDataIn.Completion.IsCompleted)
                {
                    // No input channel.
                    throw new InvalidOperationException("Tag data input channel is null or completed.");
                }

                if (tagCollectionOut == null || tagCollectionOut.Reader.Completion.IsCompleted)
                {
                    // No output channel.
                    throw new InvalidOperationException("Tag collection output channel is null or completed.");
                }

                stopProcessing = new CancellationTokenSource();
                IsProcessing = true;

                TagCollections.Clear();
                expiredIds.Clear();

                // Processing Loop:
                // - Read TagData from channel
                // - Add tag data to collection
                // - Expire old collections
                // - Export expired collections
                while (!stopProcessing.IsCancellationRequested)
                {
                    //Read new tagData from channel
                    if (TagCollections.Count < 1)
                    {
                        try
                        {
                            tagDataIn.WaitToReadAsync(stopProcessing.Token).AsTask().Wait(); //Synchronous wait for this async method
                        }
                        catch (Exception) { continue; }
                    }

                    //Add tagdata to tag collections
                    if (tagDataIn.TryRead(out TagData newData) && newData != null)
                    {
                        Add(newData);

                        //Compare latest time against previous time for all IDs
                        CompareTime(newData.LastSeenTime.Ticks);
                    }

                    //Write expired tag collections on output channel
                    while (expiredIds.Count() > 0)
                    {
                        ExportCollection(expiredIds.Dequeue());
                    }
                }

                //Expire all remaing collections when the loop is stopped.
                foreach (string id in TagCollections.Keys)
                {
                    expiredIds.Enqueue(id);
                }

                //Write expired tag collections on output channel
                while (expiredIds.Count() > 0)
                {
                    ExportCollection(expiredIds.Dequeue());
                }

                TagCollections.Clear();
                expiredIds.Clear();
            }
            finally
            {
                IsProcessing = false;
                tagCollectionOut?.Writer.TryComplete();

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
            stopProcessing.Cancel();
            await loopRunning.WaitAsync();
            loopRunning.Release();
        }

        /// <summary>
        /// Requests for the processing loop to stop, if not already requested.
        /// </summary>
        public void Stop()
        {
            stopProcessing.Cancel();
        }

        /// <summary>
        /// Tries to add tag data into a tag collection matching the tag ID. If none is found,
        /// creates a new tag collection.
        /// </summary>
        /// <param name="tagData">Data point to be added to a collection.</param>
        private void Add(TagData tagData)
        {
            if (tagData != null)
            {
                // ID of the tag that was read.
                string tagId = tagData.TagId;

                // If no collection exists, add new one
                if (!TagCollections.ContainsKey(tagId))
                {
                    TagCollections.Add(tagId, new TagCollection(tagId));
                }

                // Add tagData to collection
                TagCollections[tagId].Add(tagData);
            }
        }

        /// <summary>
        /// Exports collection tag data for further analysis or saving.
        /// </summary>
        /// <param name="id">ID of tag data collection to export.</param>
        private void ExportCollection(string id)
        {
            var tagCollection = TagCollections[id];
            tagCollectionOut.Writer.WriteAsync(tagCollection);
            TagCollections.Remove(id);
        }
    }

    /// <summary>
    /// Collection of the tag data points that match a specific tag ID.
    /// </summary>
    public class TagCollection
    {
        /// <summary>
        /// ID of the tag whose data is being saved in the collection.
        /// </summary>
        public string TagId { get; }

        /// <summary>
        /// Creates new collection of data points for the given tag ID.
        /// </summary>
        /// <param name="tagId">ID of the tag whose data is being saved.</param>
        public TagCollection(string tagId)
        {
            TagId = tagId;
        }

        /// <summary>
        /// Creates new collection of data points for the given tag ID and antenna collection list.
        /// </summary>
        /// <param name="tagId">ID of the tag whose data is being saved.</param>
        /// <param name="antennaCollections">List of antenna collections to add to the antenna collection list.</param>
        public TagCollection(string tagId, IEnumerable<AntennaCollection> antennaCollections)
        {
            TagId = tagId;
            AntennaCollections = antennaCollections.ToList();
        }

        /// <summary>
        /// Collection of the data points for a specific tag ID collected by a single antenna.
        /// </summary>
        public class AntennaCollection
        {
            /// <summary>
            /// ID of the antenna that received this data. Ex: 0, 1, 2, or 3.
            /// </summary>
            public int AntennaId { get; }

            /// <summary>
            /// Creates a new collection of data points with the given antenna ID.
            /// </summary>
            /// <param name="antennaId">ID of the antenna that read these data points.</param>
            public AntennaCollection(int antennaId)
            {
                AntennaId = antennaId;
            }

            /// <summary>
            /// Creates a new collection of data points with the given antenna ID and data list.
            /// </summary>
            /// <param name="antennaId">ID of the antenna that read these data points.</param>
            /// <param name="dataList">List of data points to be included in this collection.</param>
            public AntennaCollection(int antennaId, IEnumerable<Tuple<long, double>> dataList)
            {
                AntennaId = antennaId;
                DataList = dataList.ToList();
            }

            /// <summary>
            /// Collection of the data points, stored as a List of Tuples. Ex: { (last seen time in ms as long), (strength of RSSI as double)}
            /// </summary>
            public List<Tuple<long, double>> DataList { get; } = new List<Tuple<long, double>>();

            /// <summary>
            /// Adds a new data point to the collection from individual values.
            /// </summary>
            /// <param name="newTime">Time of last measurement of tag in ticks.</param>
            /// <param name="newRssi">RSSI strength of tag.</param>
            public void Add(long newTime, double newRssi)
            {
                DataList.Add(new Tuple<long, double>(newTime, newRssi));
            }

            /// <summary>
            /// Adds a new data point to the collection from a TagData object.
            /// </summary>
            /// <param name="newData">Tag data to add to collection.</param>
            public void Add(TagData newData)
            {
                Add(newData.LastSeenTime.Ticks, newData.Rssi);
            }

            /// <summary>
            /// Gets the number of elements contained in the AntennaCollection.
            /// </summary>
            public int Count { get { return DataList.Count; } }

            /// <summary>
            /// Returns the time of the latest reading to the tag with the chosen antenna.
            /// </summary>
            public long LastSeenTime
            {
                get
                {
                    return DataList.Max(d => d.Item1);
                }
            }
        }

        /// <summary>
        /// List of AntennaCollection objects. Contains one for each antenna that read the tag in the given cycle.
        /// </summary>
        public List<AntennaCollection> AntennaCollections { get; } = new List<AntennaCollection>();

        //$? Maybe add some strength thresholding to add a new data point. Should reduce noise from unwanted antennas.
        /// <summary>
        /// Tries to add tag data into an antenna collection matching the AntennaId. If none is found,
        /// creates a new antenna collection.
        /// </summary>
        /// <param name="tagData">Data point to be added to a collection.</param>
        public void Add(TagData tagData)
        {
            // ID of the antenna that measured the data point.
            int antId = tagData.RxAntenna;

            // Try to add data point to an existing collection.
            foreach (AntennaCollection ac in AntennaCollections)
            {
                if (ac.AntennaId == antId)
                {
                    ac.Add(tagData);
                    return;
                }
            }

            // No collection found matching antId, add new collection.
            var antColl = new AntennaCollection(antId);
            antColl.Add(tagData);
            AntennaCollections.Add(antColl);
        }

        /// <summary>
        /// Gets the total number of elements in the tag collection.
        /// </summary>
        /// <returns></returns>
        public int Count { get { return AntennaCollections.Select(x => x.Count).Sum(); } }

        /// <summary>
        /// Time in ms of the last reading of the tag by any antenna.
        /// </summary>
        public long LastSeenTime
        {
            get
            {
                return AntennaCollections.Max(tagsByAntenna => tagsByAntenna.LastSeenTime);
            }
        }

        /// <summary>
        /// Determines whether the data has expired. This is based on whether the time since 
        /// the last tag reading is greater than the persist time.
        /// </summary>
        /// <param name="currentTime">The current time (UTC).</param>
        /// <param name="persistTime">Time in ms that data is valid for.</param>
        /// <returns></returns>
        public bool IsOld(DateTime currentTime, long persistTime = 200)
        {
            return (currentTime.Ticks - LastSeenTime) / 10000 > persistTime;
        }
    }
}
