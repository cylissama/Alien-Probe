using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CES.AlphaScan.Base;
using System.Threading;
using System.Threading.Channels;

namespace CES.AlphaScan.Rfid
{
    /// <summary>
    /// Receives and processes RFID tag data from RFID Reader. Handles message received events from the server 
    /// and outputs <see cref="TagData"/> objects through a channel.
    /// </summary>
    public class RfidInputParser : ILogMessage
    {
        /*
        The RfidInputParser receives tag data as a string message and parses it to create 
        TagData objects. It then outputs these TagData objects to a Channel<TagData> for 
        use by other objects in the RfidManager. In addition, it has the ability to save 
        TagData to a file and is able to filter tag IDs by company code and year code.

        The input parser receives tag data messages by handling the ServerMessageReceived 
        event in the nsAlienRFID2.CAlienServer class. The subscription is handled in the 
        RfidReaderModule. This event is raised on the threadpool so thread safety is a 
        concern. There can be multiple calls to this handler at the same time from 
        multiple threads.

        When the event is raised, the handler first checks if the channel is setup and if 
        parsing is allowed. This is determined by the isChannelSetUp ManualResetEventSlim 
        and the allowParsing CancellationTokenSource respectively. 
        Whenever the Stop method is called, it completes the channel, so a new channel is 
        required. isChannelSetUp prevents starting until the new channel is set.
        allowParsing acts like an on/off switch for the module. The start and stop 
        functions replace or cancel the token. When it is cancelled, no new events are 
        handled.

        If neither check returns, then it will increment the SafeCounter threadsParsingCount. 
        This threadsafe counter keeps track of how many threads (or calls to this method) are 
        currently running, so we don't reset settings when things are running. The rest of 
        the method is in a try-finally block, so the threadsParsingCount is always decremented 
        at the end.

        Next the actual function begins. The tag data string is parsed assuming the string has 
        the default custom format. If the TagRequirements object is not set to null, it will 
        check the ID of each TagData to ensure it meets the requirements. If it doesn't, it 
        will be discarded. The function then returns a list of TagData objects (multiple tags 
        can be sent in one message). Then it will write each TagData to the channel tagDataOut. 
        If saveTagData is set to true, then it will try to save the list of tag data. After 
        saving, the function is done and threadsParsingCount decrements.

        To save tag data, first the data is added to a list of data to save. To reduce overhead 
        when saving to a file, it attempts to save a list of data at a single time. To do this, 
        after adding the data to the list dataList, it checks the length of dataList. If the 
        length of dataList is greater than the hardcoded value numDataToSend (by default 16), 
        then it will attempt to save the entire list of data. This is done with a call to 
        IOutputManager.TrySaveData(). The TagData objects are saved in a CSV file.
        //*/

        /// <summary>
        /// Output channel for the parsed <see cref="TagData"/> objects. Completed when the 
        /// stop method is called. Reset in the SetChannel method.
        /// </summary>
        private ChannelWriter<TagData> tagDataOut;

        /// <summary>
        /// Whether the channel for the parser has been set up yet. Is reset whenever the 
        /// channel completes and set whenever the channel is set.
        /// </summary>
        private readonly ManualResetEventSlim isChannelSetUp = new ManualResetEventSlim(false);

        /// <summary>
        /// The newline characters the RFID reader uses in its messages.
        /// </summary>
        private readonly string[] newLineChars = { "\r\n" };

        #region Tag ID Requirements
        /// <summary>
        /// Tag requirements to check. Company code and year code. If null, all tags pass check.
        /// </summary>
        private TagRequirements tagReq = null;

        #endregion

        #region Threading
        /// <summary>
        /// Cancellation token controlling whether parsing is allowed. Used like on/off switch for parsing module.
        /// </summary>
        private CancellationTokenSource allowParsing = new CancellationTokenSource();

        /// <summary>
        /// Whether parsing is allowed. If not allowed, any new events will be ignored, 
        /// not buffered. Set changes to allow or cancel parsing.
        /// </summary>
        /// <value>Cancels or replaces <see cref="CancellationTokenSource"/> field, <see cref="allowParsing"/>.</value>
        public bool ParsingAllowed
        {
            get { return !allowParsing.IsCancellationRequested; }
            set
            {
                if (value == allowParsing.IsCancellationRequested)
                {
                    if (value) allowParsing = new CancellationTokenSource();
                    else allowParsing.Cancel();
                }
            }
        }

        /// <summary>
        /// Returns in threadsafe way whether any threads are still processing tags.
        /// </summary>
        /// <returns>Boolean value of whether any threads are still processing data.</returns>
        public bool IsProcessing => threadsParsingCount.CountNotZero;

