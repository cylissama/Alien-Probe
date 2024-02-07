using System.Collections.Generic;
//using System.Threading.Channels;
using System.Threading.Tasks;

namespace Alphascan_Stationary_API
{
    public class APIManager
    {
        // notes api set up as a class library (can not run from API, must be accessed by another project)

        // variables for access
        //combinedSensors.sharedVariables combinedVars = new combinedSensors.sharedVariables();
        mmWave.mmWavePorts mmWavePortAccess = new mmWave.mmWavePorts();
        mmWave.mmWavePostProcessManager mmWavePostProcess = new mmWave.mmWavePostProcessManager();
        mmWave.mmWaveSaveData mmWaveSave = new mmWave.mmWaveSaveData();

        GPS.gpsPorts gpsPort = new GPS.gpsPorts();
        GPS.gpsSaving gpsSave = new GPS.gpsSaving();
        // end variables
        

        public void moveAllOldData(string saveDirectory)
        {
            mmWaveSave.moveFiles(saveDirectory);
        }

        #region mmWaveAccess
        public void mmWaveOpenPort(string COM, string DATA, string config, string saveDirectory)
        {
            try { moveAllOldData(saveDirectory); } catch { }
            mmWavePortAccess.startingmmWave(COM, DATA, config, saveDirectory);
        }

        public void mmWaveClosePort()
        {
            mmWavePortAccess.stopMmWave();
        }

        public void postProcessmmWaveData(string directory, string sensorType)
        {
            mmWavePostProcess.startPostProcess(directory, sensorType);
        }

        #endregion

        #region gpsAccess
        public void GPSOpenPort(string port, string saveDirectory)
        {
            try { moveAllOldData(saveDirectory); } catch { }
            gpsPort.startingGPS(port, saveDirectory);
        }

        public void GPSClosePort(string saveDirectory)
        {
            gpsPort.stopGPS(saveDirectory);
        }


        #endregion

        #region RFIDAccess

        #endregion
    }
}
