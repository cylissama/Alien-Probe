using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using CES.AlphaScan.Base;
using System.Collections.Specialized;
using System.ComponentModel; // for INotifyPropertyChanged interface
using System.Runtime.CompilerServices; // for [CallerMemberName] in NotifyPropertyChanged

namespace CES.AlphaScan.Rfid
{
    /// <summary>
    /// Interface representing an RFID manager. An object that communicates with, controls, 
    /// and processes data read by an RFID reader.
    /// </summary>
    public interface IRfidManager
    {
        /// <summary>
        /// Whether or not the RFID reader is currently connected.
        /// </summary>
        bool IsConnected { get; }
        /// <summary>
        /// Whether or not the RFID reader is currently running.
        /// </summary>
        bool IsRunning { get; }
        /// <summary>
        /// Whether or not the RFID manager is currently processing data.
        /// </summary>
        bool IsProcessing { get; }

        /// <summary>
        /// Starts RFID manager and sub classes.
        /// </summary>
        /// <returns>Whether RFID manager was successfully started.</returns>
        bool Start();
        /// <summary>
        /// Stops reading and processing data. Not guaranteed to process all collected data.
        /// </summary>
        /// <returns>Awaitable task of whether aborted successfully.</returns>
        Task<bool> Abort();
        /// <summary>
        /// Stops reading, but continues to process collected data.
        /// </summary>
        /// <returns>Awaitable task of whether stopped and finished processing successfully.</returns>
        Task<bool> StopAndProcess();
        
        /// <summary>
        /// Sends settings to the RFID manager and initializes the object.
        /// </summary>
        /// <param name="_rfidSettings">Settings pertaining to the RFID manager.</param>
        /// <param name="_globalSettings">Settings pertaining to the system as a whole.</param>
        /// <param name="_outputManager">Reference to the <see cref="CES.AlphaScan.Base.IOutputManager"/>
        /// object that handles data saving for the system.</param>
        /// <returns>Whether manager was successfully initialized with the given settings.</returns>
        bool SetSensorSettings(IDictionary<string, object> _rfidSettings, IDictionary<string, object> _globalSettings, CES.AlphaScan.Base.IOutputManager _outputManager);
        /// <summary>
        /// Sends a string message to the RFID reader.
        /// </summary>
        /// <param name="message">Message to send to the reader.</param>
        void SendToReader(string message);
        /// <summary>
        /// Sets settings on RFID reader to values specified in RFID settings file.
        /// </summary>
        /// <returns>Whether successfully reset sensor settings.</returns>
        bool ResetReaderSettings();
        /// <summary>
        /// Reboots RFID reader.
        /// </summary>
        /// <returns>Awaitable task that completes when reader has rebooted.</returns>
        Task RebootReader();

    }

    /// <summary>
    /// An object that communicates with, controls, and processes data read by an RFID reader.
    /// </summary>
    public class RfidManager : ILogMessage, INotifyPropertyChanged, IRfidManager
    {
        /*
        The RfidManager contains and controls all the other modules needed for RFID. It 
        communicates with the RFID reader, processes the data it returns, and outputs 
        TagPeak data for use in later processing steps.
        It also detects blacklisted RFID tags and alerts the user if they are detected.

        When the start method is called, it starts each module for processing data. It 
        creates new Threads for modules that need them. Next it attempts to start the RFID 
        reader. If it fails, it stops all the processing loops.

        When the stop method is called, it stops the RFID reader, then it stops each 
        processing loop in order. It waits for the previous loop to finish before stopping 
        the next in order to prevent data loss. If the abort method is called, it does not 
        wait, but just stops them all at once.

        Uses the CES.AlphaScan.Base.ChannelSplitter class to split the TagData channel so a 
        copy of the each TagData object is sent to both the TagCollator and the 
        BlacklistDetector. Because it does this, the TagCollator will process tags 
        regardless of them being blacklisted.
        //*/

        private RfidReaderModule readerModule;
        private RfidInputParser inputParser;
        private TagCollator tagCollator;
        private Thread collatorThread;
        private TagPeakDetector peakDetector;
        private Thread peakDetectorThread;
        private BlacklistDetector blacklistDetector;
        private Thread blacklistDetectorThread;
        private CancellationTokenSource stopParsedTags = null;
        
        /// <summary>
        /// Event raised when a blacklisted tag is detected. May throw exception if detector is not initialised.
        /// </summary>
        public event EventHandler<TagData> BlacklistTagDetected
        {
            add { blacklistDetector.DetectedBlacklistTag += value; }
            remove { blacklistDetector.DetectedBlacklistTag -= value; }
        }