        /// <summary>
        /// A thread-safe counter that keeps track of how many are still parsing data.
        /// </summary>
        private readonly SafeCounter threadsParsingCount = new SafeCounter();

        #endregion

        #region Logging
        /// <summary>
        /// Name of the code module.
        /// </summary>
        public string Name { get; private set; } = "InputParser";

        /// <summary>
        /// Logs message string.
        /// </summary>
        /// <param name="message"></param>
        private void LogMessage(string message)
        {
            MessageLogged?.Invoke(this, new LogMessageEventArgs(message, Name));
        }

        /// <summary>
        /// Event through which messages to log are sent.
        /// </summary>
        public event EventHandler<LogMessageEventArgs> MessageLogged;

        #endregion

        #region Data Saving

        private readonly int numDataToSend = 16;
        private string fileName = "TagData";

        private bool saveTagData = false;

        private readonly List<TagData> dataList = new List<TagData>();
        private readonly SemaphoreSlim dataListSem = new SemaphoreSlim(1, 1);

        private IOutputManager outputManager;

        /// <summary>
        /// Adds data to a list to save. When enough data is in list, saves data to file.
        /// </summary>
        /// <param name="newData">Collection of new data to add to list to save.</param>
        private async Task AddDataToSave(IEnumerable<TagData> newData)
        {
            if (!saveTagData || newData == null || newData.Count() < 1) return;

            List<TagData> dataToSend = null;

            await dataListSem.WaitAsync(allowParsing.Token).ConfigureAwait(false);
            try
            {
                dataList.AddRange(newData);
                if (dataList.Count >= numDataToSend)
                {
                    dataToSend = dataList.ToList();
                    dataList.Clear();
                }
            }
            finally
            {
                dataListSem.Release();
            }

            if (dataToSend != null) 
                SaveData(dataToSend);
        }

        /// <summary>
        /// Saves a list of data through the <see cref="OutputManager"/>.
        /// </summary>
        /// <param name="dataToSend">Collection of data to save.</param>
        private void SaveData(IEnumerable<TagData> dataToSend)
        {
            try
            {
                outputManager.TrySaveData(fileName, dataToSend);
            }
            catch (OperationCanceledException)
            {
                //Probably do nothing.
            }
            catch (Exception e)
            {
                LogMessage("Failed to save TagData: " + e.Message);
            }
        }

        #endregion

        /// <summary>
        /// Constructs new <see cref="RfidInputParser"/> using a specified output channel and tag requirements.
        /// </summary>
        /// <param name="outputChannel">Channel to output tag data to.</param>
        /// <param name="_tagReq">Tag requirements to check against each tag.</param>
        public RfidInputParser(Channel<TagData> outputChannel, TagRequirements _tagReq = null)
        {
            // Update tag requirements.
            tagReq = _tagReq;

            tagDataOut = outputChannel.Writer;
            allowParsing.Cancel(); //Construct in "stopped" state.
            isChannelSetUp.Set();
        }

        /// <summary>
        /// Constructs new <see cref="RfidInputParser"/> using a specified output channel and tag requirements. 
        /// Enables saving using the specified <paramref name="outputManager"/> and <paramref name="fileName"/>.
        /// </summary>
        /// <param name="outputChannel">Channel to output tag data to.</param>
        /// <param name="outputManager">Output manager to use to save tag data.</param>
        /// <param name="fileName">Name of file to save tag data to.</param>
        /// <param name="_tagReq">Tag requirements to check against each tag.</param>
        public RfidInputParser(Channel<TagData> outputChannel, IOutputManager outputManager, string fileName = "TagData.csv", TagRequirements _tagReq = null)
        {
            // Update tag requirements.
            tagReq = _tagReq;

            tagDataOut = outputChannel.Writer;
            if (outputManager == null) LogMessage("No output manager passed. Unable to save TagData.");
            else
            {
                saveTagData = true;
                if (!string.IsNullOrWhiteSpace(fileName) && fileName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) < 0)
                    this.fileName = fileName;
                this.outputManager = outputManager;
                dataList = new List<TagData>(numDataToSend);
            }
            allowParsing.Cancel(); //Construct in "stopped" state.
            isChannelSetUp.Set();
        }

