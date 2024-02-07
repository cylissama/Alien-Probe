using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using CES.AlphaScan.Base;

namespace CES.AlphaScan.Acquisition
{
    public class OutputManager : IOutputManager
    {
        /// <summary>
        /// The root directory for all file saving.
        /// </summary>
        public DirectoryInfo OutputDirectory { get; private set; }

        /// <summary>
        /// Directory of the current run.
        /// </summary>
        public DirectoryInfo RunDirectory { get; private set; }

        //$? maybe not hardcode this
        /// <summary>
        /// String used as name for run folders.
        /// </summary>
        private readonly string RunName = "Run";

        private readonly object _createRunLock = new object();

        private CancellationTokenSource createRunCancel = new CancellationTokenSource();

        /// <summary>
        /// A count of the threads currently saving to files. Used to make 
        /// sure all files are closed before going to next run.
        /// </summary>
        private SafeCounter fileThreadCount = new SafeCounter();

        /// <summary>
        /// Cancellation token to stop all file saving when switching to next run.
        /// </summary>
        private CancellationTokenSource stopSaving = new CancellationTokenSource();

        /// <summary>
        /// Creates new <see cref="OutputManager"/> object. Base directory must be set before use.
        /// </summary>
        public OutputManager()
        {
        }

        /// <summary>
        /// Creates new <see cref="OutputManager"/> object based around the specified directory.
        /// </summary>
        /// <param name="outputDirectory">Directory to contain all run directories.</param>
        public OutputManager(string outputDirectory)
        {
            string dirName = outputDirectory;

            //$? Possibly add more flexibility or automation on creating base directory;

            OutputDirectory = new DirectoryInfo(dirName);

            if (!Directory.Exists(OutputDirectory.FullName))
            {
                Directory.CreateDirectory(dirName);
            }
        }

        /// <summary>
        /// String used as name (and file type) for info file.
        /// </summary>
        public string InfoFileName { get; } = "Info.txt";

        /// <summary>
        /// Whether info file has already been saved for this run.
        /// </summary>
        private bool infoFileSaved = false;

        /// <summary>
        /// Lock for info file. Prevents overwriting and multiple access errors.
        /// </summary>
        private readonly object _infoFileLock = new object();

        //$$ Add a semaphore so only one thread updates run at a time.
        /// <summary>
        /// Creates and sets up a new run folder. Sets the run number as the 
        /// lowest one not already used. If a valid run is currently selected, 
        /// attempts to use the next valid run number.
        /// </summary>
        /// <returns>Selected run number.</returns>
        public int NextRun()
        {
            var stopToken = createRunCancel.Token;
            lock (_createRunLock)
            {
                if (stopToken.IsCancellationRequested) return -1;

                int runNum = 0; //Default if no current run.
                if (RunDirectory != null)
                {
                    runNum = StringToEndInt(RunDirectory.Name) + 1; //Tries to set number as next run.
                }
                return NextRun(runNum);
            }
        }

