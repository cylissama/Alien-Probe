using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Threading;
using CES.AlphaScan.Base;

namespace CES.AlphaScan.Rfid
{
    /// <summary>
    /// Analyses TagCollection objects to find the times of the peak strength values.
    /// </summary>
    public class TagPeakDetector : ILogMessage
    {
        /*
        The TagPeakDetector analyzes the read strength of each tag with each antenna over 
        time and finds the peaks the signal.

        When the start method is called, it starts the same kind of processing loop as the 
        one in TagCollator (see those comments for more detailed explanation). The loop 
        reads TagCollection objects from the input channel, looks for peaks in the strength 
        data, and exports a TagPeak object if possible.

        The peak detector analyzes each AntennaCollection in a TagCollection. This is a the 
        strength of the tag over time as read by a single antenna. The data analyzed is the 
        RSSI strength and the time stamp. To find peaks, we convert the data to the correct 
        units. Next we smooth out the edges of data by adding zero values. A data point of 
        strength zero would not be read, but these values are used for the other processing 
        functions. Next we perform a linear interpolation to have a common timebase. Then we 
        take the moving average over the data to smooth out high frequency noise. Then we 
        find local maxima (peaks) and filter these peaks with thresholds. We are left with a 
        list of peaks for the antenna. This is repeated for each antenna in the TagCollection.

        Next we export. In order to export a TagPeak, we need a peak from two antennas that 
        are on the same side of the vehicle. If we do not get enough data, then we fail to 
        make a TagPeak and export nothing. If we have enough data, we create a TagPeak object 
        with the first and last antenna peaks and write this TagPeak to the output channel.
        //*/

        #region Logging
        /// <summary>
        /// Name of the module.
        /// </summary>
        public string Name { get; private set; } = "RSSIPeakDetector";

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
        private AntennaArrangement antArrangement = null;

        /// <summary>
        /// Length of time (in ms) that zeros added to beginning and end of collections will span.
        /// </summary>
        public int ZeroTimeLength { get; private set; } = 600;
        /// <summary>
        /// Amount of time (in ms) in between each zero value added to collection data.
        /// </summary>
        public int ZeroPeriod { get; private set; } = 100;
        /// <summary>
        /// Period (in ms) that new interpolated data will have.
        /// </summary>
        public uint LinInterp_SamplePeriod { get; private set; } = 20;
        /// <summary>
        /// Width (in # data points) of window to take moving average over.
        /// </summary>
        public int MovMean_WindowWidth { get; private set; } = 60;
        /// <summary>
        /// Minimum strength (in not-dB RSSI) required so that a strength peak will not be immediately filtered out.
        /// </summary>
        public double PeakFilter_MinPeak { get; private set; } = 0;
        /// <summary>
        /// Minimum time (in ms) allowed between peaks. If time difference is smaller, larger peak will be used.
        /// </summary>
        public double PeakFilter_MinPeakDist { get; private set; } = 4000;



