using System;
using System.IO;
using CES.AlphaScan.Gps;

namespace Alphascan_Stationary_API.GPS
{
    public static class gpsSaving
    {
        static string name;
        static public bool isInit = false;

        /// <summary>
        /// adds line to an existing CSV file of GPS data, will create file if it does not exist and top with header
        /// </summary>
        /// <param name="input"></param>
        /// <param name="saveDirectory"></param>
        public static void saveLine(GpsData input, string saveDirectory)
        {
            if (isInit == false)
            {
                name = input.Time.ToString("mm.dd.yyyy-hh.mm.ss");
                isInit = true;
            }

            var filePath = saveDirectory + "//" + "gps" + name + ".csv";

            string outString = input.Time.ToString() + "," + input.Lat.ToString() + "," + input.Long.ToString() + ","
                + input.RTKEnable.ToString() + "," + input.FixType + "," + input.FixFlagType;

            if (!File.Exists(filePath)) // saves with header
            {
                string header = "Time" + "," + "Lat" + "," + "Long" + "," + "RTK Enabled" + "," + "Fix Type" + "," + "Fix Flag Type";
                File.WriteAllText(filePath, header + "\n");
            }
            else
            {
                File.AppendAllText(filePath, outString + "\n");
            }
        }
    }
}
