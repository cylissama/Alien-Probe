using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CES.AlphaScan.Base;
using nsAlienRFID2;
using System.Threading;
using System.ComponentModel; // for INotifyPropertyChanged interface
using System.Runtime.CompilerServices; // for [CallerMemberName] in NotifyPropertyChanged


namespace CES.AlphaScan.Rfid
{
    /// <summary>
    /// Connects to and communicates with an RFID reader.
    /// </summary>
    public class RfidReaderModule : IRfidSensorModule, ILogMessage
    {
        /*
        The RfidReaderModule connects to and communicates with the RFID reader.

        This uses API provided by Alien in the nsAlienRFID2 namespace. The reader object is 
        used to connect to the RFID reader, change settings on it, and get the value of 
        settings. The server object is used to listen for the messages from the reader. We 
        subscribe to the ServerMessageReceived event with a method in the RfidInputParser 
        class. It is not handled in this class.

        We connect to the reader over ethernet. This requires set up in the settings of the 
        computer before trying to run this code. After connecting to the reader, we need to 
        log in to the reader. We do this with a saved username and password. This is not 
        secure as it is not currently intended to be on a network that can be accessed by 
        unauthorized individuals. Once we are connected, we can get and set settings, send 
        messages to the reader, and start/stop the reader.

        To start the reader, we switch the automode setting to true. This makes it so the 
        reader will cycle through reading tags and sending the read data as messages. To 
        stop the reader, we set this setting to false. There are also some settings that we 
        need to set each time the reader starts.
        //*/

        /// <summary>
        /// Username to access the reader.
        /// </summary>
        private string _username;
        /// <summary>
        /// Password to access the reader.
        /// </summary>
        private string _password;

        /// <summary>
        /// Reader object from the Alien API. Allows communication with the reader.
        /// </summary>
        private clsReader reader;
        /// <summary>
        /// Server object from the Alien API. Allows listening to messages sent from reader.
        /// </summary>
        private CAlienServer server;
        /// <summary>
        /// Object for parsing reader messages into tag data objects./>
        /// </summary>
        private RfidInputParser parser;

        //Properties that notify of changes.
        #region Property Notification

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

        public string Name { get; protected set; } = "RFIDReaderModule";
        public object Tag { get; protected set; }

