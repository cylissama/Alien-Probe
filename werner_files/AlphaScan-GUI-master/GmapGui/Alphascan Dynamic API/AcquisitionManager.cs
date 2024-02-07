using System;
using System.ComponentModel; // for INotifyPropertyChanged interface
using System.Runtime.CompilerServices; // for [CallerMemberName] in NotifyPropertyChanged
using System.Threading.Tasks;
using CES.AlphaScan.Gps;
using System.Collections.Generic;
using CES.AlphaScan.mmWave;
using CES.AlphaScan.Rfid;
using CES.AlphaScan.CombinedSensors;
using CES.AlphaScan.Base;

namespace CES.AlphaScan.Acquisition
{
    /// <summary>
    /// Facilitates and controls data acquisition and processing. Exposes members necessary 
    /// for operation of the system.
    /// </summary>
    /// <remarks>
    /// Contains and controls objects that communicate with, control, and process data from 
    /// the individual sensors. Also combines the data from different sensors into a single 
    /// data type to output. Also reads saved settings from files and writes output data to 
    /// files.
    /// </remarks>
    public class AcquisitionManager : IAcquisitionManager, INotifyPropertyChanged, ILogMessage
    {
        /// <summary>
        /// Manager for the mmWave sensor.
        /// </summary>
        private mmWaveManager mmWaveManager = null;
        /// <summary>
        /// Manager for the GPS sensor.
        /// </summary>
        private GPSManager gpsManager = null;
        /// <summary>
        /// Manager for the RFID reader.
        /// </summary>
        private RfidManager rfidManager = null;

        /// <summary>
        /// Manager for the combination of data between different sensors.
        /// </summary>
        private CombinationManager combinationManager = null;

        /// <summary>
        /// Manages the saving of output data to files. Can handle data from different sources and different threads.
        /// </summary>
        private OutputManager outputManager = null;

        //Properties and methods to enable easy logging of messages through the ILogMessage interface.
        #region Logging

        /// <summary>
        /// Name of the all sensor manager, probably won't ever change
        /// </summary>
        public string Name { get; protected set; } = "AllSensorController";

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

        //Properties that notify of changes.
        //$$ Currently unused. Implement or remove.
        #region Property Notification
        /// <summary>
        /// Notifies higher levels of changed properties.
        /// </summary>
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

        #endregion

        /// <summary>
        /// Gets whether each sensor manager is processing or the sensor is reading.
        /// </summary>
        /// <returns></returns>
        public bool[] GetSensorsRunning()
        {
            return new bool[3] { this.mmWaveManager is null ? false : this.mmWaveManager.IsRunning || this.mmWaveManager.IsProcessing,
                this.gpsManager is null ? false : this.gpsManager.IsRunning || this.gpsManager.IsProcessing, 
                this.rfidManager is null ? false: this.rfidManager.IsRunning || this.rfidManager.IsProcessing };
        }

        /// <summary>
        /// Gets whether each combination manager loop is processing.
        /// </summary>
        /// <returns>A boolean for each loop. Each represent whether the loop is running.</returns>
        public bool[] GetProcessorsRunning()
        {
            return new bool[2] { combinationManager?.IsmmWaveGPSThreadProcessing ?? false,
                combinationManager?.IsClusterObjRFIDThreadProcessing ?? false };
        }

        //$? There is probably a better way to do this. Like property notification events for GUI layer.
        /// <summary>
        /// Gets tasks that complete when the combination manager threads finish.
        /// </summary>
        /// <param name="timeout">Timeout for any wait methods that require it. In milliseconds.</param>
        /// <returns>One awaitable task for each combination manager loop possible. Each completes
        /// with result true when the loop has stopped running. If the loop was not running, 
        /// returns a completed task with result true. Result is false if there is a timeout.</returns>
        public Task<bool>[] WaitForProcessorRunning(int timeout = -1)
        {
            return new Task<bool>[2] {combinationManager?.WaitmmWaveGpsCombinationFinish(timeout) ?? Task.FromResult(false),
            combinationManager?.WaitRfidObjectCombinationFinish(timeout) ?? Task.FromResult(false)};
        }