        /// <summary>
        /// Creates and sets up a new run folder. Sets the run number as the 
        /// lowest one not already used starting from <paramref name="runNum"/>.
        /// </summary>
        /// <param name="runNum">Minimum run number to select.</param>
        /// <returns>Selected run number.</returns>
        public int NextRun(int runNum)
        {
            var stopToken = createRunCancel.Token;
            try
            {
                //$$ Idk how this line works. Investigate.
                if (Monitor.IsEntered(_createRunLock)) Monitor.Enter(_createRunLock);
                if (stopToken.IsCancellationRequested) return -1;

                var runNumbers = OutputDirectory.EnumerateDirectories().Select(dir => StringToEndInt(dir.Name));

                //$$ Could be more efficient using Linq Max().
                while (runNumbers.Contains(runNum))
                {
                    runNum++;
                }

                // Tell threads to stop saving.
                stopSaving.Cancel();

                lock (_infoFileLock)
                {
                    // Wait for threads to stop saving.
                    if (!fileThreadCount.WaitForZero().AsTask().Result)
                    {
                        //Bad. Counter failed to reach zero.
                        //Either threads failed to stop saving or counter is broken.
                        throw new TimeoutException("fileThreadCount counter failed to reach zero.");
                    }

                    RunDirectory = new DirectoryInfo(Path.Combine(OutputDirectory.FullName, RunName + runNum));

                    //Set up run
                    if (!Directory.Exists(RunDirectory.FullName))
                    {
                        Directory.CreateDirectory(RunDirectory.FullName);
                    }

                    infoFileSaved = false;
                }

                lock (_listLock)
                {
                    fileList.Clear();
                    //$$ Should we clear lock list?
                }

                //$$ may not be threadsafe.
                // Reset cancellation token.
                stopSaving = new CancellationTokenSource();

                return runNum;
            }
            finally
            {
                if (Monitor.IsEntered(_createRunLock)) Monitor.Exit(_createRunLock);
            }
        }

