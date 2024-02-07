using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.IO.Ports;
using System.Management;
using ParkDS;
using System.Linq;

namespace GmapGui
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// Window for changing system settings.
    /// </summary>
    public partial class AlphaScanSettings : Window
    {
        bool adminCheck;

        SettingsData settings = new SettingsData();

        private PermManager permManager;

        private string permName;
        string[] keys;

        public AlphaScanSettings(bool admin)
        {
            InitializeComponent();

            permManager = new PermManager();
            string defaultMapLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Alphascan", "DefaultMaps");

            if (!Directory.Exists(defaultMapLocation))
                Directory.CreateDirectory(defaultMapLocation);

            if (!File.Exists(defaultMapLocation + "\\DefaultLoad.txt"))
            {
                var myFile = File.CreateText(defaultMapLocation + "\\DefaultLoad.txt");
                myFile.Close();
            }


            if (!File.Exists(defaultMapLocation + "\\DefaultPermLoad.txt"))
            {
                var myOtherFile = File.CreateText(defaultMapLocation + "\\DefaultPermLoad.txt");
                myOtherFile.Close();
            }


            StreamReader streamReader = new StreamReader(defaultMapLocation + "\\DefaultLoad.txt");
            string defaultMapPath = streamReader.ReadLine();
            txb_defaultMapFile.Text = defaultMapPath;
            streamReader.Close();


            StreamReader streamReader1 = new StreamReader(defaultMapLocation + "\\DefaultPermLoad.txt");
            string defaultPermPath = streamReader1.ReadLine();
            txb_defaultPermissionsFile.Text = defaultPermPath;
            streamReader1.Close();

            adminCheck = admin;

            // check if the settings file exists, if not use default files to create new settings
            if (settings.mmWaveSettings.Count == 0)
                ConfigWriter.CreateDefaultCfgs("mmWave");
            if (settings.GPSSettings.Count == 0)
                ConfigWriter.CreateDefaultCfgs("GPS");
            if (settings.RFIDSettings.Count == 0)
                ConfigWriter.CreateDefaultCfgs("RFID");
            if (settings.GlobalSettings.Count == 0)
                ConfigWriter.CreateDefaultCfgs("Global");
            if (settings.MapSettings.Count == 0)
                ConfigWriter.CreateDefaultCfgs("Map");

            // refresh settings
            settings = new SettingsData();

            // -- MAP SETTINGS -- \\

            // get default map and perm directories
            if (settings.MapSettings.ContainsKey("Default Map"))
                txb_defaultMapFile.Text = settings.MapSettings["Default Map"];
            if (settings.MapSettings.ContainsKey("Default Perm"))
                txb_defaultPermissionsFile.Text = settings.MapSettings["Default Perm"];

            // -- MMWAVE SETTINGS -- \\

            // default settings (set within the default config file)
            // get mmWave config directory from config
            if (settings.mmWaveSettings.ContainsKey("Config Directory"))
                mmWaveconfigDirectory.Text = settings.mmWaveSettings["Config Directory"];
            // get mmWave save directory from config
            if (settings.mmWaveSettings.ContainsKey("Save Data"))
                savemmWaveOption.IsChecked = Convert.ToBoolean(settings.mmWaveSettings["Save Data"]);
            // non default settings
            // process of getting the mmWave settings from the config file to update the default item displayed and selected to be the port in the config
            // will display the selected COM ports
            ComboBoxItem UARTitem = new ComboBoxItem();
            if (settings.mmWaveSettings.ContainsKey("UART Port"))
            {
                UARTitem.Content = settings.mmWaveSettings["UART Port"];
                mmWaveUARTPort.Items.Add(UARTitem);
                mmWaveUARTPort.SelectedItem = UARTitem;
            }

            // process of getting the mmWave settings from the config file to update the default item displayed and selected to be the port in the config
            // will display the selected COM ports
            ComboBoxItem DATAitem = new ComboBoxItem();
            if (settings.mmWaveSettings.ContainsKey("DATA Port"))
            {
                DATAitem.Content = settings.mmWaveSettings["DATA Port"];
                mmWaveDataPort.Items.Add(DATAitem);
                mmWaveDataPort.SelectedItem = DATAitem;
            }

            // -- GPS SETTINGS -- \\

            // default settings (in default config)
            if (settings.GPSSettings.ContainsKey("RTK Enabled"))
                gpsEnableRTK.IsChecked = Convert.ToBoolean(settings.GPSSettings["RTK Enabled"]);
            if (settings.GPSSettings.ContainsKey("Rate"))
                gpsRate.Text = settings.GPSSettings["Rate"];
            if (settings.GPSSettings.ContainsKey("Save Data"))
                saveGPSOption.IsChecked = Convert.ToBoolean(settings.GPSSettings["Save Data"]);
            // non default settings
            // process of getting the GPS setting from the config file to update the default item displayed and selected to be the port in the config
            // will display the selected COM port
            ComboBoxItem item = new ComboBoxItem();
            if (settings.GPSSettings.ContainsKey("COM Port"))
            {
                item.Content = settings.GPSSettings["COM Port"];
                gpsCOMMPort.Items.Add(item);
                gpsCOMMPort.SelectedItem = item;
            }

            // -- RFID SETTINGS -- \\

            // default settings
            // get RFID port number from config
            if (settings.RFIDSettings.ContainsKey("Port"))
                RFIDPort.Text = settings.RFIDSettings["Port"];
            // get RFID save tag data check from config
            if (settings.RFIDSettings.ContainsKey("SaveTagData"))
                saveRFIDOption.IsChecked = Convert.ToBoolean(settings.RFIDSettings["SaveTagData"]);
            // get RFID detect blacklsit tags check from config
            if (settings.RFIDSettings.ContainsKey("DetectBlacklistTags"))
                enableBlacklistOption.IsChecked = Convert.ToBoolean(settings.RFIDSettings["DetectBlacklistTags"]);
            // get RFID blacklist file name from config
            if (settings.RFIDSettings.ContainsKey("BlacklistDetector.BlacklistFileName"))
                blackListFileDirectoryText.Text = settings.RFIDSettings["BlacklistDetector.BlacklistFileName"];
            // non default settings (not set from default config)
            // get RFID reader username from config
            if (settings.RFIDSettings.ContainsKey("Username"))
                RFIDUserName.Text = settings.RFIDSettings["Username"];
            // get RFID reader password from config
            if (settings.RFIDSettings.ContainsKey("Password"))
                RFIDPassword.Text = settings.RFIDSettings["Password"];
            // get RFID reader readerIP from config
            if (settings.RFIDSettings.ContainsKey("ReaderIP"))
                RFIDReaderIP.Text = settings.RFIDSettings["ReaderIP"];
            // get RFID reader serverIP from config
            if (settings.RFIDSettings.ContainsKey("ServerIP"))
                RFIDServerIP.Text = settings.RFIDSettings["ServerIP"];

            // -- GLOBAL SETTINGS -- \\

            // get global save directory for application and vehicle side from config if present
            if (settings.GlobalSettings.ContainsKey("Save Directory"))
                globalSaveDirectory.Text = settings.GlobalSettings["Save Directory"];
            // get vehicle side being observed, set the setting to be correct on the GUI
            if (settings.GlobalSettings.ContainsKey("Vehicle Side"))
            {
                string vehSide = settings.GlobalSettings["Vehicle Side"];
                if (vehSide == "1")
                    VehicleSideSelect.SelectedIndex = 1;
                else
                    VehicleSideSelect.SelectedIndex = 0;
            }
        }

        //Clicking the "..." button will pull up a list of the XML files in the debug folder. User can select one that will be used as the default selection for a map
        //when running aquisition or enforcement
        private void btn_selectDefaultMap_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML Files (*.xml)|*.xml*";
            if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Alphascan", "Maps")))
                openFileDialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Alphascan", "Maps");
            else
                openFileDialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Alphascan", "DefaultMaps");
            if (openFileDialog.ShowDialog() == true)
            {
                string temp = openFileDialog.FileName;
                txb_defaultMapFile.Text = temp;
                StreamWriter writer = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Alphascan", "DefaultMaps", "DefaultLoad.txt"), false);
                writer.WriteLine(temp);
                writer.Close();
            }
        }

        /// <summary>
        /// save settings from the settings window into each type's respective directory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveSettings_Click(object sender, RoutedEventArgs e)
        {
            // need to optimize this to be more efficient, possibly a list and loop? or mayb a dictionary

            // save all mmWave settings from the settings window to its respective file (as strings)
            settings.ChangeSetting("mmWave", "UART Port", mmWaveUARTPort.Text);
            settings.ChangeSetting("mmWave", "DATA Port", mmWaveDataPort.Text);
            settings.ChangeSetting("mmWave", "Save Data", savemmWaveOption.IsChecked.ToString());

            // save all GPS settings from the settings window to its respective file (as strings)
            settings.ChangeSetting("GPS", "COM Port", gpsCOMMPort.Text);
            settings.ChangeSetting("GPS", "RTK Enabled", gpsEnableRTK.IsChecked.ToString());
            settings.ChangeSetting("GPS", "Rate", gpsRate.Text);
            settings.ChangeSetting("GPS", "Save Data", saveGPSOption.IsChecked.ToString());
            // save all RFID settings from the settings window to its respective file (as strings)
            settings.ChangeSetting("RFID", "Username", RFIDUserName.Text);
            settings.ChangeSetting("RFID", "Password", RFIDPassword.Text);
            settings.ChangeSetting("RFID", "ReaderIP", RFIDReaderIP.Text);
            settings.ChangeSetting("RFID", "ServerIP", RFIDServerIP.Text);
            settings.ChangeSetting("RFID", "Port", RFIDPort.Text);
            settings.ChangeSetting("RFID", "SaveTagData", saveRFIDOption.IsChecked.ToString());
            settings.ChangeSetting("RFID", "DetectBlacklistTags", enableBlacklistOption.IsChecked.ToString());
            // save all global settings from the settings window to its respective file (as strings)
            int vehSideEnum;
            if (VehicleSideSelect.Text == "Left") { vehSideEnum = 0; }
            else { vehSideEnum = 1; }

            settings.ChangeSetting("Global", "Vehicle Side", vehSideEnum.ToString());
            settings.ChangeSetting("Global", "Save Directory", globalSaveDirectory.Text);
            // save all map settings from the settings window to its respective file (as strings)
            settings.ChangeSetting("Map", "Default Map", txb_defaultMapFile.Text);
            settings.ChangeSetting("Map", "Default Perm", txb_defaultPermissionsFile.Text);
            // finalize and write all configs using new settings
            ConfigWriter.WriteConfig(settings.mmWaveSettings, "mmWave");
            ConfigWriter.WriteConfig(settings.GPSSettings, "GPS");
            ConfigWriter.WriteConfig(settings.RFIDSettings, "RFID");
            ConfigWriter.WriteConfig(settings.GlobalSettings, "Global");
            ConfigWriter.WriteConfig(settings.InfoSettings, "Info");
            ConfigWriter.WriteConfig(settings.MapSettings, "Map");
            // re open the main window when the settings are saved and close the settings window
            alphaScanMainMenu open = new alphaScanMainMenu(adminCheck);
            open.Show();
            this.Close();
        }

        //Return user to main menu (activates on a cancel)
        private void btn_exit_Click(object sender, RoutedEventArgs e)
        {
            alphaScanMainMenu open = new alphaScanMainMenu(adminCheck);
            open.Show();
            this.Close();
        }

        //Lets user select a permission file to be used.
        private void btn_selectDefaultPermissions_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML Files (*.xml)|*.xml*";
            if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Alphascan", "Permissions")))
                openFileDialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Alphascan", "Permissions");
            else
                openFileDialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Alphascan", "DefaultMaps");
            if (openFileDialog.ShowDialog() == true)
            {
                string temp = openFileDialog.FileName;
                txb_defaultPermissionsFile.Text = temp;
                StreamWriter writer = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Alphascan", "DefaultMaps", "DefaultPermLoad.txt"), false);
                //changed to allign with how permissions are loaded in now
                writer.WriteLine(temp);
                writer.Close();
            }
        }

        //***Need to rename
        //This adds the permission to the listbox, each permission add gets its own row, and each attribute goes to its appropriate column
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (tbx_PermName.Text != "")
            {
                if (hexCheck(txb_lowBound.Text) && hexCheck(txb_hiBound.Text))
                {
                    lsb_PermInfo.Items.Add(tbx_PermName.Text);
                    lsb_PermInfo.Items.Add(txb_lowBound.Text);
                    lsb_PermInfo.Items.Add(txb_hiBound.Text);

                    tbx_PermName.Text = "";
                    txb_hiBound.Text = "";
                    txb_lowBound.Text = "";
                }
                else
                {
                    MessageBox.Show("Please only input hexadecimal values for upper and lower bounds.");
                }
            }
            else
            {
                MessageBox.Show("Please enter a name for the permission.");
            } 
        }

        //Removes the entire row of whatever item is selected in the listbox
        private void btn_RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            int startIndex;
            int startIndexCorrected;
            startIndex = lsb_PermInfo.SelectedIndex;
            startIndexCorrected = (startIndex / 3);
            startIndexCorrected = startIndexCorrected * 3;

            if (startIndexCorrected >= 3)
            {
                lsb_PermInfo.Items.RemoveAt(startIndexCorrected);
                lsb_PermInfo.Items.RemoveAt(startIndexCorrected);
                lsb_PermInfo.Items.RemoveAt(startIndexCorrected);
            }
            else
            {
                MessageBox.Show("Please select a valid row to remove.");
            }
        }

        //Makes sure entire string for upper and lower bounds are in hex
        private bool hexCheck(string str)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(str, @"\A\b[0-9a-fA-F]+\b\Z");

        }

        //This creates a new permission file populated with the user inputted data in the "New Permission File" section of settings
        private void btn_CreateFile_Click(object sender, RoutedEventArgs e)
        {
            int i = 0;
            bool name = true;
            bool start = false;
            bool stop = false;
            try
            {
                permManager.RemoveAllPerms();
            }
            catch { }

            keys = new string[2];

            foreach (var lbi in lsb_PermInfo.Items)
            {
                if(i < 3)
                {
                    i++;
                    continue;
                }
                else
                {
                    if (name)
                    {
                        permName = lbi.ToString();
                        start = true;
                        name = false;
                        continue;
                    }
                    else if (start)
                    {
                        keys[0] = lbi.ToString();
                        start = false;
                        stop = true;
                        continue;

                    }
                    else if (stop)
                    {
                        keys[1] = lbi.ToString();
                        stop = false;
                        name = true;

                        
                        permManager.AddPermission(permName, keys, null);
                        
                        Console.WriteLine("Perm Name: " + permName + " Lower Bound: " + keys[0] + " Upper Bound: " + keys[1]);
                        continue;
                    }
                    
                }
                
            }
            permManager.SavePerm(newPermFileName.Text + ".xml");
            clearListBoxes();

        }

        //Lets the user select a file to edit and populates the listbox with that files info
        private void btn_editPermFile_Click(object sender, RoutedEventArgs e)
        {
            
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML Files (*.xml)|*.xml*";
            // load into documents if the folder exists, else go to the default location
            if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Alphascan", "Permissions")))
                openFileDialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Alphascan", "Permissions");
            else
                openFileDialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Alphascan", "DefaultMaps");
            if (openFileDialog.ShowDialog() == true)
            {
                string temp = openFileDialog.FileName;
                txb_editFileName.Text = temp;
                permManager.LoadPerm(temp);
                List<Permission> perms = permManager.GetAvailablePerms();
                foreach (Permission perm in perms)
                {
                        lbx_LoadedPermStuff.Items.Add(perm.Name);                      
                        lbx_LoadedPermStuff.Items.Add(perm.Keys[0]);                     
                        lbx_LoadedPermStuff.Items.Add(perm.Keys[1]);                                          
                }
            }
            //All perms removed for save file reasons
            permManager.RemoveAllPerms();
            
        }

        //Same as adding perms to a new file
        private void btn_addToListEdit_Click(object sender, RoutedEventArgs e)
        {
            if (txb_editPermName.Text != "")
            {
                if (hexCheck(txb_editPermLo.Text) && hexCheck(txb_editPermHi.Text))
                {
                    lbx_LoadedPermStuff.Items.Add(txb_editPermName.Text);
                    lbx_LoadedPermStuff.Items.Add(txb_editPermLo.Text);
                    lbx_LoadedPermStuff.Items.Add(txb_editPermHi.Text);

                    txb_editPermName.Text = "";
                    txb_editPermLo.Text = "";
                    txb_editPermHi.Text = "";
                }
                else
                {
                    MessageBox.Show("Please only input hexadecimal values for upper and lower bounds.");
                }
            }
            else
            {
                MessageBox.Show("Please enter a name for the permission.");
            }
        }

    
        //Same as removing a row in a new file
    private void btn_RemoveFromListEdit_Click(object sender, RoutedEventArgs e)
        {
            int startIndex;
            int startIndexCorrected;
            startIndex = lbx_LoadedPermStuff.SelectedIndex;
            startIndexCorrected = (startIndex / 3);
            startIndexCorrected = startIndexCorrected * 3;

            if (startIndexCorrected >= 3)
            {
                lbx_LoadedPermStuff.Items.RemoveAt(startIndexCorrected);
                lbx_LoadedPermStuff.Items.RemoveAt(startIndexCorrected);
                lbx_LoadedPermStuff.Items.RemoveAt(startIndexCorrected);
            }
            else
            {
                MessageBox.Show("Please select a valid row to remove.");
            }
        }

        //Saves the file and clears the listbox
        private void btn_saveEditFile_Click(object sender, RoutedEventArgs e)
        {
            int i = 0;
            bool name = true;
            bool start = false;
            bool stop = false;


            keys = new string[2];

            foreach (var lbi in lbx_LoadedPermStuff.Items)
            {
                if (i < 3)
                {
                    i++;
                    continue;
                }
                else
                {
                    if (name)
                    {
                        permName = lbi.ToString();
                        start = true;
                        name = false;
                        continue;
                    }
                    else if (start)
                    {
                        keys[0] = lbi.ToString();
                        start = false;
                        stop = true;
                        continue;

                    }
                    else if (stop)
                    {
                        keys[1] = lbi.ToString();
                        stop = false;
                        name = true;


                        permManager.AddPermission(permName, keys, null);

                        Console.WriteLine("Perm Name: " + permName + " Lower Bound: " + keys[0] + " Upper Bound: " + keys[1]);
                        continue;
                    }

                }

            }

            permManager.SavePerm(txb_editFileName.Text, true);
            clearListBoxes();
        
        }

        //This deletes everything in the listbox and adds back in the headers
        private void clearListBoxes()
        {
            lsb_PermInfo.Items.Clear();
            lbx_LoadedPermStuff.Items.Clear();

            txb_editFileName.Clear();
            newPermFileName.Clear();

            ListBoxItem C1 = new ListBoxItem();
            C1.FontWeight = FontWeights.Bold;
            C1.Content = "Name";            

            ListBoxItem C2 = new ListBoxItem();
            C2.FontWeight = FontWeights.Bold;
            C2.Content = "|Upper Bound";

            ListBoxItem C3 = new ListBoxItem();
            C3.FontWeight = FontWeights.Bold;
            C3.Content = "|Lower Bound";

            ListBoxItem C4 = new ListBoxItem();
            C4.FontWeight = FontWeights.Bold;
            C4.Content = "Name";

            ListBoxItem C5 = new ListBoxItem();
            C5.FontWeight = FontWeights.Bold;
            C5.Content = "|Upper Bound";

            ListBoxItem C6 = new ListBoxItem();
            C6.FontWeight = FontWeights.Bold;
            C6.Content = "|Lower Bound";

            lsb_PermInfo.Items.Add(C1);
            lsb_PermInfo.Items.Add(C2);
            lsb_PermInfo.Items.Add(C3);

            lbx_LoadedPermStuff.Items.Add(C4);
            lbx_LoadedPermStuff.Items.Add(C5);
            lbx_LoadedPermStuff.Items.Add(C6);
        }

        #region API Setting functionality

        /// <summary>
        /// updates list for combobox dropdown to include all current COM ports
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mmWaveUARTPort_DropDownOpened(object sender, EventArgs e)
        {
            string temp = mmWaveUARTPort.Text;
            mmWaveUARTPort.Items.Clear();
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'"))
            {
                var portnames = SerialPort.GetPortNames();
                var ports = searcher.Get().Cast<ManagementBaseObject>().ToList().Select(p => p["Caption"].ToString());

                var portList = portnames.Select(n => n + " - " + ports.FirstOrDefault(s => s.Contains(n))).ToList();
                foreach (var i in portList)
                {
                    mmWaveUARTPort.Items.Add(i);
                }
            }

            mmWaveUARTPort.SelectedItem = temp;
        }

        /// <summary>
        /// updates list for combobox dropdown to include all current COM ports
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mmWaveDataPort_DropDownOpened(object sender, EventArgs e)
        {
            string temp = mmWaveDataPort.Text;
            mmWaveDataPort.Items.Clear();
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'"))
            {
                var portnames = SerialPort.GetPortNames();
                var ports = searcher.Get().Cast<ManagementBaseObject>().ToList().Select(p => p["Caption"].ToString());

                var portList = portnames.Select(n => n + " - " + ports.FirstOrDefault(s => s.Contains(n))).ToList();
                foreach (var i in portList)
                {
                    mmWaveDataPort.Items.Add(i);
                }
            }

            mmWaveDataPort.SelectedItem = temp;
        }

        /// <summary>
        /// updates list for combobox dropdown to include all current COM ports
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gpsCOMMPort_DropDownOpened(object sender, EventArgs e)
        {
            string temp = gpsCOMMPort.Text;
            gpsCOMMPort.Items.Clear();
            
            gpsCOMMPort.SelectedItem = temp;
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'"))
            {
                var portnames = SerialPort.GetPortNames();
                var ports = searcher.Get().Cast<ManagementBaseObject>().ToList().Select(p => p["Caption"].ToString());

                var portList = portnames.Select(n => n + " - " + ports.FirstOrDefault(s => s.Contains(n))).ToList();
                foreach (var i in portList)
                {
                    gpsCOMMPort.Items.Add(i);
                }
            }

            gpsCOMMPort.SelectedItem = temp;
        }

        /// <summary>
        /// select .cfg file of mmWave settings to run
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mmWaveselectConfig_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Config files (*.cfg)|*.cfg";
            if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Alphascan", "config")))
                openFileDialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Alphascan", "config");
            else
                openFileDialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Alphascan", "DefaultConfigs");
            if (openFileDialog.ShowDialog() == true)
            {
                mmWaveconfigDirectory.Text = openFileDialog.FileName;

                settings.ChangeSetting("mmWave", "Config Directory", openFileDialog.FileName);
            }
        }

        /// <summary>
        /// sets the save directoyry for all data found by the API
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void globalSelectSaveDirectory_Click(object sender, RoutedEventArgs e)
        {
            string filePath;
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                filePath = dialog.SelectedPath;
            }
            // do not update anything if cancelled
            if (filePath.Length > 0)
            {
                globalSaveDirectory.Text = filePath;
                settings.ChangeSetting("Global", "Save Directory", filePath);
            }
        }

        #endregion



        private void selectBlacklistBtn(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text Files (*.txt)|*.txt";
            if (openFileDialog.ShowDialog() == true)
            {
                blackListFileDirectoryText.Text = openFileDialog.FileName;

                settings.ChangeSetting("RFID", "BlacklistDetector.BlacklistFileName", openFileDialog.FileName);
            }

        }

        private void changePass_Click(object sender, RoutedEventArgs e)
        {
           PasswordChange open = new PasswordChange();
            open.Show();
           
        }
    }
 }