        /// <summary>
        /// Sets the settings for the peak detector.
        /// </summary>
        /// <param name="newSettings">New settings to apply to the peak detector.</param>
        /// <exception cref="ArgumentNullException">Thrown if no settings are passed as argument.</exception>
        /// <exception cref="InvalidOperationException">Thrown if tries to set settings while still detecting peaks.</exception>
        /// <exception cref="FormatException">Thrown if settings values is unable to parse from that format.</exception>
        /// <exception cref="OverflowException">Thrown if settings value is too large for the data type.</exception>
        public void SetSettings(IDictionary<string, string> newSettings, AntennaArrangement antArrang = null)
        {
            if (newSettings == null || newSettings.Count < 1)
            {
                // No settings received
                throw new ArgumentNullException(nameof(newSettings), "No settings were sent to " + nameof(TagPeakDetector));
            }

            if (loopRunning.Wait(0))
            {
                try
                {
                    if (antArrang != null)
                    {
                        antArrangement = antArrang;
                    }
                    else if (antArrangement == null)
                    {
                        throw new ArgumentNullException(nameof(newSettings), "No " + nameof(AntennaArrangement) + " was sent to " + nameof(TagPeakDetector));
                    }

                    var settings = new Dictionary<string, string>(newSettings);

                    if (settings.ContainsKey("AddZeros.ZeroTimeLength") && !string.IsNullOrWhiteSpace(settings["AddZeros.ZeroTimeLength"]))
                    {
                        ZeroTimeLength = int.Parse(settings["AddZeros.ZeroTimeLength"]);
                    }

                    if (settings.ContainsKey("AddZeros.ZeroPeriod") && !string.IsNullOrWhiteSpace(settings["AddZeros.ZeroPeriod"]))
                    {
                        ZeroPeriod = int.Parse(settings["AddZeros.ZeroPeriod"]);
                    }

                    if (settings.ContainsKey("LinearInterpolation.SamplePeriod") && !string.IsNullOrWhiteSpace(settings["LinearInterpolation.SamplePeriod"]))
                    {
                        LinInterp_SamplePeriod = uint.Parse(settings["LinearInterpolation.SamplePeriod"]);
                    }

                    if (settings.ContainsKey("MovingMean.WindowWidth") && !string.IsNullOrWhiteSpace(settings["MovingMean.WindowWidth"]))
                    {
                        MovMean_WindowWidth = int.Parse(settings["MovingMean.WindowWidth"]);
                    }

                    if (settings.ContainsKey("FilterPeaks.MinPeak") && !string.IsNullOrWhiteSpace(settings["FilterPeaks.MinPeak"]))
                    {
                        PeakFilter_MinPeak = double.Parse(settings["FilterPeaks.MinPeak"]);
                    }

                    if (settings.ContainsKey("FilterPeaks.MinPeakDist") && !string.IsNullOrWhiteSpace(settings["FilterPeaks.MinPeakDist"]))
                    {
                        PeakFilter_MinPeakDist = double.Parse(settings["FilterPeaks.MinPeakDist"]);
                    }
                }
                finally
                {
                    loopRunning.Release();
                }
            }
            else
            {
                // Fail when system running.
                throw new InvalidOperationException("Cannot change settings while system is running.");
            }
        }