        /// <summary>
        /// Delete a new run directory if an error occurs
        /// </summary>
        /// <returns>Result of deletion</returns>
        public bool ErrorRun()
        {
            var stopToken = createRunCancel.Token;
            lock (_createRunLock)
            {
                try
                {
                    if (stopToken.IsCancellationRequested) return false;

                    int runNum = 0; //Default if no current run.
                    if (RunDirectory != null)
                    {
                        runNum = StringToEndInt(RunDirectory.Name); // Get current run number
                    }

                    string directoryPath = Path.Combine(OutputDirectory.ToString(), "Run" + runNum.ToString()); // get directory of file, will be the newest
                    string[] files = Directory.GetFiles(directoryPath); // get all files within the directory
                    // a directory CANNOT be deleted if there are files within, delete all the files within the target directory
                    foreach (string file in files)
                    {
                        using (FileStream stream = File.Open(file, FileMode.Open, FileAccess.Read))
                            stream.Close();

                         File.Delete(file);
                    }
                    // delete directory
                    Directory.Delete(directoryPath, false);
                    // successful deletion
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Reads a sequence of digits on the end of a string and returns them as an int. Ex: "Run123" returns int "123".
        /// </summary>
        /// <param name="s">A string with a sequence of digits on the end.</param>
        /// <returns>The number on the end of the string or 0 if failed to parse.</returns>
        public static int StringToEndInt(string s)
        {
            var charStack = new Stack<char>();

            foreach (char c in s.Reverse())
            {
                if (!char.IsDigit(c))
                {
                    break;
                }
                charStack.Push(c);
            }

            var numstr = new string(charStack.ToArray());

            int.TryParse(numstr, out int num);

            return num;
        }

        private Dictionary<string, FileInfo> fileList = new Dictionary<string, FileInfo>();
        private Dictionary<string, object> fileLockList = new Dictionary<string, object>();
        private readonly object _listLock = new object();

        //$$? this is a bit of a mess. Using channels/queues and a single filesaving thread could be easier.
        /// <summary>
        /// Writes collection of data objects to a csv file. Handles selection and creation of csv files. 
        /// Uses preexisting file if it exists, creates new file if it doesn't. "Thread-safe".
        /// </summary>
        /// <param name="fileName">Name of the file to save the data to. Creates it if it does not exist. 
        /// (Includes file extension. Ex: "dataFile.csv", not "dataFile")</param>
        /// <param name="dataList"></param>
        /// <returns>Whether data was successfully saved or not.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="fileName"/> is invalid.</exception>
        /// <exception cref="OperationCanceledException">Thrown if save operation is cancelled.</exception>
        public bool TrySaveData(string fileName, IEnumerable<ICsvWritable> dataList)
        {
            if (!CheckFileNameValid(fileName)) throw new ArgumentException("File name was invalid or null.", nameof(fileName));
            fileName = MakeExtensionMatch(fileName, "csv");

            if (dataList == null || dataList.Count() < 1) return false;

            bool listLockEntered = false;
            bool fileLockEntered = false;
            object fileLock = null;
            CancellationToken stopSavingToken = stopSaving.Token;

            //Return if cancellation requested.
            if (stopSavingToken.IsCancellationRequested) throw new OperationCanceledException(stopSavingToken);

            // Possible issue if stopSaving.Cancel() and WaitForZero() called in this gap.
            // Resolved by adding cancellation token check after increment.

            //Increment thread count
            fileThreadCount.Increment();
            try
            {
                while (true)
                {
                    if (stopSavingToken.IsCancellationRequested) throw new OperationCanceledException(stopSavingToken);

                    Monitor.Enter(_listLock, ref listLockEntered);
                    //Check for file lock and enter
                    if (!fileLockList.ContainsKey(fileName))
                    {
                        //No lock exists; Add lock and enter
                        fileLock = new object();
                        fileLockList.Add(fileName, fileLock);
                        Monitor.Enter(fileLock, ref fileLockEntered);
                        break;
                    }
                    else if (fileLockList[fileName] == null)
                    {
                        //Lock is null; Update lock and enter
                        fileLock = new object();
                        fileLockList[fileName] = fileLock;
                        Monitor.Enter(fileLock, ref fileLockEntered);
                        break;
                    }
                    else
                    {
                        //Lock exists; Wait to enter lock
                        fileLock = fileLockList[fileName];

                        Monitor.TryEnter(fileLock, ref fileLockEntered);
                        if (fileLockEntered)
                        {
                            break;
                        }
                        else
                        {
                            Monitor.Exit(_listLock);
                            listLockEntered = false;
                            if (stopSavingToken.IsCancellationRequested) throw new OperationCanceledException(stopSavingToken);

                            //$? Maybe sleep
                        }
                    }
                }


                //list locked
                //file locked

                //Check for fileinfo.
                if (!fileList.ContainsKey(fileName))
                {
                    //Create new fileinfo from filename.
                    fileList.Add(fileName, new FileInfo(Path.Combine(RunDirectory.FullName, fileName)));
                }
                else if (fileList[fileName] == null)
                {
                    //Replace fileinfo with new fileinfo from filename.
                    fileList[fileName] = new FileInfo(Path.Combine(RunDirectory.FullName, fileName));
                }

                //Check file exists.
                if (!File.Exists(fileList[fileName].FullName) && dataList.First() != null)
                {
                    // File doesn't exist; Create file from fileinfo name.
                    using (var fs = new StreamWriter(File.Create(fileList[fileName].FullName)))
                    {
                        // Add header to file
                        fs.WriteLine(dataList.First().CsvHeader);
                    }
                    if (!File.Exists(fileList[fileName].FullName))
                    {
                        return false;
                    }
                }

                string path = fileList[fileName].FullName;

                Monitor.Exit(_listLock);
                listLockEntered = false;

                //write data to file
                if (!TryAppendToFile(path, dataList)) return false;

                Monitor.Exit(fileLock);
                fileLockEntered = false;
                return true;
            }
            finally
            {
                //$? Maybe try and if is overkill. Monitor.Exit throws exception if not entered.
                try
                {
                    if (Monitor.IsEntered(_listLock))
                    {
                        Monitor.Exit(_listLock);
                    }
                }
                catch { }
                try
                {
                    if (fileLock != null && Monitor.IsEntered(fileLock))
                    {
                        Monitor.Exit(fileLock);
                    }
                }
                catch { }

                fileThreadCount.Decrement();
            }
        }

        /// <summary>
        /// Writes data to an alternate file type. Handles selection and creation of files. 
        /// Uses preexisting file if it exists, creates new file if it doesn't. "Thread-safe".
        /// </summary>
        /// <param name="fileName">Name of the file to save the data to. Creates it if it does not exist. 
        /// (Includes file extension. Ex: "dataFile.bin", not "dataFile") File name cannot be the same as 
        /// the <see cref="InfoFileName"/>.</param>
        /// <param name="data">Object representing the data to save.</param>
        /// <param name="saveFunction">Function delegate that saves data in alternative format. Takes object 
        /// representing data, string representing path of file to save to, and bool of if it is the first 
        /// time writing to the file. Returns whether successfully saved the data.</param>
        /// <returns>Whether data was successfully saved or not.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="fileName"/> is invalid.</exception>
        /// <exception cref="OperationCanceledException">Thrown if save operation is cancelled.</exception>
        public bool TrySaveData(string fileName, object data, Func<string, object, bool, bool> saveFunction)
        {
            if (!CheckFileNameValid(fileName)) throw new ArgumentException("File name was invalid or null.", nameof(fileName));
            if (fileName.Equals(InfoFileName)) throw new ArgumentException("File name was the same as the info file.", nameof(fileName));
            if (data == null) return false;

            bool listLockEntered = false;
            bool fileLockEntered = false;
            object fileLock = null;
            CancellationToken stopSavingToken = stopSaving.Token;

            //Return if cancellation requested.
            if (stopSavingToken.IsCancellationRequested) throw new OperationCanceledException(stopSavingToken);

            // Possible issue if stopSaving.Cancel() and WaitForZero() called in this gap.
            // Resolved by adding cancellation token check after increment.

            //Increment thread count
            fileThreadCount.Increment();
            try
            {
                while (true)
                {
                    if (stopSavingToken.IsCancellationRequested) throw new OperationCanceledException(stopSavingToken);

                    Monitor.Enter(_listLock, ref listLockEntered);
                    //Check for file lock and enter
                    if (!fileLockList.ContainsKey(fileName))
                    {
                        //No lock exists; Add lock and enter
                        fileLock = new object();
                        fileLockList.Add(fileName, fileLock);
                        Monitor.Enter(fileLock, ref fileLockEntered);
                        break;
                    }
                    else if (fileLockList[fileName] == null)
                    {
                        //Lock is null; Update lock and enter
                        fileLock = new object();
                        fileLockList[fileName] = fileLock;
                        Monitor.Enter(fileLock, ref fileLockEntered);
                        break;
                    }
                    else
                    {
                        //Lock exists; Wait to enter lock
                        fileLock = fileLockList[fileName];

                        Monitor.TryEnter(fileLock, ref fileLockEntered);
                        if (fileLockEntered)
                        {
                            break;
                        }
                        else
                        {
                            Monitor.Exit(_listLock);
                            listLockEntered = false;
                            if (stopSavingToken.IsCancellationRequested) throw new OperationCanceledException(stopSavingToken);

                            //$? Maybe sleep
                        }
                    }
                }


                //list locked
                //file locked
                bool fileNew = false;

                //Check for fileinfo.
                if (!fileList.ContainsKey(fileName))
                {
                    //Create new fileinfo from filename.
                    fileList.Add(fileName, new FileInfo(Path.Combine(RunDirectory.FullName, fileName)));
                }
                else if (fileList[fileName] == null)
                {
                    //Replace fileinfo with new fileinfo from filename.
                    fileList[fileName] = new FileInfo(Path.Combine(RunDirectory.FullName, fileName));
                }

                //Check file exists.
                if (!File.Exists(fileList[fileName].FullName))
                {
                    // File doesn't exist; Create file from fileinfo name.
                    File.Create(fileList[fileName].FullName).Dispose();
                    fileNew = true;

                    if (!File.Exists(fileList[fileName].FullName))
                    {
                        return false;
                    }
                }

                string path = fileList[fileName].FullName;

                Monitor.Exit(_listLock);
                listLockEntered = false;

                //write data to file
                if (!saveFunction(path, data, fileNew)) return false;

                Monitor.Exit(fileLock);
                fileLockEntered = false;
                return true;
            }
            finally
            {
                //$? Maybe try and if is overkill. Monitor.Exit throws exception if not entered.
                try
                {
                    if (Monitor.IsEntered(_listLock))
                    {
                        Monitor.Exit(_listLock);
                    }
                }
                catch { }
                try
                {
                    if (fileLock != null && Monitor.IsEntered(fileLock))
                    {
                        Monitor.Exit(fileLock);
                    }
                }
                catch { }

                fileThreadCount.Decrement();
            }
        }

        /// <summary>
        /// Takes and collection of <see cref="ICsvWritable"/> objects and outputs a list of csv line strings.
        /// </summary>
        /// <param name="dataList">Collection of data objects to write as csv text.</param>
        /// <returns>List of csv line strings representing data in <paramref name="dataList"/>.</returns>
        public static IEnumerable<string> PrintCsvList(IEnumerable<ICsvWritable> dataList)
        {
            foreach (ICsvWritable d in dataList)
            {
                if (d == null)
                    continue;
                yield return d.ToCsvString();
            }
        }

        /// <summary>
        /// Simple check for invalid characters in a potential file name.
        /// </summary>
        /// <param name="fileName">File name to check for validity.</param>
        /// <returns>Whether file name is potentially valid.</returns>
        public static bool CheckFileNameValid(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                if (fileName.Contains(c)) return false;
            }
            return true;
        }

        /// <summary>
        /// Simple check for invalid characters in a potential path name.
        /// </summary>
        /// <param name="path">Path name to check for validity.</param>
        /// <returns>Whether file name is potentially valid.</returns>
        public static bool CheckPathValid(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            foreach (char c in Path.GetInvalidPathChars())
            {
                if (path.Contains(c)) return false;
            }
            return true;
        }

        /// <summary>
        /// Checks if extension of <paramref name="fileName"/> matches <paramref name="extension"/>, and if 
        /// it doesn't, adds <paramref name="extension"/> to the end.
        /// </summary>
        /// <param name="fileName">File name to test extension.</param>
        /// <param name="extension">Extension to make file name match to.</param>
        /// <returns>File name with specified extension.</returns>
        public static string MakeExtensionMatch(string fileName, string extension = "csv")
        {
            string ext = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(ext) || ext.ToLowerInvariant() != extension)
            {
                return fileName + '.' + extension;
            }
            return Path.ChangeExtension(fileName, extension); //This might stop errors from upper/lower case extensions.
        }

        /// <summary>
        /// Attempts to save a collection of <see cref="ICsvWritable"/> data to a file. If 
        /// <see cref="IOException"/> occurs, retries saving until timeout or successfully saved.
        /// </summary>
        /// <param name="path">Path of the file to save.</param>
        /// <param name="dataList">Collection of <see cref="ICsvWritable"/> data to save to a file.</param>
        /// <param name="timeout">How long in milliseconds to continue trying to save if error occurs.</param>
        /// <returns>Whether data was successfully saved to file.</returns>
        private bool TryAppendToFile(string path, IEnumerable<ICsvWritable> dataList, int timeout = 1000)
        {
            IEnumerable<string> lines = PrintCsvList(dataList).ToList();
            
            bool saveSuccess = false;
            CancellationTokenSource stopTrying = new CancellationTokenSource();
            System.Threading.Timer timer = new Timer((_) => stopTrying.Cancel(), null, timeout, Timeout.Infinite);

            while (!saveSuccess)
            {
                try
                {
                    File.AppendAllLines(path, lines);
                    saveSuccess = true;
                }
                catch (IOException)
                {
                    saveSuccess = false;
                }

                if (stopTrying.IsCancellationRequested) break;
            }

            timer.Dispose();
            return saveSuccess;
        }

        /// <summary>
        /// Sets the info data to be saved to the info file in the next run.
        /// </summary>
        /// <param name="infoData">List of names and values of info data.</param>
        /// <returns>Whether successful in saving info file. False if file already saved or file failed to save.</returns>
        public bool SaveInfoFile(IDictionary<string, object> infoData)
        {
            bool infoFileEntered = false;

            try
            {
                Monitor.TryEnter(_infoFileLock, ref infoFileEntered);
                if (stopSaving.IsCancellationRequested) return false;
                if (!infoFileEntered || infoFileSaved) return false;
                if (RunDirectory == null || !Directory.Exists(RunDirectory.FullName)) return false;

                if (infoData == null || infoData.Count < 1)
                {
                    infoData = new Dictionary<string, object>() { { "Error", "No info data given." } };
                }

                DateTime time = DateTime.UtcNow;
                infoData.Add("Date", time.ToLongDateString());
                infoData.Add("Time (UTC)", time.TimeOfDay.ToString());

                string infoFilePath = Path.Combine(RunDirectory.FullName, InfoFileName);

                using (StreamWriter writer = new StreamWriter(File.Create(infoFilePath)))
                {
                    foreach (KeyValuePair<string, object> item in infoData)
                    {
                        writer.WriteLine(item.Key.ToString() + "=" + item.Value.ToString());
                    }
                }

                infoFileSaved = true;
                return true;
            }
            finally
            {
                if (Monitor.IsEntered(_infoFileLock))
                {
                    Monitor.Exit(_infoFileLock);
                }
            }
        }

        /// <summary>
        /// Sets the info data to be saved to the info file in the next run. Also adds the list of sensors 
        /// running to the info data.
        /// </summary>
        /// <param name="infoData">List of names and values of info data.</param>
        /// <param name="runningSensors">List of the names of each sensor running for the next run.</param>
        /// <returns>Whether successful in saving info file. False if file already saved or file failed to save.</returns>
        public bool SaveInfoFile(IDictionary<string, object> infoData, List<string> runningSensors)
        {

            string runningSensorsName = "Running Sensors";

            if (infoData == null || infoData.Count < 1)
            {
                infoData = new Dictionary<string, object>() { { "Error", "No info data given." } };
            }

            if (runningSensors != null && !infoData.ContainsKey(runningSensorsName))
            {
                infoData.Add(runningSensorsName, string.Join(",", runningSensors));
            }

            return SaveInfoFile(infoData);
        }

        //$$ Document exceptions.
        //$$ Add semaphore so NextRun() and ChangeOutputDirectory() can't run at the same time.
        /// <summary>
        /// Changes the directory that this <see cref="OutputManager"/> saves data to.
        /// </summary>
        /// <param name="outDirectory"></param>
        public void ChangeOutputDirectory(string outDirectory)
        {
            if (outDirectory == null) throw new ArgumentNullException(nameof(outDirectory));
            if (Path.GetFullPath(OutputDirectory.FullName) == Path.GetFullPath(outDirectory)) return;
            if (!CheckPathValid(outDirectory)) throw new ArgumentException("Output directory invalid path name.", nameof(outDirectory));

            createRunCancel.Cancel();
            
            // Tell threads to stop saving.
            stopSaving.Cancel();

            // Wait for threads to stop saving.
            if (!fileThreadCount.WaitForZero().AsTask().Result)
            {
                //Bad. Counter failed to reach zero.
                //Either threads failed to stop saving or counter is broken.
                throw new TimeoutException("fileThreadCount counter failed to reach zero.");
            }

            lock (_createRunLock)
            {
                OutputDirectory = new DirectoryInfo(outDirectory);

                //Set up output directory
                if (!Directory.Exists(OutputDirectory.FullName))
                {
                    Directory.CreateDirectory(OutputDirectory.FullName);
                }

                RunDirectory = null;

                createRunCancel = new CancellationTokenSource();
            }
            
            lock (_listLock)
            {
                fileList.Clear();
                //$$ Should we clear lock list? -> probably yes for new out directory. maybe not for new run.
                fileLockList.Clear();
            }

        }
    }
}