        /// <summary>
        /// Called when a message is received by the server from RFID Reader.
        /// Parses RFID Reader message for tag data. Assumes reader is using custom tag message format.
        /// </summary>
        /// <param name="data">Message as string.</param>
        public async void Server_MessageReceived(string data)
        {
            //Do nothing if output channel is not set up.
            if (!isChannelSetUp.IsSet) return;

            //Do nothing if parsing is not allowed. Input parser is "turned off".
            if (allowParsing.IsCancellationRequested) return;

            threadsParsingCount.Increment(); //increments number of threads parsing data
            try
            {
                //Parse message for tag data.
                IEnumerable<TagData> newData = ParseCustomTag(data);

                foreach (TagData tagData in newData)
                {
                    // Writes tag data to channel.
                    await tagDataOut.WriteAsync(tagData, allowParsing.Token).ConfigureAwait(false);
                }

                if (saveTagData)
                    await AddDataToSave(newData);
            }
            finally
            {
                threadsParsingCount.Decrement(); //decrements number of threads parsing data when finished
            }
        }

        //$$ try using built in xml decoding
        /// <summary>
        /// Parses tag list string in custom XML format for TagData objects. If set up to, 
        /// only returns tags whose IDs meet the TagRequirements.
        /// </summary>
        /// <param name="data">Message from reader as string.</param>
        /// <returns>Array of TagData objects found.</returns>
        private IEnumerable<TagData> ParseCustomTag(string data)
        {
            List<TagData> tagInfoList = new List<TagData>();

            string[] lines = data.Split(newLineChars, StringSplitOptions.RemoveEmptyEntries);
            List<string> tagStrList = new List<string>();

            //Searches for lines containing RFID tag info in XML format.
            foreach (string line in lines)
            {
                if (line.Contains("<Alien-RFID-Tag>"))
                {
                    tagStrList.Add(line);
                }
            }

            //Parses each line for tag info and adds each new TagData to tagInfoList
            foreach (string s in tagStrList)
            {
                int i = s.IndexOf("<Alien-RFID-Tag>") + "<Alien-RFID-Tag>".Length;
                int endTag = s.IndexOf("</Alien-RFID-Tag>");

                Dictionary<string, string> tagValues = new Dictionary<string, string>();

                //Goes through entire string of tag info and parses tag data
                while (i < endTag)
                {
                    int start = s.IndexOf('<', i);
                    int end = s.IndexOf('>', start);
                    string valName = "";
                    for (int j = start + 1; j < end; j++)
                    {
                        valName += s[j];
                    }

                    string valStr = "";
                    int endVal = s.IndexOf("</" + valName + ">", end);

                    for (int j = end + 1; j < endVal; j++)
                    {
                        valStr += s[j];
                    }

                    //Adds new data value to tagValues dictionary.
                    tagValues.Add(valName, valStr);

                    //Sets begin index for the next data packet.
                    i = endVal + 2 + valName.Length + 1;
                }

                if (!DetermineTagValid(tagValues["TagID"]))
                    continue;
                //Creates a TagData object from data values and adds it to tagInfoList.
                tagInfoList.Add(TagDataConstructor(tagValues));
            }

            return tagInfoList;
        }

        /// <summary>
        /// Checks if a tag ID is valid for the company and year. Uses <see cref="tagReq"/> 
        /// to do this. All IDs are valid if <see cref="tagReq"/> is null.
        /// </summary>
        /// <param name="id">ID to verify.</param>
        /// <returns>Whether ID contains the correct substrings.</returns>
        private bool DetermineTagValid(string id)
        {
            if (tagReq == null) return true;

            //$? Probably not very efficient. Try improving.
            try
            {
                return id.Substring(tagReq.TagCodeIdx, tagReq.TagCode.Length).Equals(tagReq.TagCode)
                    && id.Substring(tagReq.YearCodeIdx, tagReq.YearCode.Length).Equals(tagReq.YearCode);
            }
            catch
            {
                return false;
            }
        }

        //$$ Add error checking. Handle if fails to parse, or value missing.
        /// <summary>
        /// Creates a <see cref="TagData"/> object from a dictionary of names of values and their values as strings.
        /// </summary>
        /// <param name="tagValues">Names and values of each data parsed from the string message.</param>
        /// <returns><see cref="TagData"/> object with the specified values.</returns>
        private TagData TagDataConstructor(Dictionary<string, string> tagValues)
        {
            string tagId = "None";
            long lastSeen = 0;
            double rssi = double.NaN;
            int rx = -1;

            string value;
            foreach (string name in tagValues.Keys)
            {
                value = tagValues[name];
                switch (name)
                {
                    case "TagID":
                        tagId = value;
                        break;
                    case "Last":
                        lastSeen = long.Parse(value);
                        break;
                    case "RSSI":
                        rssi = double.Parse(value);
                        break;
                    case "RX":
                        rx = int.Parse(value);
                        break;
                    default:
                        break;
                }
            }

            return new TagData(tagId, rssi, lastSeen, rx);
        }