        private CES.AlphaScan.Base.IOutputManager outputManager = null;

        #region Logging

        public string Name { get; private set; } = "RFIDManager";

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
        /// Whether RFID tag peak data should be generated.
        /// </summary>
        public bool ProcessTagPeaks { get; private set; } = false;

        /// <summary>
        /// Whether blacklisted RFID tags should be detected.
        /// </summary>
        public bool DetectBlacklistTags { get; private set; } = false;

        /// <summary>
        /// Whether RFID tag peak data should be saved to file. Meaningless if not generated.
        /// </summary>
        public bool SaveTagPeaks { get; private set; } = false;

        /// <summary>
        /// Whether raw RFID tag data should be saved to file.
        /// </summary>
        public bool SaveTagData { get; private set; } = false;


        /// <summary>
        /// Layout of the antennas for this RFID manager.
        /// </summary>
        public AntennaArrangement AntennaLayout
        {
            get { return new AntennaArrangement(_antennaLayout); }
            private set { _antennaLayout = new AntennaArrangement(value); }
        }
        private AntennaArrangement _antennaLayout = null;

        private TagRequirements tagReq = null;

        //$$ Is this needed?
        /// <summary>
        /// Whether settings have been sent from the settings/config file.
        /// </summary>
        private bool settingsReceived = false;

        /// <summary>
        /// Whether manager has been setup yet.
        /// </summary>
        private readonly ManualResetEventSlim isSetUp = new ManualResetEventSlim(false);

        private IDictionary<string, object> rfidSettings = null;

        private IDictionary<string, object> globalSettings = null;

        /// <summary>
        /// Sends settings to the RFID manager and initializes the object.
        /// </summary>
        /// <param name="_rfidSettings">Settings pertaining to the RFID manager.</param>
        /// <param name="_globalSettings">Settings pertaining to the system as a whole.</param>
        /// <param name="_outputManager">Reference to the <see cref="CES.AlphaScan.Base.IOutputManager"/>
        /// object that handles data saving for the system.</param>
        /// <returns>Whether manager was successfully initialized with the given settings.</returns>
        public bool SetSensorSettings(IDictionary<string, object> _rfidSettings, IDictionary<string, object> _globalSettings, CES.AlphaScan.Base.IOutputManager _outputManager)
        {
            //$$Could improve this by having start, stop, etc. check out a semaphore
            // Check if running.
            if (IsRunning || IsConnected || IsProcessing)
            {
                return false;
            }

            if (isSetUp.IsSet || settingsReceived)
            {
                LogMessage("Replacing RfidManager settings.");

                isSetUp.Reset();
                //Check that still not running.
                if (IsRunning || IsConnected || IsProcessing)
                {
                    isSetUp.Set();
                    return false;
                }
            }

            // Check arguments were passed.
            if (_rfidSettings == null || _rfidSettings.Count < 1)
            {
                LogMessage("No " + nameof(_rfidSettings) + " passed to RFID manager.");
                return false;
            }
            if (_globalSettings == null)
            {
                LogMessage("No " + nameof(_globalSettings) + " passed to RFID manager.");
                return false;
            }
            if (_outputManager == null)
            {
                LogMessage("No " + nameof(_outputManager) + " passed to RFID manager.");
                return false;
            }

            // Copy settings
            rfidSettings = new Dictionary<string, object>(_rfidSettings);
            globalSettings = new Dictionary<string, object>(_globalSettings);
            outputManager = _outputManager;

            settingsReceived = true;

            try
            {
                ProcessTagPeaks = bool.Parse((string)rfidSettings["ProcessTagPeaks"]);
                if (ProcessTagPeaks) SaveTagPeaks = bool.Parse((string)rfidSettings["SaveTagPeaks"]);
                else SaveTagPeaks = false;
                SaveTagData = bool.Parse((string)rfidSettings["SaveTagData"]);
                DetectBlacklistTags = bool.Parse((string)rfidSettings["DetectBlacklistTags"]);
            }
            catch (Exception e)
            {
                settingsReceived = false;
                LogMessage("Failed to set RfidManager settings: " + e.Message);
                return false;
            }

            // Read antenna arrangement from settings.
            try
            {
                IList<int> rightList = new List<int>();
                var right = (_rfidSettings["AntennaArrangement.Right"].ToString()).Split(' ');
                foreach (string s in right)
                {
                    rightList.Add(int.Parse(s));
                }

                IList<int> leftList = new List<int>();
                var left = (_rfidSettings["AntennaArrangement.Left"].ToString()).Split(' ');
                foreach (string s in left)
                {
                    leftList.Add(int.Parse(s));
                }

                AntennaLayout = new AntennaArrangement(rightList, leftList);
            }
            catch (Exception e)
            {
                LogMessage("Failed to read antenna layout: " + e.Message);
                return false;
            }

            // Read tag id requirements from settings
            try
            {
                tagReq = new TagRequirements(FindByKey(_rfidSettings, "TagReq"));
            }
            catch (Exception e)
            {
                LogMessage("Failed to read tag ID requirements: " + e.Message);
                return false;
            }

            // Set up manager using given settings.
            try
            {
                SetUp();
            }
            catch (Exception e)
            {
                LogMessage("Failed to set up RfidManager: " + e.Message);
                return false;
            }


            // Find and send settings to readerModule.
            try
            {
                var readerSettings = FindByKey(_rfidSettings, "Reader");
                readerModule.SetSettings(readerSettings);
            }
            catch (Exception e)
            {
                LogMessage("Failed to set RFID reader module settings: " + e.Message);
                return false;
            }

            if (ProcessTagPeaks)
            {
                // Find and send settings to tagCollator.
                try
                {
                    var collatorSettings = FindByKey(_rfidSettings, "Collator");
                    tagCollator.SetSettings(collatorSettings);
                }
                catch (Exception e)
                {
                    LogMessage("Failed to set RFID tag collator settings: " + e.Message);
                    return false;
                }

                // Find and send settings to peakDetector.
                try
                {
                    var detectorSettings = FindByKey(_rfidSettings, "PeakDetector");
                    peakDetector.SetSettings(detectorSettings, AntennaLayout);
                }
                catch (Exception e)
                {
                    LogMessage("Failed to set RFID tag peak detector settings: " + e.Message);
                    return false;
                }
            }

            if (DetectBlacklistTags)
            {
                // Find and send settings to peakDetector.
                try
                {
                    var blacklistSettings = FindByKey(_rfidSettings, "BlacklistDetector");
                    blacklistDetector.SetSettings(blacklistSettings);
                }
                catch (Exception e)
                {
                    LogMessage("Failed to set RFID blacklisted tag detector settings: " + e.Message);
                    return false;
                }
            }

            settingsReceived = true;
            isSetUp.Set();
            return true;
        }