        /// <summary>
        /// Whether or not the reader is currently connected.
        /// </summary>
        private bool _isConnected = false;
        public bool IsConnected
        {
            get
            {
                return _isConnected;
            }
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Whether or not the reader is currently running.
        /// </summary>
        private bool _isRunning = false;
        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }
            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    NotifyPropertyChanged();
                }
            }
        }

        #endregion

        //$$? See if there is another way to handle the InputParser without a direct reference. Delegate maybe.
        /// <summary>
        /// Constructs a new <see cref="RfidReaderModule"/> object that connects with a RFID 
        /// reader using the default IP addresses.
        /// </summary>
        /// <param name="parser_">Input parser to handle the tag data messages received.</param>
        /// <param name="username">Username to use to log in to the reader.</param>
        /// <param name="password">Password to use to log in to the reader.</param>
        public RfidReaderModule(RfidInputParser parser_, string username, string password)
        {
            _username = username;
            _password = password;

            Initialize(parser_, "192.168.217.52", 23, "192.168.217.51");
        }

        /// <summary>
        /// Constructs a new <see cref="RfidReaderModule"/> object that connects with a RFID 
        /// reader using the specified IP addresses.
        /// </summary>
        /// <param name="parser_">Input parser to handle the tag data messages received.</param>
        /// <param name="readerIP">IP address of RFID Reader.</param>
        /// <param name="port">Port with which to connect to Reader.</param>
        /// <param name="serverIP">IP address of this computer.</param>
        /// <param name="username">Username to use to log in to the reader.</param>
        /// <param name="password">Password to use to log in to the reader.</param>
        public RfidReaderModule(RfidInputParser parser_, string readerIP, int port, string serverIP, string username, string password)
        {
            _username = username;
            _password = password;

            Initialize(parser_, readerIP, port, serverIP);
        }

        /// <summary>
        /// Initialises a new ReaderManager object.
        /// </summary>
        /// <param name="readerIP">IP address of RFID Reader.</param>
        /// <param name="port">Port with which to connect to Reader.</param>
        /// <param name="serverIP">IP address of this computer.</param>
        private void Initialize(RfidInputParser parser_, string readerIP, int port, string serverIP)
        {
            parser = parser_;

            reader = new clsReader(readerIP, port);
            reader.Disconnected += Reader_Disconnected;
            server = new CAlienServer(port, serverIP);

            //Sends message received from reader to higher levels.
            reader.MessageReceived += LogMessage;
        }

        #region Interface

        /// <summary>
        /// Opens LAN connection with the reader.
        /// </summary>
        /// <returns>Returns true if opens successfully.</returns>
        public bool Connect()
        {
            //return false;

            // If reader is connected, returns.
            if (reader.IsConnected) return IsConnected = true;

            // Else tries to connect. Returns if failed.
            LogMessage(reader.Connect());

            if (!reader.IsConnected) return IsConnected = false;

            if (reader.Login(_username, _password))
            {
                LogMessage("Login Successful");

                // Updates the state of the reader in this class.
                IsConnected = true;
                IsRunning = reader.AutoMode == "ON";

                // Resets the automode settings. **Must reset RSSIFilter each time for RSSI data to be sent.
                ResetAutoModeSettings();

                return true;
            }
            else
            {
                reader.Disconnect();

                LogMessage("Login Failed");
                IsConnected = false;
                return false;
            }
        }

        /// <summary>
        /// Closes connection with RFID reader.
        /// </summary>
        /// <returns>Returns true if closes successfully.</returns>
        public bool Disconnect()
        {
            if (!reader.IsConnected) return true;

            reader.SendReceive("q", true);
            if (server.IsListening)
            {
                server.StopListening();
            }
            reader.Disconnect();
            IsConnected = false;

            return true;
        }

        /// <summary>
        /// Called when reader is disconnected.
        /// </summary>
        /// <param name="disconnectData"></param>
        private void Reader_Disconnected(string disconnectData)
        {
            LogMessage("Reader_Disconnected::" + disconnectData);
            IsConnected = false;
        }

        /// <summary>
        /// Starts reader AutoMode and starts server listening.
        /// </summary>
        /// <returns>Returns false if reader cannot connect.</returns>
        public bool Start()
        {
            if (!reader.IsConnected)
            {
                if (!Connect()) return false;
            }

            reader.AutoMode = "ON";
            server.StartListening();
            server.ServerMessageReceived += parser.Server_MessageReceived;

            IsRunning = reader.AutoMode == "ON";
            return true;
        }

        /// <summary>
        /// Stops reader AutoMode and stops server listening.
        /// </summary>
        /// <returns>Returns false if reader cannot connect.</returns>
        public bool Stop()
        {
            if (!Connect()) return false;

            reader.AutoMode = "OFF";
            if (server.IsListening) server.StopListening();
            server.ServerMessageReceived -= parser.Server_MessageReceived;

            IsRunning = reader.AutoMode == "ON";
            return true;
        }

        /// <summary>
        /// Sends a message to the RFID reader if possible. Response handled elsewhere.
        /// </summary>
        /// <param name="message">Message to send to RFID reader.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null or empty.</exception>
        public void SendMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) throw new ArgumentNullException(nameof(message));

            if (!IsConnected)
            {
                if (!Connect()) return;
            }

            reader.Send(message, false);
        }

        #endregion

        #region Logging
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

        #region Settings/Config

        //$$? Is this even needed?
        private IList<string> requiredSettings = new List<string>() { "IPAddress", "Netmask", "CommandPort", "AntennaSequence",
            "RFModulation", "PersistTime", "TagListAntennaCombine", "TagStreamCustomFormat", "AcquireMode",
            "AcqG2Session", "AcqG2AntennaCombine", "RSSIFilter", "AutoStopTimer"};

        private object _settingsLock = new object();
        private IDictionary<string, string> readerSettings;

        public void SetSettings(IDictionary<string, string> newSettings)
        {
            if (newSettings == null || newSettings.Count < 1)
            {
                //No settings received
                throw new ArgumentNullException(nameof(newSettings), "No settings were sent to " + nameof(RfidReaderModule));
            }

            if (this.IsRunning)
            {
                //Fail when system running
                throw new Exception("Cannot change settings while system is running.");
            }

            lock (_settingsLock)
            {
                if (CheckRequiredSettings(newSettings))
                {
                    readerSettings = new Dictionary<string, string>(newSettings);
                }
            }
        }

        /// <summary>
        /// Checks to see if new settings list contains all necessary settings.
        /// </summary>
        /// <param name="newSettings"></param>
        /// <returns></returns>
        private bool CheckRequiredSettings(IDictionary<string, string> newSettings)
        {
            bool allPresent = true;
            List<string> missingSettings = new List<string>();
            foreach (string req in requiredSettings)
            {
                if (!newSettings.ContainsKey(req))
                {
                    missingSettings.Add(req);
                    allPresent = false;
                }
            }

            if (!allPresent)
            {
                LogMessage("Missing required settings: " + string.Join(", ", missingSettings) + ". Please update settings file and run again.");
            }

            return allPresent;
        }

        /// <summary>
        /// Checks if the settings set on the reader match the settings sent to the reader module.
        /// </summary>
        /// <param name="newSettings">Settings to compare against the saved reader settings.</param>
        /// <returns>Whether settings saved on the reader match the given settings.</returns>
        private bool CheckSettingsCorrect(IDictionary<string, string> newSettings)
        {
            var correct = new Dictionary<string, bool>(newSettings.Count);

            foreach (KeyValuePair<string, string> kvp in newSettings)
            {
                string s = reader.SendReceive(kvp.Key + "?", true);
                s = s.Split('=').Last().TrimStart(' ');
                correct[kvp.Key] = kvp.Value.ToLower().Equals(s.ToLower());
            }

            if (!correct.Values.Any(b => b))
            {
                string notMatchMessage = "The following settings were not set correctly on the reader: ";
                foreach (KeyValuePair<string, bool> kvp in correct.Where(b => !b.Value))
                {
                    notMatchMessage += kvp.Key + ", ";
                }
                LogMessage(notMatchMessage + "; User should reset RFID reader settings.");
                return false;
            }
            return true;
        }

        //$$ Ideas for config
        // settings stored as Dictionary<string,string>
        // update with dictionary, or single string pair.
        // if not found property, throw exception
        // else update property with new value: both in dict and on reader or wherever is needed

        // notifypropertychanged when property updated
        // also allow viewing current config list

        /// <summary>
        /// Resets certain reader settings required for running Automode. Reset every time you run.
        /// </summary>
        /// <returns>Returns false if reader not connected.</returns>
        private bool ResetAutoModeSettings()
        {
            if (readerSettings == null) return false;

            if (!IsConnected)
            {
                if (!Connect()) return false;
            }

            try
            {
                reader.TimeZone = "0"; //Should be UTC time

                DateTime nowTime = DateTime.UtcNow;
                string dateTime = nowTime.ToString("y/MM/dd HH:mm:ss", System.Globalization.CultureInfo.GetCultureInfo("en-US").DateTimeFormat);
                reader.DateTime = dateTime; //YYYY/mm/dd hh:mm:ss
                string readerDT = reader.DateTime;

                lock (_settingsLock)
                {
                    // Reader doesn't send RSSI data unless RSSIFilter is set to something 
                    //  different to what is saved on the reader. We set it to a new value 
                    //  each time, even though we don't use it.
                    reader.RSSIFilter = readerSettings["RSSIFilter"];

                    reader.AntennaSequence = readerSettings["AntennaSequence"];
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Resets certain reader settings. 
        /// </summary>
        /// <returns>Returns false if reader unable to connect or unable to save settings.</returns>
        public bool FullResetSettings()
        {
            if (readerSettings == null) return false;

            if (!Connect()) return IsConnected = false;

            try
            {
                // Reset reader to factory settings; Takes effect on reboot.
                reader.FactorySettings();

                lock (_settingsLock)
                {

                    reader.IPAddress = readerSettings["IPAddress"];
                    reader.Netmask = readerSettings["Netmask"];
                    reader.Gateway = server.IPAddressString;
                    reader.CommandPort = readerSettings["CommandPort"];

                    //General
                    reader.AntennaSequence = readerSettings["AntennaSequence"];
                    reader.RFModulation = readerSettings["RFModulation"];

                    //Time
                    //reader.TimeZone = "America/Chicago";
                    reader.TimeZone = "0"; //Should be UTC time 
                    DateTime nowTime = DateTime.UtcNow;
                    string dateTime = nowTime.ToString("y/MM/dd HH:mm:ss", System.Globalization.CultureInfo.GetCultureInfo("en-US").DateTimeFormat);
                    reader.DateTime = dateTime; //YYYY/mm/dd hh:mm:ss

                    //Taglist
                    reader.TagListMillis = "ON";
                    reader.PersistTime = readerSettings["PersistTime"];
                    reader.TagListAntennaCombine = readerSettings["TagListAntennaCombine"];
                    reader.TagStreamMode = "ON";
                    reader.TagStreamAddress = server.NotificationHost;
                    reader.TagStreamFormat = "Custom";
                    reader.TagStreamCustomFormat = readerSettings["TagStreamCustomFormat"];
                    //Custom XML message string. $$Make more dynamic and automated.
                    reader.TagListCustomFormat = readerSettings["TagStreamCustomFormat"];


                    //Acquire
                    reader.AcquireMode = readerSettings["AcquireMode"];
                    reader.AcqG2Session = readerSettings["AcqG2Session"];
                    reader.AcqG2AntennaCombine = readerSettings["AcqG2AntennaCombine"];

                    // Reader doesn't send RSSI data unless RSSIFilter is set to something 
                    //  different to what is saved on the reader. We set it to a new value 
                    //  each time, even though we don't use it.
                    if (readerSettings["RSSIFilter"].Equals("-159 160"))
                    {
                        reader.RSSIFilter = "-160 160";
                    }
                    else
                    {
                        reader.RSSIFilter = "-159 160";
                    }

                    //Automode
                    reader.AutoModeReset();
                    reader.AutoStopTimer = readerSettings["AutoStopTimer"];
                }

                // Save Settings. Some settings need reboot to take effect.
                reader.SaveSettings();
            }
            catch(Exception e)
            {
                LogMessage("Failed to save settings to reader. Exception: " + e.Message);
                return false;
            }

            LogMessage("Settings saved to reader. Some settings may require reboot to take effect.");

            return true;
        }

        //$$? Could maybe make better. Is there a way to do this without blocking a thread?
        /// <summary>
        /// Pseudo asynchronous reboot of reader. Takes about 60 seconds to complete.
        /// Note, not really asynchronous. Blocks a threadpool thread.
        /// </summary>
        /// <returns>Awaitable task representing reboot.</returns>
        public async Task Reboot()
        {
            if (!Connect())
            {
                LogMessage("Failed to reboot RFID reader. Failed to connect to reader.");
                return;
            }

            await Task.Run(reader.Reboot);
        }

        #endregion

    }






}