        /// <summary>
        /// Sets the input and output channel to a new value. Fails if processing loop is running.
        /// </summary>
        /// <param name="tagCollectionChannel">New input channel.</param>
        /// <param name="tagPeakChannel">New output channel.</param>
        /// <returns>Whether new channels were successfully set.</returns>
        public bool SetChannel(ChannelReader<TagCollection> tagCollectionChannel, Channel<TagPeak> tagPeakChannel)
        {
            if (loopRunning.Wait(0))
            {
                try
                {
                    try
                    {
                        tagCollectionIn = tagCollectionChannel;
                        tagPeakOut = tagPeakChannel;
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

        #region Data Saving
        //$$ This thread safety is probably unnecessary as this is all one thread. It 
        //  was copied from InputParser and should probably be deleted.

        /// <summary>
        /// The number of tag peak data points to collect before saving them to a file. Intended to increase efficiency due to overhead of file IO.
        /// </summary>
        private readonly int numDataToSend = 1;

        /// <summary>
        /// The name of the file to save tag peak data to.
        /// </summary>
        private string fileName = "TagPeakData";

        /// <summary>
        /// Whether tag peak data will be saved to file.
        /// </summary>
        private bool saveTagPeaks = false;

        private readonly object _dataListLock = new object();
        // original
        //private List<TagPeak> dataList = new List<TagPeak>();
        // no idea why this needs to be private, unsure how to access - murphey
        public List<TagPeak> dataList = new List<TagPeak>();

        private CES.AlphaScan.Base.IOutputManager outputManager;

        /// <summary>
        /// Adds data to a list to save. When enough data is in list, saves data to file.
        /// </summary>
        /// <param name="newData">Collection of new data to add to list to save.</param>
        private void AddDataToSave(TagPeak newData)
        {
            if (!saveTagPeaks) return;

            List<TagPeak> dataToSend = null;

            lock (_dataListLock)
            {
                dataList.Add(newData);
                if (dataList.Count >= numDataToSend)
                {
                    dataToSend = dataList.ToList();
                    dataList.Clear();
                }
            }

            if (dataToSend != null) SaveData(dataToSend);
        }

        /// <summary>
        /// Saves a list of data through the <see cref="OutputManager"/>.
        /// </summary>
        /// <param name="dataToSend">Collection of data to save.</param>
        private void SaveData(IEnumerable<TagPeak> dataToSend)
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
                LogMessage("Failed to save TagPeakData: " + e.Message);
            }
        }

        #endregion

        /// <summary>
        /// Whether tag peak detection loop is running.
        /// </summary>
        public bool IsProcessing { get; private set; }

        /// <summary>
        /// Reader for input channel of tag collections.
        /// </summary>
        private ChannelReader<TagCollection> tagCollectionIn;

        /// <summary>
        /// Output channel of tag peak data.
        /// </summary>
        private Channel<TagPeak> tagPeakOut;

        /// <summary>
        /// Handles the cancelling of the processing loop. Used in <see cref="Stop"/> and <see cref="StopAndWait"/>
        /// </summary>
        private CancellationTokenSource stopProcessing = new CancellationTokenSource();

        /// <summary>
        /// Prevents multiple instances of the loop started in <see cref="Start"/> from running simultaneously.
        /// </summary>
        private readonly SemaphoreSlim loopRunning = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Constructor without file saving.
        /// </summary>
        /// <param name="channelIn">Input channel of tag collections.</param>
        /// <param name="channelOut">Output channel of tag peak data.</param>
        /// <param name="antennaArrangement">Arrangement of RFID antennas.</param>
        public TagPeakDetector(ChannelReader<TagCollection> channelIn, Channel<TagPeak> channelOut, AntennaArrangement antennaArrangement = null)
        {
            tagCollectionIn = channelIn;
            tagPeakOut = channelOut;
            if (antennaArrangement != null)
                antArrangement = antennaArrangement;
        }

        /// <summary>
        /// Constructor with file saving.
        /// </summary>
        /// <param name="channelIn">Input channel of tag collections.</param>
        /// <param name="channelOut">Output channel of tag peak data.</param>
        /// <param name="outputManager">Output manager to use to save data.</param>
        /// <param name="fileName">Name of file to save peak data to.</param>
        public TagPeakDetector(ChannelReader<TagCollection> channelIn, Channel<TagPeak> channelOut, IOutputManager outputManager, string fileName = "TagPeakData.csv")
        {
            tagCollectionIn = channelIn;
            tagPeakOut = channelOut;
            if (outputManager == null) LogMessage("No output manager passed. Unable to save TagPeakData.");
            else
            {
                saveTagPeaks = true;
                if (!string.IsNullOrWhiteSpace(fileName) && fileName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) < 0)
                    this.fileName = fileName;
                this.outputManager = outputManager;
                dataList = new List<TagPeak>(numDataToSend);
            }
        }

        /// <summary>
        /// Starts the loop that detects peaks in incoming tag collections. Only one loop can run at once.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if attempting to start loop while loop is already running.</exception>
        public void Start()
        {
            // Only allow one loop to run at a time.
            if (!loopRunning.Wait(0))
            {
                // Only one loop can run at once.
                throw new InvalidOperationException("Attempted to start tag peak detector loop when it was already running.");
            }
            try
            {
                if (tagCollectionIn == null || tagCollectionIn.Completion.IsCompleted)
                {
                    // No input channel.
                    throw new InvalidOperationException("Tag collection input channel is null or completed.");
                }

                if (tagPeakOut == null || tagPeakOut.Reader.Completion.IsCompleted)
                {
                    // No output channel.
                    throw new InvalidOperationException("Tag peak output channel is null or completed.");
                }

                stopProcessing = new CancellationTokenSource();
                IsProcessing = true;

                while (!stopProcessing.IsCancellationRequested)
                {
                    //Read new tagCollection from channel
                    try
                    {
                        tagCollectionIn.WaitToReadAsync(stopProcessing.Token).AsTask().Wait(); //Synchronous wait for this async method
                    }
                    catch (Exception) { continue; }

                    //Try to read new data. Process and export.
                    if (tagCollectionIn.TryRead(out TagCollection newData))
                    {
                        var peakCollection = ProcessCollection(newData);
                        var peakData = PairPeaks(peakCollection);

                        //Export peak data on channel //$$ Requires testing. May not work as intended.
                        tagPeakOut.Writer.TryWrite(peakData);

                        //Save tag peak data if configured to.
                        if (saveTagPeaks) AddDataToSave(peakData);
                    }
                }
            }
            finally
            {
                IsProcessing = false;
                tagPeakOut?.Writer.TryComplete();

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
        /// Converts power in dB to power in Watts.
        /// </summary>
        /// <param name="decibels">Value in dB to convert.</param>
        /// <returns>Amplitude value in Watts.</returns>
        public static double DecibelsToAmplitude(double decibels)
        {
            return Math.Pow(10, decibels / 10);
        }

        /// <summary>
        /// Converts power in Watts to power in dB
        /// </summary>
        /// <param name="amplitude">Value to convert to dB.</param>
        /// <returns>Value in dB.</returns>
        public static double AmplitudeToDecibels(double amplitude)
        {
            return Math.Log10(amplitude) * 10;
        }

        /// <summary>
        /// Analyses TagCollection object and returns $$ (determine return type).
        /// </summary>
        /// <param name="collection">Tag collection that is analysed.</param>
        /// <returns></returns>
        private TagCollection ProcessCollection(TagCollection collection)
        {
            string tagId = collection.TagId;

            var ACList = new List<TagCollection.AntennaCollection>();

            // Generate list of collections of peak values for each antenna.
            foreach (TagCollection.AntennaCollection ac in collection.AntennaCollections)
            {
                if (ac.Count > 2) //Needs at least 3 data points to process
                {
                    //var antennaPeaks = new TagCollection.AntennaCollection(ac.AntennaId, Process(ac.DataList));
                    if (this.antArrangement.LeftSide.Contains(ac.AntennaId))
                    {
                        var peak = Process(ac.DataList).OrderByDescending(x => x.Item2).First();
                        var antennaPeaks = new TagCollection.AntennaCollection(ac.AntennaId);
                        antennaPeaks.Add(peak.Item1 * 10000, peak.Item2);
                        ACList.Add(antennaPeaks);
                    }
                    else
                    {
                        var peak = Process(ac.DataList).OrderByDescending(x => x.Item2).First();
                        var antennaPeaks = new TagCollection.AntennaCollection(ac.AntennaId);
                        antennaPeaks.Add(peak.Item1 * 10000, peak.Item2);
                        ACList.Add(antennaPeaks);
                    }
                }
                else if (ac.Count > 0)
                {
                    //$? Maybe add some peak detection for collections with 1 or 2 data points. -- otherwise can remove this elseif
                    if (ac.Count == 1)
                    {
                        Tuple<long, double> peak = ac.DataList[0];
                        var antennaPeaks = new TagCollection.AntennaCollection(ac.AntennaId);
                        antennaPeaks.Add(peak.Item1, DecibelsToAmplitude(peak.Item2));

                        ACList.Add(antennaPeaks);
                    }
                    else
                    {
                        Tuple<long, double> point1 = ac.DataList[0];
                        Tuple<long, double> point2 = ac.DataList[1];
                        var antennaPeaks = new TagCollection.AntennaCollection(ac.AntennaId);
                        antennaPeaks.Add((point2.Item1 + point1.Item1) / 2, (DecibelsToAmplitude(point2.Item2) + DecibelsToAmplitude(point1.Item2)) / 2);

                        ACList.Add(antennaPeaks);
                    }
                }
            }

            // Create tag collection from list of antenna peak collections.
            var tagPeaks = new TagCollection(collection.TagId, ACList);

            //Output
            return tagPeaks;
        }

        /// <summary>
        /// Performs preprocessing and peak detection on data from a single antenna collection.
        /// </summary>
        /// <param name="rawData">Time vs RSSI data from a single antenna collection.</param>
        /// <returns>List of peak values. Strength as amplitude. Time as ms.</returns>
        private IList<Tuple<long, double>> Process(List<Tuple<long, double>> rawData)
        {
            Tuple<List<long>, List<double>> data = Unpack(rawData);

            List<long> timeData = new List<long>(data.Item1.Count);
            List<double> strengthData = new List<double>(data.Item2.Count);

            // Change time to ms
            foreach (long t in data.Item1)
            {
                timeData.Add(t / 10000); // Based on time stored as C# ticks.
            }

            // Convert signal from decibels
            foreach (double s in data.Item2)
            {
                strengthData.Add(DecibelsToAmplitude(s));
            }

            // Signal conditioning before finding peaks
            data = AddZeros(timeData, strengthData, ZeroTimeLength, ZeroPeriod);

            data = LinearlyInterpolate(data.Item1, data.Item2, LinInterp_SamplePeriod);

            timeData = data.Item1;
            strengthData = MovingMean(data.Item2, MovMean_WindowWidth);

            // Find and filter peaks
            var peaks = FilterPeaks(timeData, strengthData, PeakFilter_MinPeak, PeakFilter_MinPeakDist);

            //$? Maybe turn strength back to dB

            return peaks;

            //$$ add some checking that list sizes are the same

        }

        //$? Maybe move this function to another class. Class of utilities?
        /// <summary>
        /// "Unpacks" a list of 2-tuples to a 2-tuple of lists.
        /// </summary>
        /// <typeparam name="T1">First type in the tuple.</typeparam>
        /// <typeparam name="T2">Second type in the tuple.</typeparam>
        /// <param name="tupleList">List of tuples to unpack.</param>
        /// <returns>Tuple of lists containing the original data.</returns>
        public static Tuple<List<T1>, List<T2>> Unpack<T1, T2>(IEnumerable<Tuple<T1, T2>> tupleList)
        {
            List<T1> list1 = new List<T1>(tupleList.Count());
            List<T2> list2 = new List<T2>(tupleList.Count());

            foreach (Tuple<T1, T2> t in tupleList)
            {
                list1.Add(t.Item1);
                list2.Add(t.Item2);
            }

            return new Tuple<List<T1>, List<T2>>(list1, list2);
        }

        /// <summary>
        /// Adds strength data points of value zero to beginning and end of data. Intended to improve accuracy of future processing.
        /// </summary>
        /// <param name="timeData">Time data to add new values to.</param>
        /// <param name="strengthData">Strength data to add new zero values to.</param>
        /// <param name="zeroTimeLength">Length of time interval over which to add zeros. In milliseconds.</param>
        /// <param name="zeroPeriod">Length of time between each zero. In milliseconds.</param>
        public static Tuple<List<long>, List<double>> AddZeros(List<long> timeData, List<double> strengthData, int zeroTimeLength, int zeroPeriod)
        {
            // Find start and end times of original data
            long startTime = timeData.Min();
            long endTime = timeData.Max(); ;


            List<long> newTimes = new List<long>(zeroTimeLength / zeroPeriod);

            // Find times to add before start of data
            for (long i = startTime - zeroPeriod; i >= startTime - zeroTimeLength + zeroPeriod; i -= zeroPeriod)
            {
                newTimes.Add(i);
            }
            newTimes.Add(startTime - zeroTimeLength);
            newTimes.Reverse();

            // Add new times and zero values
            timeData.InsertRange(0, newTimes);
            strengthData.InsertRange(0, new double[newTimes.Count]);

            newTimes.Clear();

            // Find times to add after end of data:
            // - Should be one value for every zeroPeriod after endTime. Last value not included 
            //   if closer than one period to end of range. Always include value at end of range. 
            // - Ex: for Length = 10, Period = 3, endTime = 0 : 0,1,2,{3},4,5,{6},7,8,9,{10},11
            for (long i = endTime + zeroPeriod; i <= endTime + zeroTimeLength - zeroPeriod; i += zeroPeriod)
            {
                newTimes.Add(i);
            }
            newTimes.Add(endTime + zeroTimeLength);

            // Add new times and zero values
            timeData.AddRange(newTimes);
            strengthData.AddRange(new double[newTimes.Count]);

            // Return new data as a tuple of lists;
            return new Tuple<List<long>, List<double>>(timeData, strengthData);

        }

        /// <summary>
        /// Performs linear interpolation on dataset.
        /// </summary>
        /// <param name="timeData">Time data to be interpolated.</param>
        /// <param name="strengthData">Strength data to interpolated.</param>
        /// <param name="samplePeriod">Period of the desired sample rate after interpolation.</param>
        /// <returns>Interpolation of time and strength data.</returns>
        public static Tuple<List<long>, List<double>> LinearlyInterpolate(List<long> timeData, List<double> strengthData, uint samplePeriod)
        {
            // Find start and end times of original data
            long startTime = timeData.Min();
            long endTime = timeData.Max();

            // Get new time values;
            List<long> newTimes = new List<long>();
            for (long t = startTime; t < endTime; t += samplePeriod)
            {
                newTimes.Add(t);
            }

            var newStrengths = new List<double>(newTimes.Count);

            // Get interpolated strength values
            double s = 0;
            int i = 0;
            foreach (long time in newTimes)
            {
                // Search for time in data
                i = timeData.BinarySearch(time);
                if (i < 0) //Time not found. ~i is the next value.
                {
                    i = ~i;
                    // Linear interpolation
                    s = strengthData[i - 1] + (time - timeData[i - 1]) * (strengthData[i] - strengthData[i - 1]) / (timeData[i] - timeData[i - 1]);
                }
                else //Time found at i
                {
                    s = strengthData[i];
                }
                newStrengths.Add(s);
            }

            // Return interpolated data
            return new Tuple<List<long>, List<double>>(newTimes, newStrengths);
        }

        /// <summary>
        /// Take moving average of strength. Assumes strength values are spaced evenly.
        /// </summary>
        /// <param name="strengthData">Strength data to be averaged.</param>
        /// <param name="windowWidth">Width of the window over which to take the average. Measured in number of data points.</param>
        /// <returns>The mean strength values. Same length as input data.</returns>
        public static List<double> MovingMean(List<double> strengthData, int windowWidth)
        {
            // Takes average of a "window" around each point.
            // Width of the window is the number of points around each point to average over.
            // Averages data at the beginning and end by shortening window.

            int winRadius = windowWidth / 2;
            //$$ Could be more efficient if just an int here, instead of "(windowEven ? 1 : 0)" later.
            bool windowEven = windowWidth % 2 == 0; // to make even, don't include one point on high end of data

            var meanStrengths = new List<double>(strengthData.Count);

            double m = 0;
            for (int i = 0; i < strengthData.Count; i++)
            {
                m = Average(strengthData, i - winRadius, i + winRadius - (windowEven ? 1 : 0));
                meanStrengths.Add(m);
            }

            return meanStrengths;
        }

        /// <summary>
        /// Averages the values within the given indices in a collection. Does not include any indices outside of the bounds of the collection.
        /// </summary>
        /// <param name="data">Collection to take average from.</param>
        /// <param name="startIdx">Starting index of average.</param>
        /// <param name="endIdx">Ending index of average.</param>
        /// <returns>Average of the the values in the given indices.</returns>
        public static double Average(IEnumerable<double> data, int startIdx, int endIdx)
        {
            // Remove indices that are outside of the array
            startIdx = Math.Max(0, startIdx);
            endIdx = Math.Min(data.Count() - 1, endIdx);

            double sum = 0;
            int num = 0;
            for (int i = startIdx; i < endIdx + 1; i++)
            {
                sum += data.ElementAt(i);
                num++;
            }

            return sum / num;
        }

        //$? Maybe move to a utilities class.
        /// <summary>
        /// Finds relative maxima of the data.
        /// </summary>
        /// <param name="data">Data to find peaks of.</param>
        /// <returns>Returns the index of each peak.</returns>
        public static IList<int> FindRelativePeaks(IList<double> data)
        {
            //$? This method does not use LINQ. There may be a way to use yield return to make this better.

            // Go through list.
            // Compare value to previous and following.
            // If higher than previous and following, output index.

            // Needs at least 3 data points to work.
            if (data.Count < 3)
            {
                throw new ArgumentException("Insufficient data.", "data");
            }

            List<int> peaks = new List<int>();

            int lastIdx = data.Count - 1;

            //First element
            if (data[0] >= double.NegativeInfinity && data[0] > data[1])
            {
                //peak
                peaks.Add(0);
            }

            for (int i = 1; i < lastIdx; i++)
            {
                if (data[i] >= data[i - 1] && data[i] > data[i + 1])
                {
                    //peak
                    peaks.Add(i);
                }
            }

            //Final element
            if (data[lastIdx] >= double.NegativeInfinity && data[lastIdx] > data[lastIdx - 1])
            {
                //peak
                peaks.Add(lastIdx);
            }

            return peaks;
        }

        //$? These two filtering techniques may be easier in the original function for finding peaks.
        /// <summary>
        /// Filters the peaks to find only desired. Uses min peak value, min peak distance parameters.
        /// </summary>
        /// <param name="timeData">Time data used to filter peaks.</param>
        /// <param name="strengthData">Strength data to filter peaks for.</param>
        /// <param name="minPeak">Minimum strength value allowed for a peak. In same units as <paramref name="strengthData"/>.</param>
        /// <param name="minPeakDist">Minimum difference in time allowed between two peaks. In same units as <paramref name="timeData"/>.</param>
        /// <returns>List of peaks as Tuple of time and strength.</returns>
        public IList<Tuple<long, double>> FilterPeaks(IList<long> timeData, IList<double> strengthData, double minPeak, double minPeakDist)
        {
            // Finds all relative peaks before filtering
            var peaks = FindRelativePeaks(strengthData);

            List<long> peakTimes = new List<long>();
            List<double> peakStrengths = new List<double>();
            var pp = new List<Tuple<long, double>>();

            //1. Apply strength minimum threshold
            //Remove all peaks below the threshold.
            foreach (int peak in peaks)
            {
                if (strengthData[peak] >= minPeak)
                {
                    pp.Add(Tuple.Create(timeData[peak], strengthData[peak]));
                }
            }

            //2. Apply minimum peak separation threshold
            //Compare two peaks.
            //If peaks are closer than minimum distance, remove smaller peak.
            //Assume in order

            List<Tuple<long, double>> filteredPeaks = new List<Tuple<long, double>>();

            //Not very efficient; Looks at peaks within min distance of peaks that are already counted.
            //Efficiency may not matter as number of peaks should be very low.
            foreach (Tuple<long, double> p in pp.OrderByDescending((x) => x.Item2))
            {
                var peaksTooClose = pp.Where(x => Math.Abs(p.Item1 - x.Item1) < minPeakDist); //returns all peaks closer than min distance
                var val = peaksTooClose.Where(x => x.Item2.Equals(peaksTooClose.Max(y => y.Item2))).First(); //Should return the highest peak in the range.
                if (!filteredPeaks.Contains(val))
                    filteredPeaks.Add(val);
            }

            //Order by time.
            return filteredPeaks.OrderBy(x => x.Item1).ToList();
        }

        /// <summary>
        /// Pairs peaks of the data collection.
        /// </summary>
        /// <param name="tagPeaks">TagCollection containing tag peak values.</param>
        /// <returns>TagPeak object representing a pair of peaks, or null if failed to pair peaks.</returns>
        private TagPeak PairPeaks(TagCollection tagPeaks)
        {
            //Determine how many antennas read this tag and select the correct ones to process.

            //To generate a valid TagPeak, we need both antennas on a side to have a peak. 
            //If there is no full side of antenna peaks, it fails to create a TagPeak. 
            //If both sides have both antenna peaks, uses the side that has the higher peak strength.

            // Count the number of full sides.
            //if 0, fail.
            //if 1, succeeds.
            //if 2, determine strongest, succeeds.

            var antIds = tagPeaks.AntennaCollections.Select(x => x.AntennaId);

            int antSideCount = 0;

            // Check left side
            bool leftSideComplete = true;
            foreach (int ant in antArrangement.LeftSide)
            {
                if (!antIds.Contains(ant))
                {
                    leftSideComplete = false;
                    break;
                }
            }
            if (leftSideComplete) antSideCount++;

            // Check right side
            bool rightSideComplete = true;
            foreach (int ant in antArrangement.RightSide)
            {
                if (!antIds.Contains(ant))
                {
                    rightSideComplete = false;
                    break;
                }
            }
            if (rightSideComplete) antSideCount++;

            switch (antSideCount)
            {
                case 0:
                    //Not enough data.
                    return null;
                case 1:
                    //Succeeded.
                    break;
                case 2:
                    //Determine strongest side and send.

                    //Get average of left vs average of right. 
                    //Set weaker side to false.
                    if (tagPeaks.AntennaCollections.FindAll(x => antArrangement.LeftSide.Contains(x.AntennaId)).Select(y => y.DataList.Select(z => z.Item2).Max()).Average() >
                        tagPeaks.AntennaCollections.FindAll(x => antArrangement.RightSide.Contains(x.AntennaId)).Select(y => y.DataList.Select(z => z.Item2).Max()).Average())
                    {
                        rightSideComplete = false;
                    }
                    else
                    {
                        leftSideComplete = false;
                    }
                    break;
                default:
                    //Error; Should never be true.
                    throw new Exception("Invalid number of sides with full antenna readings. Num sides: " + antSideCount);
            }

            // Return if no sides are complete; $$May be excessive error checking.
            if (!leftSideComplete && !rightSideComplete) return null;

            VehicleSide vehicleSide = 0;
            IList<int> antIdList = null;

            if (leftSideComplete)
            {
                vehicleSide = VehicleSide.Left;
                antIdList = antArrangement.LeftSide;
            }
            else if (rightSideComplete)
            {
                vehicleSide = VehicleSide.Right;
                antIdList = antArrangement.RightSide;
            }
            else return null;

            List<int> peakIdList = new List<int>();
            List<Tuple<long, double>> peakList = new List<Tuple<long, double>>();
            foreach (int ant in antIdList)
            {
                var peak = tagPeaks.AntennaCollections.Where(x => x.AntennaId == ant).First().DataList.First();
                peakList.Add(peak);
                peakIdList.Add(ant);
            }

            var peaks = peakList.Take(2);
            var peakIds = peakIdList.Take(2);

            return new TagPeak(tagPeaks.TagId, vehicleSide, peaks.First(), peakIds.First(), peaks.Last(), peakIds.Last());
        }

    }

}
