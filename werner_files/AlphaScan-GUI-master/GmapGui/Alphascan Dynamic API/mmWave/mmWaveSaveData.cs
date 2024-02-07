using System;
using System.IO;
using System.Linq;
using CES.AlphaScan.Base;

namespace CES.AlphaScan.mmWave
{
    public static class mmWaveSaveData
    {
        /*
            Functionality for saving raw mmWave data. This varies from other sensors due to the size of a mmWave packet. Saving into a CSV will slow
            the whole program a ton due to the massive amount of writing to occur, so the solution was to save mmWave data into a binary file for quick writing.
            This data can be analyzed using binary readers and conversions, where analyzing the data is similar to what is seen in this applciation.
        */ 
         
        static string fileName = "mmWaveRawData" + ".bin";

        /// <summary>
        /// saves mmWave packet data as a binary file of format date ticks + packet, appends to same file
        /// </summary>
        /// <param name="buffer">input byte list of raw data</param>
        /// <param name="dataReceiveTime">time associated with the raw data</param>
        /// <param name="outputManager">outputManager to save the data.</param>
        public static bool SaveData(byte[] buffer, DateTime dataReceiveTime, IOutputManager outputManager)     // saves packet as CSV, see naming scheme above
        {
            object data = Tuple.Create(buffer, dataReceiveTime);

            return outputManager.TrySaveData(fileName, data, saveRawDataFunc);
        }
        
        /// <summary>
        /// Function delegate containing instructions for how to save raw mmWave data to a file.
        /// </summary>
        static Func<string, object, bool, bool> saveRawDataFunc = (string filePath, object data, bool isInit) =>
        {
            //When file is newly created.
            if (!isInit)
            {

            }

            //Convert data object
            var dataVal = (Tuple<byte[], DateTime>)data;
            byte[] buffer = dataVal.Item1;
            DateTime time = dataVal.Item2;

            //Saving
            byte[] datetimeTicks = BitConverter.GetBytes(time.Ticks);
            byte[] bytes = buffer.ToArray();

            using (FileStream fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(fileStream))
            {
                writer.Write(datetimeTicks);
                writer.Write(bytes);
            }

            //Whether successful
            return true;
        };
    }
}
