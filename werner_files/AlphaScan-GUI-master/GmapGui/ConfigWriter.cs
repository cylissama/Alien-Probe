using System;
using System.Collections.Generic;
using System.IO;

namespace GmapGui
{
    /// <summary>
    /// Reads settings from config files and allows the settings to be accessed.
    /// </summary>
    public class SettingsData
    {
        /// <summary>
        /// Creates a new settings data object using the data in the config files.
        /// </summary>
        public SettingsData()
        {
            /*
                Settings for the sensors are stored in config files which are configured using these methods. These will create default configs in the documents folder if there
                are no settings present, edit the configs in documents with desired settings from the settings window, and compile the settings for use in the sensor applications.
                Default configs are saved in the Appdata/Roaming/Alphascan folders CANNOT BE DELETED.
            
                These configs work in a dictionary format where the format for the settings is:
                        
                    SettingName=SettingValue

                Where the key in the dictionary corresponds to the SettingName, or the value to the left of the equals sign, and the value responds to the SettingsValue, ro the value
                to the right of the equals sign. These are saved in an IDictionary<string, string> format for ease of use.

                Accessing a setting can be called by initilizing the SettingsData class which will read the cfg files to get their settings. The individiual settings for the desired
                operation can be accessed using SettingsDataInitializer.SETTINGTYPE["SettingName"] which will then search that specific setting type for the key.

                AVAILABLE SETTING TYPE:
                    mmWave      mmWave settings
                    GPS         GPS settings
                    RFID        RFID settings
                    Global      Global settings, save location, vehicle side
                    Map         Map file information
                    Info        Info file information, obselete
              
             
            */ 

            string programDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Alphascan");

            foreach (string type in SettingTypes)
            {
                if (File.Exists(Path.Combine(programDirectory, "config", type + ".cfg")))
                {
                    string[] fileLines = File.ReadAllLines(Path.Combine(programDirectory, "config", type + ".cfg"));
                    foreach (string line in fileLines)
                    {
                        // Ignore comment lines starting with "#" and blank lines.
                        if (line.Trim().StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;

                        string[] vals = line.Split('=');
                        switch (type)
                        {
                            case "mmWave":
                                mmWaveSettings.Add(vals[0], vals[1]);
                                break;
                            case "GPS":
                                GPSSettings.Add(vals[0], vals[1]);
                                break;
                            case "RFID":
                                RFIDSettings.Add(vals[0], vals[1]);
                                break;
                            case "Global":
                                GlobalSettings.Add(vals[0], vals[1]);
                                break;
                            case "Map":
                                MapSettings.Add(vals[0], vals[1]);
                                break;
                            case "Info":
                                InfoSettings.Add(vals[0], vals[1]);
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Either overwrites or adds a setting with the given value.
        /// </summary>
        /// <param name="input">Config list to add setting to.</param>
        /// <param name="key">Key of setting to set.</param>
        /// <param name="value">Value to set setting to.</param>
        private void CheckIfKeyExists(IDictionary<string, string> input, string key, string value)
        {
            if (input.ContainsKey(key))
            {
                input[key] = value;
            }
            else
            {
                input.Add(key, value);
            }
        }

        /// <summary>
        /// Either overwrites or adds a setting with the given value.
        /// </summary>
        /// <param name="settingType">Type of config list to set setting in.</param>
        /// <param name="settingName">Name of the setting to set.</param>
        /// <param name="settingValue">Value of the setting to set.</param>
        public void ChangeSetting(string settingType, string settingName, string settingValue)
        {
            switch (settingType)
            {
                case ("mmWave"):
                    CheckIfKeyExists(mmWaveSettings, settingName, settingValue);
                    break;
                case ("GPS"):
                    CheckIfKeyExists(GPSSettings, settingName, settingValue);
                    break;
                case ("RFID"):
                    CheckIfKeyExists(RFIDSettings, settingName, settingValue);
                    break;
                case ("Global"):
                    CheckIfKeyExists(GlobalSettings, settingName, settingValue);
                    break;
                case ("Map"):
                    CheckIfKeyExists(MapSettings, settingName, settingValue);
                    break;
                case ("Info"):
                    CheckIfKeyExists(InfoSettings, settingName, settingValue);
                    break;
            }
        }

        /// <summary>
        /// Settings for the mmWave sensor.
        /// </summary>
        public IDictionary<string, string> mmWaveSettings { get; } = new Dictionary<string, string>();
        /// <summary>
        /// Settings for the GPS sensor.
        /// </summary>
        public IDictionary<string, string> GPSSettings { get; } = new Dictionary<string, string>();
        /// <summary>
        /// Settings for the RFID reader.
        /// </summary>
        public IDictionary<string, string> RFIDSettings { get; } = new Dictionary<string, string>();
        /// <summary>
        /// Settings for the entire program.
        /// </summary>
        public IDictionary<string, string> GlobalSettings { get; } = new Dictionary<string, string>();
        /// <summary>
        /// Settings used for info files when outputting data.
        /// </summary>
        public IDictionary<string, string> InfoSettings { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Settings used for map files when getting default maps
        /// </summary>
        public IDictionary<string, string> MapSettings { get; } = new Dictionary<string, string>();

        /// <summary>
        /// List of the types of settings contained in the object.
        /// </summary>
        public List<string> SettingTypes { get; } = new List<string> { "mmWave", "GPS", "RFID", "Global", "Info", "Map" };
    }

    /// <summary>
    /// Class for writing configuration files for each sensor or code module.
    /// </summary>
    public static class ConfigWriter
    {
        /// <summary>
        /// Writes a list of configuration settings to a the configuration 
        /// file of a specific sensor or code module. Overwrites existing file.
        /// </summary>
        /// <param name="input">Name-value pairs for each configuration setting to write to the file.</param>
        /// <param name="dataType">Type of config file to write to. Used in file name.</param>
        public static void WriteConfig(IDictionary<string, string> input, string dataType)
        {
            string programDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Alphascan");

            // Create config directory if it does not exist.
            if (!Directory.Exists(Path.Combine(programDirectory, "config")))
            {
                Directory.CreateDirectory(Path.Combine(programDirectory, "config"));
            }

            // Creates config file if it does not exist.
            string filePath = Path.Combine(programDirectory, "config", dataType + ".cfg");
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();
            }

            File.WriteAllText(filePath, string.Empty);
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (KeyValuePair<string, string> item in input)
                {
                    writer.WriteLine(item.Key.ToString() + "=" + item.Value.ToString());
                }
            }
        }

        /// <summary>
        /// Creates new config files if they do not exist with default settings.
        /// </summary>
        /// <param name="sensorType">type of sensor being searched</param>
        public static void CreateDefaultCfgs(string sensorType)
        {
            string programDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Alphascan");
            string documentDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Alphascan");

            // creates alphascan config folder in documents if not existing
            if (!Directory.Exists(Path.Combine(documentDirectory, "config")))
                Directory.CreateDirectory(Path.Combine(documentDirectory, "config"));

            // creates config file if it does not exist
            string filePath = Path.Combine(documentDirectory, "config", sensorType + ".cfg");
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();
            }

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                string[] lines = File.ReadAllLines(Path.Combine(programDirectory, "DefaultConfigs", sensorType + "Default.cfg"));
                foreach (string line in lines)
                {
                    writer.WriteLine(line);
                }

                // Special settings that require using other files' names to determine values.
                if (sensorType == "mmWave") // get the default mmWave sensor startup file (profile.cfg)
                {
                    writer.WriteLine("Config Directory=" + Path.Combine(documentDirectory, "config", "profile.cfg"));

                    string[] profileLines = null;
                    try
                    {
                        profileLines = System.IO.File.ReadAllLines(Path.Combine(programDirectory, "DefaultConfigs", "profile.cfg"));
                    }
                    catch // uses the default in the project directory
                    {
                        profileLines = File.ReadAllLines(Path.Combine(programDirectory, "DefaultConfigs") + "\\profile.cfg");
                    }

                    using (StreamWriter pWriter = new StreamWriter(Path.Combine(documentDirectory, "config", "profile.cfg")))
                    {
                        foreach (string l in profileLines)
                        {
                            pWriter.WriteLine(l);
                        }
                    }
                }
                else if (sensorType == "RFID") // create a new blacklisted tags text file, make that the blacklist file 
                {
                    // create blacklsit default file in config folder of documents
                    if (!File.Exists(Path.Combine(documentDirectory, "BlackListedTags.txt")))
                    {
                        File.Create(Path.Combine(documentDirectory, "config", "BlackListedTags.txt")).Dispose();
                    }
                    // write the blacklist file to the RFIDconfig
                    string blacklistFile = Path.Combine(documentDirectory, "config", "BlackListedTags.txt");
                    if (File.Exists(blacklistFile))
                        writer.WriteLine("BlacklistDetector.BlacklistFileName=" + blacklistFile);
                }
                else if (sensorType == "Global") // create new save directory for saving data if not existing
                {
                    string saveDirectory = Path.Combine(documentDirectory, "SaveData");
                    if (!Directory.Exists(saveDirectory))
                        Directory.CreateDirectory(saveDirectory);
                    writer.WriteLine("Save Directory=" + saveDirectory);
                }
                else if (sensorType == "Map") // write default map files to the map config
                {
                    if (!File.Exists(Path.Combine(documentDirectory, "config", "MapLoad.txt")))
                        File.Create(Path.Combine(documentDirectory, "config", "MapLoad.txt"));
                    if (!File.Exists(Path.Combine(documentDirectory, "config", "PermLoad.txt")))
                        File.Create(Path.Combine(documentDirectory, "config", "PermLoad.txt"));

                    writer.WriteLine("Default Map=" + Path.Combine(programDirectory, "DefaultMaps", "DefaultMap.xml"));
                    writer.WriteLine("Default Perm=" + Path.Combine(programDirectory, "DefaultMaps", "DefaultMap_perm.xml"));
                }

            }
            
        }
    }
}
