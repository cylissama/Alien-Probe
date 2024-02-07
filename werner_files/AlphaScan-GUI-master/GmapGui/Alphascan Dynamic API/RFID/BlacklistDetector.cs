using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CES.AlphaScan.Base;
using System.Threading;
using System.Threading.Channels;

namespace CES.AlphaScan.Rfid
{
    /// <summary>
    /// Object for detecting blacklisted tags.
    /// </summary>
    public class BlacklistDetector : ILogMessage
    {
        /*
        The BlacklistDetector checks each tag against the blacklist to see if it matches. If 
        the tag ID is on the blacklist, it stores the tag data in an internal list, and 
        notifies the user.

        This class uses a similar processing loop as the TagCollator (see the comments in 
        that file for more details). The loop reads TagData from the input channel, detects 
        blacklisted tags, and raises the DetectedBlacklistTag event if a tag is found.

        The blacklisted tag detector has an internal list of tags found during that run. The 
        detector first checks a new tag against that list, so it doesn't notify about the 
        same tag twice. If it is not on the list, it checks the tag against the blacklist. 
        If the tag is on the blacklist, it notifies the user, and if it is not, nothing 
        happens. The internal list is cleared every time the loop is started.
        //*/

        #region Logging
        /// <summary>
        /// Name of the module.
        /// </summary>
        public string Name { get; private set; } = "BlacklistDetector";

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
        /// <summary>
        /// File path of the file containing the blacklist.
        /// </summary>
        private string BlacklistFileName { get; set; } = "";

        /// <summary>
        /// List of all tags on the blacklist.
        /// </summary>
        private IList<string> TagBlacklist { get; set; } = null;

        /// <summary>
        /// Sets the settings for the blacklisted tag detector.
        /// </summary>
        /// <param name="newSettings">New settings to apply to the collator.</param>
        /// <exception cref="ArgumentNullException">Thrown if no settings are passed as argument.</exception>
        /// <exception cref="InvalidOperationException">Thrown if tries to set settings while still collating.</exception>
        /// <exception cref="FormatException">Thrown if settings values fail to parse.</exception>
        public void SetSettings(IDictionary<string, string> newSettings)
        {
            if (newSettings == null || newSettings.Count < 1)
            {
                //No settings received
                throw new ArgumentNullException(nameof(newSettings), "No settings were sent to " + nameof(BlacklistDetector));
            }

            if (loopRunning.Wait(0))
            {
                try
                {
                    var settings = new Dictionary<string, string>(newSettings);

                    if (settings.ContainsKey("BlacklistFileName") && !string.IsNullOrWhiteSpace(settings["BlacklistFileName"]))
                    {
                        BlacklistFileName = settings["BlacklistFileName"];

                        if (!System.IO.File.Exists(BlacklistFileName))
                        {
                            BlacklistFileName = "";
                        }
                        else
                        {
                            TagBlacklist = System.IO.File.ReadAllLines(BlacklistFileName);
                        }
                    }
                }
                finally
                {
                    loopRunning.Release();
                }
            }
            else
            {
                //fail when system running
                throw new InvalidOperationException("Cannot change settings while system is running.");
            }
        }

        /// <summary>
        /// Sets the input channel to a new value. Fails if processing loop is running.
        /// </summary>
        /// <param name="tagDataChannel">New input channel.</param>
        /// <returns>Whether new channel was successfully set.</returns>
        public bool SetChannel(ChannelReader<TagData> tagDataChannel)
        {
            if (loopRunning.Wait(0))
            {
                try
                {
                    try
                    {
                        tagDataIn = tagDataChannel;
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

        /// <summary>
        /// Handles the cancelling of the processing loop. Used in <see cref="Stop"/> and <see cref="StopAndWait"/>
        /// </summary>
        private CancellationTokenSource stopProcessing = new CancellationTokenSource();

        /// <summary>
        /// Prevents multiple instances of the loop started in <see cref="Start"/> from running simultaneously.
        /// </summary>
        private SemaphoreSlim loopRunning = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Channel of TagData objects to be collated and processed.
        /// </summary>
        private ChannelReader<TagData> tagDataIn;

        /// <summary>
        /// List of the blacklisted tags that have been detected since the last reset.
        /// </summary>
        public IReadOnlyList<TagData> DetectedTags { get { return detectedTagsList.ToList(); } }

        private readonly List<TagData> detectedTagsList = new List<TagData>();

        /// <summary>
        /// Event to report tags detected on the blacklist.
        /// </summary>
        public event EventHandler<TagData> DetectedBlacklistTag;

        /// <summary>
        /// Constructs new <see cref="BlacklistDetector"/>.
        /// </summary>
        /// <param name="inputChannel">Channel of tag data to detect blacklisted tags in.</param>
        public BlacklistDetector(ChannelReader<TagData> inputChannel)
        {
            tagDataIn = inputChannel;
        }

        /// <summary>
        /// Starts loop that detects blacklisted tags and reports detections.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if attempting to start loop while loop is already running.</exception>
        /// <exception cref="NullReferenceException">Thrown if attempts to access <see cref="TagBlacklist"/> when it is null.</exception>
        public void Start()
        {
            // Only allow one loop to run at a time.
            if (!loopRunning.Wait(0))
            {
                // Only one loop can run at once.
                throw new InvalidOperationException("Attempted to start blacklisted tag detector loop when it was already running.");
            }
            
            try
            {
                if (TagBlacklist == null)
                {
                    throw new NullReferenceException("Tag blacklist is null.");
                }

                if (tagDataIn == null || tagDataIn.Completion.IsCompleted)
                {
                    // No input channel.
                    throw new InvalidOperationException("Tag data input channel is null or completed.");
                }

                detectedTagsList.Clear();

                stopProcessing = new CancellationTokenSource();

                while (!stopProcessing.IsCancellationRequested)
                {
                    //Read new tagData from channel
                    if (tagDataIn.TryRead(out TagData newData))
                    {
                        //$? Maybe save list of IDs instead of list of tags for more efficiency.
                        if (newData != null && (detectedTagsList.Count < 1 || !detectedTagsList.Select(x => x.TagId).Contains(newData.TagId)))
                        {
                            //Check if tag is blacklisted
                            if (CheckBlacklist(newData.TagId))
                            {
                                detectedTagsList.Add(newData);

                                //Report detected tag
                                //$? Does this need to be run on a threadpool thread? Currently makes popup on this thread.
                                this.DetectedBlacklistTag?.Invoke(this, newData);
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            tagDataIn.WaitToReadAsync(stopProcessing.Token).AsTask().Wait(); //Synchronous wait for this async method
                        }
                        catch (Exception) { continue; }
                    }
                }
            }
            finally
            {
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
        /// Checks if a tag ID is on the blacklist.
        /// </summary>
        /// <param name="tagId">Tag ID to check against blacklist.</param>
        /// <returns>Whether <paramref name="tagId"/> was on the blacklist.</returns>
        private bool CheckBlacklist(string tagId)
        {
            return TagBlacklist.Contains(tagId);
        }
    }
}