        /// <summary>
        /// Stops new threads parsing and waits asynchronously for the count of threads 
        /// parsing messages to equal zero.
        /// </summary>
        /// <param name="timeout">Number of milliseconds to wait before timing out. 
        /// 0 does not wait. -1 has no timeout.</param>
        /// <returns>Whether the count reached zero successfully.</returns>
        public async Task<bool> StopAndWait(int timeout = -1)
        {
            // Prevent new parsing threads from starting and wait until the ones currently running are finished.
            allowParsing.Cancel();
            bool success = await threadsParsingCount.WaitForZero(timeout);

            // Complete channel. This allows later stages to know that no more data is coming.
            tagDataOut?.TryComplete();
            isChannelSetUp.Reset();

            //Save remaing data.
            if (saveTagData && success)
            {
                await Task.Run(async () =>
                {
                    await dataListSem.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        if (dataList.Count > 0)
                            SaveData(dataList);
                        dataList.Clear();
                    }
                    finally
                    {
                        dataListSem.Release();
                    }
                });
            }
            return success;
        }

        /// <summary>
        /// Resets input parser to allow parsing. Requires the output channel to be set first.
        /// </summary>
        /// <returns>Whether successfully allowed parsing.</returns>
        public bool Start()
        {
            if (!isChannelSetUp.IsSet)
                return false;
            allowParsing = new CancellationTokenSource();
            return true;
        }

        /// <summary>
        /// Sets settings for the input parser, including: whether it should save tag data, 
        /// where to save it, and what tag requirements to check.
        /// </summary>
        /// <param name="_saveTagData">Whether the parser should save tag data.</param>
        /// <param name="_tagReq">Tag ID requirements to check. If null, all tags are valid.</param>
        /// <param name="_tagDataFileName">Name of file to save tag data to. Unused if not set to save.</param>
        /// <param name="_outputManager">Output manager to use to save tag data. Unused if not set to save.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="_saveTagData"/> is true and 
        /// either <paramref name="_tagDataFileName"/> or <paramref name="_outputManager"/> are not 
        /// valid.</exception>
        /// <exception cref="InvalidOperationException">Thrown if data is still being parsed.</exception>
        public bool SetSettings(bool _saveTagData, TagRequirements _tagReq = null, string _tagDataFileName = "TagData.csv", IOutputManager _outputManager = null)
        {
            if (!allowParsing.IsCancellationRequested || threadsParsingCount.CountNotZero)
                return false;

            if (_saveTagData)
            {
                // Check that all needed settings are provided.
                if ((string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) > -1) &&
                        (string.IsNullOrWhiteSpace(_tagDataFileName) || _tagDataFileName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) > -1))
                    if (string.IsNullOrWhiteSpace(fileName) && string.IsNullOrWhiteSpace(_tagDataFileName)) throw new ArgumentException("No valid tag data file name provided.", nameof(_tagDataFileName));
                if (outputManager == null && _outputManager == null) throw new ArgumentException("No valid output manager passed.", nameof(_outputManager));
            }

            if (ParsingAllowed || threadsParsingCount.CountNotZero) throw new InvalidOperationException("Attempted to change settings while data is being parsed.");

            dataListSem.Wait(50);
            try
            {
                //$? This does not allow clearing TagRequirements to null if previously set. Is this what we want?
                // Update tag requirements.
                if (_tagReq != null)
                {
                    tagReq = _tagReq;
                }

                saveTagData = _saveTagData;
                if (_saveTagData)
                {
                    // Check that file name is valid.
                    if (string.IsNullOrWhiteSpace(_tagDataFileName) || _tagDataFileName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) > -1)
                    {
                        if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) > -1)
                            throw new ArgumentException("No valid tag data file name provided.", nameof(_tagDataFileName));
                    }
                    else fileName = _tagDataFileName;

                    if (_outputManager != null) outputManager = _outputManager;
                }
                dataList?.Clear();
            }
            finally
            {
                dataListSem.Release();
            }

            return true;
        }

        /// <summary>
        /// Sets the output channel to a new value. Fails if parsing allowed. Need to reset 
        /// the channel after each time the input parser is stopped.
        /// </summary>
        /// <param name="outputChannel">New output channel.</param>
        /// <returns>Whether new channel was successfully set.</returns>
        public bool SetChannel(Channel<TagData> outputChannel)
        {
            if (!allowParsing.IsCancellationRequested || threadsParsingCount.CountNotZero)
            {
                LogMessage("Failed to set channel. Threads still parsing.");
                return false;
            }

            tagDataOut = outputChannel.Writer;
            isChannelSetUp.Set();
            return true;
        }
    }
}