        /// <summary>
        /// Function for finding settings with a specific key preceding the setting name. 
        /// Ex: For list {a=1; b=2; A.c=3; B.d=4; A.a=5}, with key "A", will filter for {c=3; a=5}.
        /// </summary>
        /// <param name="settings">Settings to search through.</param>
        /// <param name="key">Key preceding setting name. Ex: "Reader" in "Reader.Setting".</param>
        /// <returns>Collection of settings with matching key preceding name. Removes preceding key from names.</returns>
        private IDictionary<string, string> FindByKey(IDictionary<string, object> settings, string key, char delimiter = '.')
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("No key given for filtering settings.", nameof(key));
            }

            if (settings == null) return null;

            IDictionary<string, string> filteredSettings = new Dictionary<string, string>();

            foreach (KeyValuePair<string, object> kvp in settings)
            {
                int idx = kvp.Key.IndexOf(delimiter);
                if (idx < 0) continue;
                if (kvp.Key.Substring(0, idx) == key)
                {
                    filteredSettings.Add(kvp.Key.Substring(idx + 1), (string)kvp.Value);
                }
            }
            return filteredSettings;
        }

        #endregion

        //$$ Sort this out
        #region Property Notification
        //$? Maybe have events passed up through subclasses.

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

        private bool _isReaderConnected = false;
        /// <summary>
        /// Whether or not the RFID reader is currently connected.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return _isReaderConnected;
            }
            private set
            {
                if (_isReaderConnected != value)
                {
                    _isReaderConnected = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _isReaderRunning = false;
        /// <summary>
        /// Whether or not the RFID reader is currently running.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return _isReaderRunning;
            }
            private set
            {
                if (_isReaderRunning != value)
                {
                    _isReaderRunning = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _isProcessing = false;
        /// <summary>
        /// Whether the RFID system is currently processing data.
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

        /// <summary>
        /// Propogates property changes from reader module to manager.
        /// </summary>
        /// <remarks>
        /// Currently only changes in <see cref="RfidReaderModule.IsConnected"/> and 
        /// <see cref="RfidReaderModule.IsRunning"/> are propogated.
        /// </remarks>
        /// <param name="sender">Object that raised the event.</param>
        /// <param name="e">Data describing property change.</param>
        private void Reader_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string propName = e.PropertyName;

            switch (propName)
            {
                case nameof(readerModule.IsConnected):
                    IsConnected = readerModule.IsConnected;
                    break;
                case nameof(readerModule.IsRunning):
                    IsRunning = readerModule.IsRunning;
                    break;
                default:
                    break;
            }
        }
        #endregion

        private Channel<TagData> parsedTagChannel = null;
        public ChannelReader<TagData> ParsedTagsOut { get { return parsedTagChannel?.Reader; } }

        private Channel<TagCollection> tagCollectionChannel = null;
        public ChannelReader<TagCollection> TagCollectionsOut { get { return tagCollectionChannel?.Reader; } }

        private Channel<TagPeak> tagPeakChannel = null;
        public ChannelReader<TagPeak> TagPeaksOut { get { return tagPeakChannel?.Reader; } }

        private ChannelReader<TagData> collatorInputChannel = null;
        private ChannelReader<TagData> blacklistInputChannel = null;

        /// <summary>
        /// Initializes RFID manager using previously received settings.
        /// </summary>
        private void SetUp()
        {
            //$$ Make sure settings received

            // Set up input parser
            parsedTagChannel = Channel.CreateUnbounded<TagData>(new UnboundedChannelOptions() { SingleWriter = false, SingleReader = true });
            if (SaveTagData)
            {
                if (rfidSettings.ContainsKey("TagDataFileName"))
                    inputParser = new RfidInputParser(parsedTagChannel, outputManager, (string)rfidSettings["TagDataFileName"], tagReq);
                else
                    inputParser = new RfidInputParser(parsedTagChannel, outputManager, _tagReq: tagReq);
            }
            else
                inputParser = new RfidInputParser(parsedTagChannel, tagReq);
            inputParser.MessageLogged += LogMessage;

            // Split parsed tag data channel if needed twice.
            if (DetectBlacklistTags && ProcessTagPeaks)
            {
                stopParsedTags = new CancellationTokenSource();
                var chList = ChannelSplitter.CopyChannel(parsedTagChannel.Reader, 2, stopParsedTags.Token);
                collatorInputChannel = chList[0];
                blacklistInputChannel = chList[1];
            }
            else
            {
                stopParsedTags = null;
                if (DetectBlacklistTags)
                {
                    blacklistInputChannel = parsedTagChannel.Reader;
                }
                if (ProcessTagPeaks)
                {
                    collatorInputChannel = parsedTagChannel.Reader;
                }
            }
            

            if (ProcessTagPeaks)
            {
                tagCollectionChannel = Channel.CreateUnbounded<TagCollection>(new UnboundedChannelOptions() { SingleWriter = true, SingleReader = true });
                // Set up collator
                tagCollator = new TagCollator(collatorInputChannel, tagCollectionChannel);
                tagCollator.MessageLogged += LogMessage;

                tagPeakChannel = Channel.CreateUnbounded<TagPeak>(new UnboundedChannelOptions() { SingleWriter = true, SingleReader = false });
                // Set up peak detector
                if (SaveTagPeaks)
                {
                    if (rfidSettings.ContainsKey("TagPeaksFileName"))
                        peakDetector = new TagPeakDetector(tagCollectionChannel.Reader, tagPeakChannel, outputManager, (string)rfidSettings["TagPeaksFileName"]);
                    else
                        peakDetector = new TagPeakDetector(tagCollectionChannel.Reader, tagPeakChannel, outputManager);
                }
                else
                    peakDetector = new TagPeakDetector(tagCollectionChannel.Reader, tagPeakChannel);
                peakDetector.MessageLogged += LogMessage;
            }

            if (DetectBlacklistTags)
            {
                blacklistDetector = new BlacklistDetector(blacklistInputChannel);
                blacklistDetector.MessageLogged += LogMessage;
            }


            {
                string readerIP = (string)rfidSettings["ReaderIP"];
                int port = int.Parse((string)rfidSettings["Port"]);
                string serverIP = (string)rfidSettings["ServerIP"];

                //$$? Change from parser reference to parseTag delegate reference.
                readerModule = new RfidReaderModule(inputParser, readerIP, port, serverIP, (string)rfidSettings["Username"], (string)rfidSettings["Password"]);
                readerModule.MessageLogged += LogMessage;
                readerModule.PropertyChanged += Reader_PropertyChanged;
            }
        }

        /// <summary>
        /// Resets channels before starting the manager.
        /// </summary>
        /// <returns>Whether successfully reset channels.</returns>
        private bool ResetChannels()
        {
            if (!isSetUp.IsSet || IsProcessing)
                return false;

            // Reset input parser channel.
            parsedTagChannel = Channel.CreateUnbounded<TagData>(new UnboundedChannelOptions() { SingleWriter = false, SingleReader = true });
            if (!inputParser.SetChannel(parsedTagChannel))
            {
                // Stop and retry if failed.
                inputParser.StopAndWait().Wait();
                if (!inputParser.SetChannel(parsedTagChannel))
                    return false;
            }

            // Split parsed tag data channel if needed twice.
            if (DetectBlacklistTags && ProcessTagPeaks)
            {
                stopParsedTags = new CancellationTokenSource();
                var chList = ChannelSplitter.CopyChannel(parsedTagChannel.Reader, 2, stopParsedTags.Token);
                collatorInputChannel = chList[0];
                blacklistInputChannel = chList[1];
            }
            else
            {
                stopParsedTags = null;
                if (DetectBlacklistTags)
                {
                    blacklistInputChannel = parsedTagChannel.Reader;
                }
                if (ProcessTagPeaks)
                {
                    collatorInputChannel = parsedTagChannel.Reader;
                }
            }

            // Reset collator and peak detector channels.
            if (ProcessTagPeaks)
            {
                tagCollectionChannel = Channel.CreateUnbounded<TagCollection>(new UnboundedChannelOptions() { SingleWriter = true, SingleReader = true });
                if (!tagCollator.SetChannel(collatorInputChannel, tagCollectionChannel))
                    return false;

                tagPeakChannel = Channel.CreateUnbounded<TagPeak>(new UnboundedChannelOptions() { SingleWriter = true, SingleReader = false });
                if (!peakDetector.SetChannel(tagCollectionChannel.Reader, tagPeakChannel))
                    return false;
            }

            // Reset blacklisted tag detector channel.
            if (DetectBlacklistTags)
            {

                if(!blacklistDetector.SetChannel(blacklistInputChannel))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Starts RFID manager and sub classes.
        /// </summary>
        /// <returns>Whether RFID manager was successfully started.</returns>
        public bool Start()
        {
            //Check that RFID is set up.
            if (!isSetUp.IsSet)
            {
                LogMessage("RfidManager has not been set up. Failed to start reader.");
                return false;
            }

            if (!ResetChannels())
            {
                LogMessage("Failed to start RFID manager. Failed to reset channels.");
                return false;
            }

            //start inputParser
            if (!inputParser.Start())
            {
                LogMessage("RFID input parser failed to start.");
                return false;
            }

            if (ProcessTagPeaks)
            {
                //start collating
                StartTagCollator();

                //start peak detectiong
                StartPeakDetector();
            }

            if (DetectBlacklistTags)
            {
                //start detecting blacklisted tags
                StartBlacklistDetector();
            }

            //Change isProcessing to true;
            IsProcessing = true;

            // Starting reader last ensures that system is ready to receive data 
            // before any is sent.
            if (!readerModule.Connect()) return false;
            if (readerModule.IsRunning) readerModule.Stop();
            return readerModule.Start();
        }

        /// <summary>
        /// Stops reading and processing data. Not guaranteed to process all collected data.
        /// </summary>
        /// <returns>Awaitable task of whether aborted successfully.</returns>
        public async Task<bool> Abort()
        {
            // Check that RFID is set up.
            if (!isSetUp.IsSet)
            {
                LogMessage("RfidManager has not been set up. Failed to stop reader.");
                return false;
            }

            // Stopping reader before processing ensures that no data is received and not processed.
            readerModule.Stop();

            // Stop inputParser and wait for it to finish.
            await inputParser.StopAndWait();
            stopParsedTags?.Cancel(); //$? Do we need this?

            if (ProcessTagPeaks)
            {
                await tagCollator.StopAndWait();
                await peakDetector.StopAndWait();
            }

            if (DetectBlacklistTags)
            {
                await blacklistDetector.StopAndWait();
            }

            //Change isProcessing to false;
            IsProcessing = false;
            return true;
        }

        /// <summary>
        /// Stops reading, but continues to process collected data.
        /// </summary>
        /// <returns>Awaitable task of whether stopped and finished processing successfully.</returns>
        public async Task<bool> StopAndProcess()
        {
            // Check that RFID is set up.
            if (!isSetUp.IsSet)
            {
                LogMessage("RfidManager has not been set up. Failed to stop reader.");
                return false;
            }

            // Stopping reader before processing ensures that no data is received and not processed.
            if (!readerModule.Stop())
            {
                LogMessage("Failed to stop RFID reader.");
            }

            // Stop inputParser and wait for it to finish.
            if (!await inputParser.StopAndWait())
            {
                LogMessage("Failed to stop input parser.");
            }
            stopParsedTags?.Cancel(); //$? Do we need this?

            if (ProcessTagPeaks)
            {
                await collatorInputChannel?.Completion; //This should not be null.
                await tagCollator.StopAndWait();

                await tagCollectionChannel.Reader.Completion;
                await peakDetector.StopAndWait();
            }

            if (DetectBlacklistTags)
            {
                await blacklistInputChannel?.Completion; //This should not be null.
                await blacklistDetector.StopAndWait();
            }

            //Change isProcessing to false;
            IsProcessing = false;
            return true;
        }

        /// <summary>
        /// Sends a string message to the RFID reader.
        /// </summary>
        /// <param name="message">Message to send to the reader.</param>
        public void SendToReader(string message)
        {
            //Check that RFID is set up.
            if (!isSetUp.IsSet)
            {
                throw new Exception("RfidManager has not been set up. Failed to send message to reader.");
            }

            readerModule.SendMessage(message);
        }

        /// <summary>
        /// Starts the <see cref="TagCollator"/> processing loop on its own thread.
        /// </summary>
        /// <returns>Whether thread was successfully started.</returns>
        private bool StartTagCollator()
        {
            collatorThread = new Thread(() =>
            {
                try
                {
                    tagCollator.Start();
                }
                catch (Exception e)
                {
                    LogMessage("RFID collator loop failed. " + e.GetType().FullName + ": " + e.Message);
                }
            })
            {
                Priority = ThreadPriority.Normal,
                Name = "RfidTagCollatorThread"
            };
            collatorThread.Start();

            return true;
        }

        /// <summary>
        /// Starts the <see cref="TagPeakDetector"/> processing loop on its own thread.
        /// </summary>
        /// <returns>Whether thread was successfully started.</returns>
        private bool StartPeakDetector()
        {
            peakDetectorThread = new Thread(() => {
                try
                {
                    peakDetector.Start();
                }
                catch (Exception e)
                {
                    LogMessage("RFID peak detector loop failed. " + e.GetType().FullName + ": " + e.Message);
                }
            })
            {
                Priority = ThreadPriority.Normal,
                Name = "RfidPeakDetectorThread"
            };
            peakDetectorThread.Start();

            return true;
        }

        /// <summary>
        /// Starts the <see cref="BlacklistDetector"/> processing loop on its own thread.
        /// </summary>
        /// <returns>Whether thread was successfully started.</returns>
        private bool StartBlacklistDetector()
        {
            blacklistDetectorThread = new Thread(() =>
            {
                try
                {
                    blacklistDetector.Start();
                }
                catch (Exception e)
                {
                    LogMessage("RFID blacklisted tag detector loop failed. " + e.GetType().FullName + ": " + e.Message);
                }
            })
            {
                Priority = ThreadPriority.Normal,
                Name = "RfidBlacklistDetectorThread"
            };
            blacklistDetectorThread.Start();

            return true;
        }

        /// <summary>
        /// Sets settings on RFID reader to values specified in RFID settings file. 
        /// May require reboot for some settings to take effect.
        /// </summary>
        /// <returns>Whether successfully reset sensor settings.</returns>
        public bool ResetReaderSettings()
        {
            //Check that RFID is set up.
            if (!isSetUp.IsSet)
            {
                LogMessage("RfidManager has not been set up. Failed to set settings.");
                return false;
            }

            return readerModule.FullResetSettings();
        }

        /// <summary>
        /// Reboots RFID reader. Note this is fake async; it blocks a threadpool thread.
        /// </summary>
        /// <returns>Awaitable task that completes when reader has rebooted.</returns>
        public async Task RebootReader()
        {
            //Check that RFID is set up.
            if (!isSetUp.IsSet)
            {
                LogMessage("RfidManager has not been set up. Failed to reboot.");
                return;
            }

            await readerModule.Reboot();
        }
    }
}
