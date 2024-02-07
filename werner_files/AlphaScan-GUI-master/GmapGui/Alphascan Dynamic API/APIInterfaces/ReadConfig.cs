using System;
using System.Collections.Generic;
using System.IO;

namespace CES.AlphaScan.Acquisition
{
    /// <summary>
    /// Class with a method for reading configuration files for each sensor.
    /// </summary>
    public class ReadConfig
    {
        /// <summary>
        /// Reads config files for each sensor and adds those config options to a dictionary
        /// </summary>
        /// <param name="sensorType">sensor config being read</param>
        /// <returns>Dictionary of Key: Setting Name, Value: Setting Value</returns>
        public static IDictionary<string, object> SensorConfigReader(string sensorType)
        {
            string programDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Alphascan");

            string filePath = Path.Combine(programDirectory, "config", sensorType + ".cfg");
            if (!File.Exists(filePath))
                return null;

            IDictionary<string, object> output = new Dictionary<string, object>();

            using (StreamReader reader = new StreamReader(filePath))
            {
                while(!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    // Allow comment lines starting with "#".
                    if (line.Trim().StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;

                    string[] vals = line.Split('=');
                    output.Add(vals[0], vals[1]);
                }
            }
            return output;
        }

    }
}