        //$? probably unneeded; otherwise make public properties and remove GetSensorsRunning().
        private bool isRfidRunning = false;
        private bool ismmWaveRunning = false;
        private bool isGpsRunning = false;

        //$$ find a better way of doing this
        private bool ismmWaveConfigured = false;
        private bool isGPSConfigured = false;
        private bool isRFIDConfigured = false;

        IDictionary<string, object> globalSettings = new Dictionary<string, object>();
        bool globalSettingsSet = false;

        //$? Could probably be improved. Used to determine if should start combination.
        /// <summary>
        /// List of which sensors are running.
        /// </summary>
        List<string> sensorsStarted = new List<string>();

        //$$ Unsure if this is needed.
        private bool isOutputManagerConfigured = false;

        /// <summary>
        /// Configures the output manager if not configured.
        /// </summary>
        /// <returns>Whether successfully configured output manager.</returns>
        private bool ConfigureOutputManager()
        {
            try
            {
                // If global settings have not been read, read from file.
                if (globalSettings == null || !globalSettingsSet)
                {
                    globalSettings = ReadConfig.SensorConfigReader("Global");
                    globalSettingsSet = true;
                }
                string saveDirectory = globalSettings["Save Directory"].ToString();

                // Create new output manager if not created. Ensure correct output directory.
                if (outputManager == null)
                {
                    outputManager = new OutputManager(saveDirectory);
                }
                else if (outputManager.OutputDirectory.FullName != saveDirectory)
                {
                    outputManager.ChangeOutputDirectory(saveDirectory);
                }

                // Save info file to output directory. //$? Seems to only be correct for StartAllSensors().
                var infoData = ReadConfig.SensorConfigReader("Info");
                outputManager.SaveInfoFile(infoData, sensorsStarted);

                return true;
            }
            catch (Exception e)
            {
                LogMessage("Failed to configure OutputManager. " + e.GetType().FullName + ": " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// starts each individual sensor with their respective settings, letter in front indicates what sensor settings
        /// </summary>
        public async Task StartAllSensors()
        {
            if (globalSettings == null || globalSettings.Count == 0)
            {
                globalSettings = ReadConfig.SensorConfigReader("Global");
                if (globalSettings == null)
                {
                    globalSettingsSet = false;
                    LogMessage("Failed to start. Global settings not found.");
                    return;
                }
                globalSettingsSet = true;
            }

            if (outputManager == null) outputManager = new OutputManager(globalSettings["Save Directory"].ToString());
            sensorsStarted.Clear();

            this.ConfigureAllSensors();

            outputManager.NextRun();
            ConfigureOutputManager();

            Task startmmWaveTask = new Task(() => this.StartmmWave());
            Task startGpsTask = new Task(() => this.StartGps());
            Task startRfidTask = new Task(() => this.StartRFID());

            Task startmmWaveGPSComboTask = new Task(() => combinationManager.CreatemmWaveGPSThread(outputManager, mmWaveManager, gpsManager));
            Task startClusterRFIDCombineTask = new Task(() => combinationManager.CreateClusterObjRFIDThread());

            Task[] startSensorTasks = new Task[3] { startmmWaveTask, startGpsTask, startRfidTask };

            if (ismmWaveConfigured)
            {
                try
                {
                    startmmWaveTask.Start(TaskScheduler.Default);    // start mmWave task
                }
                catch {
                }
            }
            else
            {
                // mmWave cannot start, delete the new run directory and return
                startmmWaveTask = Task.CompletedTask;
                outputManager.ErrorRun();
                LogMessage("mmWave not configured, removing new run");
                // do not need to stop all sensors here, mmWave is the first to boot
                return;
            }

            if (isGPSConfigured)
            {
                try
                {
                    startGpsTask.Start(TaskScheduler.Default);        // start GPS task
                }
                catch { }
            }
            else
            {
                // GPS cannot start, delete the new run directory and return
                startGpsTask = Task.CompletedTask;
                // mmWave should be the only sensor running, stop it
                await startmmWaveTask;
                await AbortAllSensors();
                outputManager.ErrorRun();
                LogMessage("GPS not configured, removing new run");
                return;
            }

            if (isRFIDConfigured)
            {
                try
                {
                    startRfidTask.Start(TaskScheduler.Default);    // start RFID task
                }
                catch { }
            }
            else
            {
                // RFID cannot start, delete the new run directory and return
                startRfidTask = Task.CompletedTask;
                // stop mmWave and GPS if not able to run
                await startmmWaveTask;
                await startGpsTask;
                await AbortAllSensors();
                outputManager.ErrorRun();
                LogMessage("RFID not configured, removing new run");
                return;
            }

            // Asynchronously wait for all sensors to start. //$? No longer has timeout. Is it needed?
            await Task.WhenAll(startSensorTasks);
           
            // this should fire if one of the sensors are not started, cannot start the run, delete new run
            if (!mmWaveManager.IsRunning || !gpsManager.IsRunning || !rfidManager.IsRunning)
            {
                // stop all sensors
                await StopAllSensors();
                outputManager.ErrorRun();
                LogMessage("A sensor failed to start, check the log for a failure mesage. Deleting the new run directory.");
            }

            // creates threads to run combination managers
            if (mmWaveManager.IsRunning && gpsManager.IsConnected)
            {
                combinationManager = new CombinationManager(this.rfidManager); //$$ Added to fix errors. What else needs to be passed as argument?
                combinationManager.MessageLogged += LogMessage;

                startmmWaveGPSComboTask.Start(TaskScheduler.Default);
                await startmmWaveGPSComboTask;

                combinationManager.updateTagLocationRecieved -= RaiseTagLocationUpdateEvent;
                combinationManager.updateTagLocationRecieved += RaiseTagLocationUpdateEvent;

                if (rfidManager.IsRunning && !combinationManager.IsClusterObjRFIDThreadProcessing)
                {
                    startClusterRFIDCombineTask.Start(TaskScheduler.Default);
                    await startClusterRFIDCombineTask;
                }
            }
            ismmWaveRunning = mmWaveManager.IsRunning;
            isGpsRunning = gpsManager.IsRunning;
            isRfidRunning = rfidManager.IsRunning;
        }

        /// <summary>
        /// Stops all sensors that are running and waits for all processing loops to finish.
        /// </summary>
        public async Task StopAllSensors()
        {
            Task stopmmWaveTask = new Task(async () => await this.StopmmWave());
            Task stopGpsTask = new Task(async () => await this.StopGPS());
            Task stopRfidTask = new Task(async () => await this.StopRFID());

            // check if sensor is running, stop sensor, and wait for each sensor to stop to prevent infinte loop
            if (mmWaveManager != null && (mmWaveManager.IsRunning || mmWaveManager.IsProcessing))
            {
                try
                {
                    stopmmWaveTask.Start(TaskScheduler.Default);
                }
                catch (Exception e)
                {
                    LogMessage("Failed to stop mmWave. " + e.GetType().FullName + ": " + e.Message);
                    stopmmWaveTask = Task.FromResult(false);
                }
            }
            else
            {
                // Already finished.
                stopmmWaveTask = Task.FromResult(true);
            }

            if (gpsManager != null && (gpsManager.IsConnected || gpsManager.IsProcessing))
            {
                try
                {
                    stopGpsTask.Start(TaskScheduler.Default);
                }
                catch (Exception e)
                {
                    LogMessage("Failed to stop GPS. " + e.GetType().FullName + ": " + e.Message);
                    stopGpsTask = Task.FromResult(false);
                }
            }
            else
            {
                // Already finished.
                stopGpsTask = Task.FromResult(true);
            }

            if (rfidManager != null && (rfidManager.IsRunning || rfidManager.IsProcessing))
            {
                try
                {
                    stopRfidTask.Start(TaskScheduler.Default);
                }
                catch (Exception e)
                {
                    LogMessage("Failed to stop RFID. " + e.GetType().FullName + ": " + e.Message);
                    stopRfidTask = Task.FromResult(false);
                }
            }
            else
            {
                // Already finished.
                System.Diagnostics.Debug.WriteLine("Before RFID");
                stopRfidTask = Task.FromResult(true);
                System.Diagnostics.Debug.WriteLine("After RFID");
            }

            // Asynchronously wait for all tasks to complete.
            await stopmmWaveTask;
            await stopGpsTask;
            await stopRfidTask;

            // Stop combination manager.
            if (combinationManager != null)
            {
                await combinationManager.StopAndProcess();
            }

            isOutputManagerConfigured = false;
        }

        /// <summary>
        /// Aborts all sensors and processing loops that are running. Not guaranteed to process all collected data.
        /// </summary>
        public async Task AbortAllSensors()
        {
            // Abort each sensor
            Task abortmmWaveTask = Task.Run(async () => { await this.AbortmmWave(); });
            Task abortGpsTask = Task.Run(async () => { await this.AbortGPS(); });
            Task abortRfidTask = Task.Run(async () => { await this.AbortRFID(); });

            await abortmmWaveTask;
            await abortGpsTask;
            await abortRfidTask;

            // Abort combination manager.
            if (combinationManager != null)
            {
                await combinationManager.Stop();
            }

            isOutputManagerConfigured = false;
        }

        /// <summary>
        /// Creates and sends settings to each sensor manager.
        /// </summary>
        public void ConfigureAllSensors()
        {
            try
            {
                ismmWaveConfigured = ((IAcquisitionManager)this).SetmmWaveSettings();
            }
            catch (Exception e)
            {
                LogMessage("Failed to set mmWave settings. " + e.GetType().FullName + ": " + e.Message);
                ismmWaveConfigured = false;
            }

            try
            {
                isGPSConfigured = ((IAcquisitionManager)this).SetGPSSettings();
            }
            catch (Exception e)
            {
                LogMessage("Failed to set GPS settings. " + e.GetType().FullName + ": " + e.Message);
                isGPSConfigured = false;
            }

            try
            {
                isRFIDConfigured = ((IAcquisitionManager)this).SetRFIDSettings();
            }
            catch (Exception e)
            {
                LogMessage("Failed to set RFID settings. " + e.GetType().FullName + ": " + e.Message);
                isRFIDConfigured = false;
            }
        }

        #region mmWave
        /// <summary>
        /// sets settings for the mmWave sensor
        /// </summary>
        bool IAcquisitionManager.SetmmWaveSettings()
        {
            // Create new mmWave manager if none exists.
            if (mmWaveManager != null)
            {
                // Stop mmWave manager.
                if (mmWaveManager.IsProcessing || mmWaveManager.IsRunning || mmWaveManager.IsConnected)
                {
                    if (!mmWaveManager.Abort().Result)
                        LogMessage("Failed to stop mmWave manager.");
                }
            }
            else
            {
                mmWaveManager = new mmWaveManager();
                mmWaveManager.MessageLogged += LogMessage;
            }

            IDictionary<string, object> mmWaveSettings = ReadConfig.SensorConfigReader("mmWave");

            if (globalSettings == null || globalSettings.Count == 0)
            {
                globalSettings = ReadConfig.SensorConfigReader("Global");
            }

            if (mmWaveSettings == null)
                return false;

            if (!mmWaveManager.SetSettings(mmWaveSettings, globalSettings, outputManager))
            {
                return false;
                
            }

            sensorsStarted.Add(mmWaveManager.Name);
            return true;
        }

        /// <summary>
        /// starts the mmWave sensor
        /// </summary>
        /// <returns>bool of whether the sensor started</returns>
        bool IAcquisitionManager.StartmmWave()
        {
            if (globalSettings == null || globalSettings.Count == 0)
            {
                globalSettings = ReadConfig.SensorConfigReader("Global");
                globalSettingsSet = true;
            }

            if (outputManager == null)
            {
                outputManager = new OutputManager(globalSettings["Save Directory"].ToString());
                outputManager.NextRun();
                ConfigureOutputManager();
            }
            //$? Should this clear all?
            sensorsStarted.Clear();

            try
            {
                ismmWaveConfigured = ((IAcquisitionManager)this).SetmmWaveSettings();
            }
            catch (Exception e)
            {
                ismmWaveConfigured = false;
                LogMessage("Failed to configure mmWave. " + e.GetType().FullName + ": " + e.Message);
            }

            return this.StartmmWave();
        }

        /// <summary>
        /// starts the mmWave sensor
        /// </summary>
        /// <returns>bool of whether the sensor started</returns>
        private bool StartmmWave()
        {
            try
            {
                mmWaveManager.Start();

                if (mmWaveManager.IsRunning)
                {
                    ismmWaveRunning = true;
                    sensorsStarted.Add("mmWave");
                    LogMessage("Started mmWave.");
                    return true;
                }
                else
                {
                    LogMessage("Failed to start mmWave: aborting mmWave.");
                    if (mmWaveManager.IsProcessing)
                        mmWaveManager.Abort().Wait();
                }
            }
            catch (Exception e)
            {
                LogMessage("Failed to start mmWave. " + e.GetType().FullName + ": " + e.Message);
                return false;
            }

            // do some error handle here
            LogMessage("Failed to Start mmWave.");
            return false;
        }

        /// <summary>
        /// Stops the mmWave manager. Stops the sensor and waits for processing loops to finish.
        /// </summary>
        /// <returns>bool of whether the sensor stopped</returns>
        public async Task<bool> StopmmWave()
        {
            try
            {
                if (await mmWaveManager.StopAndProcess())
                {
                    LogMessage("Stopped mmWave.");
                }
                
                try
                {
                    if (combinationManager != null)
                    {
                        //$$ What should happen?
                        if (combinationManager.IsmmWaveGPSThreadProcessing) ;
                    }
                    else
                    {
                        isOutputManagerConfigured = false;
                        outputManager = null;
                    }

                }
                catch
                {
                    isOutputManagerConfigured = false;
                    outputManager = null;
                }
                ismmWaveRunning = mmWaveManager.IsRunning;
                return true;
            }
            catch (Exception e)
            {
                // do some error handle here
                LogMessage("Failed to stop mmWave." + e.GetType().FullName + ": " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Stops the sensor and aborts data processing. Not guaranteed to 
        /// finish processing collected data.
        /// </summary>
        /// <returns>bool of whether the sensor stopped</returns>
        public async Task<bool> AbortmmWave()
        {
            try
            {
                if (await mmWaveManager.Abort())
                {
                    LogMessage("Stopped mmWave.");
                }

                ismmWaveRunning = mmWaveManager.IsRunning;
                return true;
            }
            catch (Exception e)
            {
                // do some error handle here
                LogMessage("Failed to abort mmWave." + e.GetType().FullName + ": " + e.Message);
                return false;
            }
        }
        #endregion

        #region GPS
        /// <summary>
        /// sets the GPS sensor settings
        /// </summary>
        /// <returns>bool of whether the GPS is configured</returns>
        bool IAcquisitionManager.SetGPSSettings()
        {
            if (gpsManager != null)
            {
                if (gpsManager.IsProcessing || gpsManager.IsConnected)
                {
                    gpsManager.Abort().Wait();
                }
            }
            else
            {
                gpsManager = new GPSManager();
                gpsManager.MessageLogged += LogMessage;
                gpsManager.AddDataToMap += RaiseGPSUpdateEvent;
            }

            IDictionary<string, object> GPSSettings = ReadConfig.SensorConfigReader("GPS");

            if (globalSettings == null || globalSettings.Count == 0)
            {
                globalSettings = ReadConfig.SensorConfigReader("Global");
            }

            if (GPSSettings == null)
                return false;

            if (gpsManager.SetSettings(GPSSettings, globalSettings, outputManager))
            {
                sensorsStarted.Add(gpsManager.Name);
                return true;
            }

            return false;
        }

        /// <summary>
        /// connects the GPS sensor to record data
        /// </summary>
        /// <returns>bool of whether the GPS sensor connected</returns>
        bool IAcquisitionManager.StartGPS()
        {
            if (globalSettings == null || globalSettings.Count == 0)
            {
                globalSettings = ReadConfig.SensorConfigReader("Global");
                globalSettingsSet = true;
            }

            if (outputManager == null)
            {
                outputManager = new OutputManager(globalSettings["Save Directory"].ToString());
                outputManager.NextRun();
                ConfigureOutputManager();
            }
            sensorsStarted.Clear();

            try
            {
                isGPSConfigured = ((IAcquisitionManager)this).SetGPSSettings();
            }
            catch (Exception e)
            {
                LogMessage("Failed to configure GPS. " + e.GetType().FullName + ": " + e.Message);
                isGPSConfigured = false;
            }

            return this.StartGps();
        }

        /// <summary>
        /// connects the GPS sensor to record data
        /// </summary>
        /// <returns>bool of whether the GPS sensor connected</returns>
        private bool StartGps()
        {
            try
            {
                // Start GPS if configured.
                if (isGPSConfigured)
                {
                    if (!gpsManager.Start())
                        return false;
                }
                else
                {
                    LogMessage("Failed to start GPS: manager not configured.");
                    return false;
                }

                LogMessage("GPS Connected.");

                if (gpsManager.IsConnected)
                {
                    sensorsStarted.Add("GPS");
                }
                else
                {
                    LogMessage("Failed to start GPS: aborting GPS.");
                    if (gpsManager.IsProcessing)
                        gpsManager.Abort().Wait();
                }

                isGpsRunning = gpsManager.IsRunning;
                return true;
            }
            catch (Exception e)
            {
                // do some error handle here
                LogMessage("Failed to connect GPS. " + e.GetType().FullName + ": " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// stops the GPS sensor from recording data
        /// </summary>
        /// <returns>bool of whether the GPS sensor stopped</returns>
        public async Task<bool> StopGPS()
        {
            try
            {
                if (gpsManager.IsConnected || gpsManager.IsProcessing)
                {
                    await gpsManager.StopAndProcess();
                }
                else
                {
                    LogMessage("Failed to Disconnect GPS: sensor not connected or processing.");
                    return false;
                }
                LogMessage("GPS Disconnected.");
                try
                {
                    if (combinationManager != null)
                    {
                        //$$ What should happen?
                        if (combinationManager.IsmmWaveGPSThreadProcessing) ;
                    }
                    else
                    {
                        isOutputManagerConfigured = false;
                        outputManager = null;
                    }
                }
                catch
                {
                    isOutputManagerConfigured = false;
                    outputManager = null;
                }
                isGpsRunning = gpsManager.IsRunning;
                return true;
            }
            catch (Exception e)
            {
                // do some error handle here
                LogMessage("Failed to disconnect GPS. " + e.GetType().FullName + ": " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Stops the sensor and aborts data processing. Not guaranteed to 
        /// finish processing collected data.
        /// </summary>
        /// <returns>bool of whether the sensor stopped</returns>
        public async Task<bool> AbortGPS()
        {
            try
            {
                if (await gpsManager.Abort())
                {
                    LogMessage("Aborted GPS.");
                }

                isGpsRunning = gpsManager.IsRunning;
                return true;
            }
            catch (Exception e)
            {
                // do some error handle here
                LogMessage("Failed to abort GPS." + e.GetType().FullName + ": " + e.Message);
                return false;
            }
        }
        #endregion

        #region RFID
        /// <summary>
        /// sets the RFID settings
        /// </summary>
        /// <returns></returns>
        bool IAcquisitionManager.SetRFIDSettings()
        {
            rfidManager = new RfidManager();
            rfidManager.MessageLogged += LogMessage;

            // Read configuration settings from file.
            IDictionary<string, object> RFIDSettings = ReadConfig.SensorConfigReader("RFID");

            // If there are no global settings, read from file.
            if (globalSettings == null || globalSettings.Count == 0)
            {
                globalSettings = ReadConfig.SensorConfigReader("Global");
            }

            // If required settings are missing, return false.
            if (RFIDSettings == null) return false;

            // create new output manager if one is not found
            if (outputManager == null)
            {
                outputManager = new OutputManager(globalSettings["Save Directory"].ToString());
                outputManager.NextRun();
                ConfigureOutputManager();
            }

            // Set settings in RfidManager.
            if (rfidManager.SetSensorSettings(RFIDSettings, globalSettings, outputManager))
            {
                //$$$ Hacky fix to make it reset reader settings every time. This decreases efficiency 
                //since it only needs to be run when settings are changed.
                if (rfidManager.ResetReaderSettings())
                {
                    //$$$ Idk if we need this every time we reset sensor settings. We need to figure this out.
                    //rfidManager.RebootReader().Wait(); 
                }
                else
                {
                    LogMessage("Failed to reset RFID settings.");
                    return false;
                }

                // Subscribe blacklist event if set to detect blacklisted tags.
                if (!bool.TryParse(RFIDSettings["DetectBlacklistTags"].ToString(), out bool detectBlacklist))
                {
                    LogMessage("Failed to parse setting: \"DetectBlacklistTags\"");
                }
                else
                {
                    if (detectBlacklist)
                    {
                        rfidManager.BlacklistTagDetected += RaiseBlacklistEvent;
                    }
                }
                
                sensorsStarted.Add(rfidManager.Name);
                return true;
            }

            return false;
        }

        /// <summary>
        /// starts the RFID reader
        /// </summary>
        /// <returns>bool of whether the RFID reader started</returns>
        bool IAcquisitionManager.StartRFID()
        {
            if (globalSettings == null || globalSettings.Count == 0)
            {
                globalSettings = ReadConfig.SensorConfigReader("Global");
                if (globalSettings == null || globalSettings.Count == 0)
                {
                    globalSettingsSet = false;
                    return false;
                }
                else
                {
                    globalSettingsSet = true;
                }
            }

            sensorsStarted.Clear();

            try
            {
                isRFIDConfigured = ((IAcquisitionManager)this).SetRFIDSettings();
            }
            catch (Exception e)
            {
                LogMessage("Failed to set RFID settings. " + e.GetType().FullName + ": " + e.Message);
                isRFIDConfigured = false;
            }

            if (outputManager == null)
            {
                try
                {
                    outputManager = new OutputManager(globalSettings["Save Directory"].ToString());
                    outputManager.NextRun();
                    ConfigureOutputManager();
                }
                catch (Exception e)
                {
                    LogMessage("Failed to start RFID: Failed to set up output manager. " + e.GetType().FullName + ": " + e.Message);
                    return false;
                }

            }

            return this.StartRFID();
        }

        /// <summary>
        /// starts the RFID reader
        /// </summary>
        /// <returns>bool of whether the RFID reader started</returns>
        private bool StartRFID()
        {
            try
            {
                // Start RFID if configured.
                if (isRFIDConfigured)
                {
                    rfidManager.Start();
                }
                else
                {
                    LogMessage("Failed to start RFID: manager not configured.");
                    return false;
                }

                // Check if RFID started successfully.
                if (rfidManager.IsRunning)
                {
                    LogMessage("RFID started.");
                    sensorsStarted.Add(rfidManager.Name);
                    isRfidRunning = rfidManager.IsRunning;
                    return true;
                }
                else
                {
                    LogMessage("Failed to start RFID: aborting RFID.");
                    if (rfidManager.IsProcessing)
                        rfidManager.Abort().Wait();
                }

                LogMessage("Failed to start RFID.");
                return false;
            }
            catch (Exception e)
            {
                // do some error handling
                LogMessage("Failed to start RFID. " + e.GetType().FullName + ": " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Stops the RFID reader and processing of RFID tag data. Note: May block thread.
        /// </summary>
        /// <returns>bool of whether the RFID reader stopped</returns>
        public async Task<bool> StopRFID()
        {
            try
            {
                // Stop RFID if set up.
                if (rfidManager != null && isRFIDConfigured)
                {
                    await rfidManager.StopAndProcess();
                }
                else
                {
                    LogMessage("Failed to stop RFID: RFID Manager not set up.");
                    return false;
                }

                // Check if RFID stopped
                if (rfidManager.IsRunning)
                {
                    LogMessage("Failed to stop RFID: Reader still running.");
                    return false;
                }
                else
                {
                    LogMessage("RFID Stopped.");
                }


                try
                {
                    if (combinationManager != null)
                    {
                        //$$ What should happen?
                        if (combinationManager.IsClusterObjRFIDThreadProcessing) ;
                    }
                    else
                    {
                        isOutputManagerConfigured = false;
                        outputManager = null;
                    }
                }
                catch
                {
                    isOutputManagerConfigured = false;
                    outputManager = null;
                }
                isRfidRunning = rfidManager.IsRunning;
                return true;
            }
            catch (Exception e)
            {
                // do some error handling
                LogMessage("Failed to stop RFID. " + e.GetType().FullName + ": " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Stops the sensor and aborts data processing. Not guaranteed to 
        /// finish processing collected data.
        /// </summary>
        /// <returns>bool of whether the sensor stopped</returns>
        public async Task<bool> AbortRFID()
        {
            try
            {
                if (await rfidManager.Abort())
                {
                    LogMessage("Aborted RFID.");
                }

                isRfidRunning = rfidManager.IsRunning;
                return true;
            }
            catch (Exception e)
            {
                // do some error handle here
                LogMessage("Failed to abort RFID." + e.GetType().FullName + ": " + e.Message);
                return false;
            }
        }
        #endregion

        #region Events

        //$$$ TESTING FOR BUBBLED EVENTS

        /// <summary>
        /// Event that notifies the GUI layer that a blacklisted RFID tag has been detected.
        /// </summary>
        public event EventHandler<TagData> BlacklistTagDetected;

        /// <summary>
        /// Raises the <see cref="BlacklistTagDetected"/> event with the specified arguments.
        /// </summary>
        /// <param name="sender">Object raising event.</param>
        /// <param name="tag">Blacklisted tag data point.</param>
        private void RaiseBlacklistEvent(object sender, TagData tag)
        {
            BlacklistTagDetected?.Invoke(sender, tag);
        }

        //$? Kinda hacky; maybe change.
        /// <summary>
        /// Queue of GPS points to add to the map to show the current vehicle location.
        /// </summary>
        public System.Collections.Concurrent.ConcurrentQueue<GpsData> GpsMapData { get { return gpsManager?.DataToAddToMap; } }

        /// <summary>
        /// Event telling the GUI to draw new GPS points to the map.
        /// </summary>
        public event Action UpdateGPSMap;

        /// <summary>
        /// Raises the <see cref="UpdateGPSMap"/> event.
        /// </summary>
        private void RaiseGPSUpdateEvent()
        {
            UpdateGPSMap?.Invoke();
        }

        
        /// <summary>
        /// Event telling the GUI to draw new TagLocation objects to the map.
        /// </summary>
        public event Action UpdateTagLocationMap;

        /// <summary>
        /// Raises the <see cref="UpdateTagLocationMap"/> event.
        /// </summary>
        private void RaiseTagLocationUpdateEvent()
        {
            UpdateTagLocationMap?.Invoke();
        }

        #endregion
    }
}
