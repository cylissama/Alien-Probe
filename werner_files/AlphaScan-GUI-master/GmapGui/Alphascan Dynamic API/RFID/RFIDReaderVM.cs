using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CES.AlphaScan.Base;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Alphascan_Stationary_API.RFID
{

    public class RfidReaderVM : INotifyPropertyChanged
    {
        static private RfidReaderModule readerModule;
        // original
        //private RfidOutputSaver outputSaver = new RfidOutputSaver("C:\\Users\\sml49237\\source\\repos\\RFIDv2\\RFIDOutput");
        // version that uses settings from GUI
        static private RfidOutputSaver outputSaver;
        static bool isInit = false;

        public RfidReaderVM(string readerIP, int port, string serverIP, string saveDirectory)
        {
            InitializeCommands();

            if (!isInit)
            {
                outputSaver = new RfidOutputSaver(saveDirectory);
                isInit = true;
            }


            readerModule.PropertyChanged += ReaderModule_PropertyChanged;
            readerModule.MessageLogged += ReceiveLogMessage;
            readerModule.DataReceived += ReaderModule_DataReceived;
            readerModule.DataReceived += ReaderModule_SaveData;
            
            

            // Enable collection synchronisation for CurrentTags list. Must be done on UI thread.
            // Allows updating the ObservableCollection on non-UI threads.
            // original
            //Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //{ System.Windows.Data.BindingOperations.EnableCollectionSynchronization(CurrentTags, _currentTagsSync); }));
            // test, no UI thread currently
            Task.Run(new Action(() =>
            { System.Windows.Data.BindingOperations.EnableCollectionSynchronization(CurrentTags, _currentTagsSync); }));
        }

        #region Data Saving

        private void ReaderModule_SaveData(object sender, DataReceivedEventArgs e)
        {
            foreach (TagData tag in e.DataBufferCopy as IEnumerable<TagData>)
            {
                outputSaver.SaveData(tag, e?.Tag);
            }
        }

        #endregion


        #region Properties

        /*
         * Property Model:
        object _property;
        object Property
        {
            get
            {
                return _property;
            }

            set
            {
                if (true) // if can update AND value is valid
                {
                    if (_property != value)
                    {
                        _property = value;
                        //Do whatever else needs to be done.

                        NotifyPropertyChanged();
                    }
                }
                else
                {
                    throw new Exception("FailedToUpdate");
                    //throw exception that failed to update.
                    // maybe include info about why failed

                    // $$ maybe raise event instead of throw exception
                }
            }
        }
        */

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ReaderModule_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string propName = e.PropertyName;
            object newVal = null;

            switch (propName)
            {
                case nameof(IsConnected):
                    IsConnected = readerModule.IsConnected;
                    break;
                case nameof(IsRunning):
                    IsRunning = readerModule.IsRunning;
                    break;
                default:
                    break;
            }

            //this.GetType().GetProperty(propName).SetValue(this, newVal);
        }

        #region Connection Control
        private string _ipAddress = "192.168.217.52";
        public string IpAddress
        {
            get
            {
                return _ipAddress;
            }
            set
            {
                if (_ipAddress != value)
                {
                    _ipAddress = value;
                    //Do whatever else needs to be done.

                    NotifyPropertyChanged();
                }
            }
        }

        private int _port = 23;
        public int Port
        {
            get
            {
                return _port;
            }
            set
            {
                if (_port != value)
                {
                    _port = value;
                    //Do whatever else needs to be done.

                    NotifyPropertyChanged();
                }
            }
        }

        private bool _isConnected = false;
        public bool IsConnected
        {
            get
            {
                return _isConnected;
            }
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    //Do whatever else needs to be done.

                    NotifyPropertyChanged();
                }
            }
        }
        #endregion

        #region Communication

        private string _monitorInputText = "";
        public string MonitorInputText
        {
            get
            {
                return _monitorInputText;
            }
            set
            {
                if (_monitorInputText != value)
                {
                    _monitorInputText = value;
                    //Do whatever else needs to be done.

                    NotifyPropertyChanged();
                }
            }
        }

        private string _monitorLogText = "";
        public string MonitorLogText
        {
            get
            {
                return _monitorLogText;
            }
            set
            {
                if (_monitorLogText != value)
                {
                    _monitorLogText = value;
                    //Do whatever else needs to be done.

                    NotifyPropertyChanged();
                }
            }
        }

        private void ReceiveLogMessage(object sender, DataReceivedEventArgs e)
        {
            MonitorLogText += ">> " + e.DataBufferCopy.ToString() + Environment.NewLine;
        }

        #endregion

        #region Data Output

        //private ObservableCollection<TagData> _currentTags = new ObservableCollection<TagData>();
        public ObservableCollection<TagData> CurrentTags { get; set; } = new ObservableCollection<TagData>();
        private readonly object _currentTagsSync = new object();

        /// <summary>
        /// Handles new tags being read by reader.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReaderModule_DataReceived(object sender, DataReceivedEventArgs e)
        {
            lock (_currentTagsSync) // lock needed for updating collection on background thread.
            {
                // Gets union of new tags and old tags;
                var newTagList = CurrentTags.ToList().Union(e.DataBufferCopy as IEnumerable<TagData>);

                // Selects most recent reading for each tagId and each antenna.
                //$$newTagList = newTagList.GroupBy(tag => tag.TagId).Select(tagsById => tagsById.GroupBy(tag => tag.RxAntenna).Select(tagsByAntenna => tagsByAntenna.OrderByDescending(tag => tag.LastSeenMSec).First())).SelectMany(tag => tag);
                var t1 = newTagList.GroupBy(tag => tag.TagId);
                var t2 = t1.Select(tagsById => tagsById.GroupBy(tag => tag.RxAntenna));
                var t3 = t2.SelectMany(tagsByAntennaById => tagsByAntennaById.Select(tagsByAntenna => tagsByAntenna.OrderByDescending(tag => tag.LastSeenMSec).First()));

                // Replaces current tag list with new tag list.
                CurrentTags.Clear();
                foreach (TagData tag in t3)
                {
                    CurrentTags.Add(tag);
                }
            }
        }

        #endregion



        #region Control

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
                    //Do whatever else needs to be done.

                    NotifyPropertyChanged();
                }
            }
        }

        #endregion

        #endregion


        #region Commands

        // List of command bindings for view to copy
        public CommandBindingCollection CommandBindings = new CommandBindingCollection();

        // Creates command binding for each command
        private void InitializeCommands()
        {
            CommandBindings.Add(new CommandBinding(ConnectCommand, ConnectCommand_Executed, ConnectCommand_CanExecute));
            CommandBindings.Add(new CommandBinding(DisconnectCommand, DisconnectCommand_Executed, DisconnectCommand_CanExecute));
            CommandBindings.Add(new CommandBinding(SendCommand, SendCommand_Executed, SendCommand_CanExecute));
            CommandBindings.Add(new CommandBinding(StartCommand, StartCommand_Executed, StartCommand_CanExecute));
            CommandBindings.Add(new CommandBinding(StopCommand, StopCommand_Executed, StopCommand_CanExecute));
            CommandBindings.Add(new CommandBinding(ResetCommand, ResetCommand_Executed, ResetCommand_CanExecute));

        }


        //Connect Command
        private RoutedUICommand _connectCommand = new RoutedUICommand("Connect", "ConnectRfid", typeof(RfidReaderVM));
        public RoutedUICommand ConnectCommand
        {
            get
            {
                return _connectCommand;
            }
            set
            {
                if (_connectCommand != value)
                {
                    _connectCommand = value;
                    //$? Maybe add some handling of change in CommandBindingCollection
                    NotifyPropertyChanged();
                }
            }
        }

        private void ConnectCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            //Is connect valid?
            e.CanExecute = true;
        }

        private void ConnectCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //Connect to reader
            readerModule.Connect();
        }


        //Disconnect Command
        private RoutedUICommand _disconnectCommand = new RoutedUICommand("Disconnect", "DisconnectRfid", typeof(RfidReaderVM));
        public RoutedUICommand DisconnectCommand
        {
            get
            {
                return _disconnectCommand;
            }
            set
            {
                if (_disconnectCommand != value)
                {
                    _disconnectCommand = value;
                    //$? Maybe add some handling of change in CommandBindingCollection
                    NotifyPropertyChanged();
                }
            }
        }

        private void DisconnectCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            //Is command valid?
            e.CanExecute = true;
        }

        private void DisconnectCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //Execute command
            readerModule.Disconnect();
        }


        //Send Command
        private RoutedUICommand _sendCommand = new RoutedUICommand("Send", "SendToRfid", typeof(RfidReaderVM));
        public RoutedUICommand SendCommand
        {
            get
            {
                return _sendCommand;
            }
            set
            {
                if (_sendCommand != value)
                {
                    _sendCommand = value;
                    //$? Maybe add some handling of change in CommandBindingCollection
                    NotifyPropertyChanged();
                }
            }
        }

        private void SendCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            //Is connect valid?
            e.CanExecute = true;
        }

        private void SendCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //Execute command
            string stringToSend = (string)e.Parameter;
            readerModule.SendMessage(stringToSend);

            MonitorInputText = "";
            //$? maybe get response, else handle elsewhere
        }


        //Start Command
        private RoutedUICommand _startCommand = new RoutedUICommand("Start", "StartRfid", typeof(RfidReaderVM));
        public RoutedUICommand StartCommand
        {
            get
            {
                return _startCommand;
            }
            set
            {
                if (_startCommand != value)
                {
                    _startCommand = value;
                    //$? Maybe add some handling of change in CommandBindingCollection
                    NotifyPropertyChanged();
                }
            }
        }

        private void StartCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            //Is connect valid?
            e.CanExecute = true;
        }

        private void StartCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //Execute command
            //readerModule.Start();
            RunForTime(30000); //$$ Temporary: for running timed tests.
        }

        //$$ Temporary: for running timed tests.
        private async void RunForTime(int timeMilliseconds)
        {
            readerModule.Start();
            await Task.Delay(timeMilliseconds);
            readerModule.Stop();
        }


        //Stop Command
        private RoutedUICommand _stopCommand = new RoutedUICommand("Stop", "StopRfid", typeof(RfidReaderVM));
        public RoutedUICommand StopCommand
        {
            get
            {
                return _stopCommand;
            }
            set
            {
                if (_stopCommand != value)
                {
                    _stopCommand = value;
                    //$? Maybe add some handling of change in CommandBindingCollection
                    NotifyPropertyChanged();
                }
            }
        }

        private void StopCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            //Is connect valid?
            e.CanExecute = true;
        }

        private void StopCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //Execute command
            readerModule.Stop();
        }


        //Full Reset Command
        private RoutedUICommand _resetCommand = new RoutedUICommand("Reset", "ResetRfid", typeof(RfidReaderVM));
        public RoutedUICommand ResetCommand
        {
            get
            {
                return _resetCommand;
            }
            set
            {
                if (_resetCommand != value)
                {
                    _resetCommand = value;
                    //$? Maybe add some handling of change in CommandBindingCollection
                    NotifyPropertyChanged();
                }
            }
        }

        private void ResetCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            //Is connect valid?
            e.CanExecute = true;
        }

        private void ResetCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //Execute command
            readerModule.FullResetSettings();
        }
    }
        #endregion

}