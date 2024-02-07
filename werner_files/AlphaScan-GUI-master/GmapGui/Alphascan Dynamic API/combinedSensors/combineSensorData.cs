using System;
using System.Collections.Generic;
using CES.AlphaScan.Acquisition;
using CES.AlphaScan.Base;
using CES.AlphaScan.mmWave;
using CES.AlphaScan.Gps;
using CES.AlphaScan.Rfid;
using System.Diagnostics;

namespace CES.AlphaScan.CombinedSensors
{
    public class CombineSensorData
    {
        #region generate object file + conversions
        /// <summary>
        /// Converts XY values in meters into lat long (decimal degree) values
        /// </summary>
        /// <param name="input">X or Y value to be converted</param>
        /// <returns>Decimal Degree equivalent of the input</returns>
        private double ConvertToDecimalDegrees(double input)
        {
            //https://en.wikipedia.org/wiki/Decimal_degrees based on info from here, verify
            double remainder;
            // find number of times divides, subtract that value from the remainder, may need optimization
            double fourDeg = Math.Floor(input / 11.132); remainder = input - (fourDeg * 11.132);
            double fiveDeg = Math.Floor(remainder / 1.1132); remainder -= (fiveDeg * 1.1132);
            double sixDeg = Math.Floor(remainder / 0.111132); remainder -= (sixDeg * 0.111132);
            double sevenDeg = Math.Floor(remainder / 0.011132); remainder -= (sevenDeg * 0.011132);
            double eightDeg = Math.Floor(remainder / 0.0011132); remainder -= (eightDeg * 0.0011132);

            return (fourDeg * Math.Pow(10, -4)) + (fiveDeg * Math.Pow(10, -5)) + (sixDeg * Math.Pow(10, -6))
                + (sevenDeg * Math.Pow(10, -7)) + (eightDeg * Math.Pow(10, -8));
        }

        /// <summary>
        /// Converts degrees to Radians
        /// </summary>
        /// <param name="input">Degree value</param>
        /// <returns>Radian equivalent of input</returns>
        private double DegToRad(double input)
        {
            return input * (Math.PI / 180);
        }

        /// <summary>
        /// rotates mmWave points to fit the relative bearing from the GPS sensor
        /// </summary>
        /// <param name="cluster">cluster data from the mmWave sensor</param>
        /// <param name="theta">angle of bearing</param>
        /// <returns></returns>
        private ClusteredObject RotatemmWavePoints(ClusteredObject cluster, double theta)
        {
            // taken from this page https://academo.org/demos/rotation-about-point/#:~:text=So%20if%20the%20point%20to,to%20get%20the%20final%20answer.
            // needs verification
            // must convert since c# math.cos function only takes a rad value
            double thetaRad = 0;
            if (cluster.VehicleSide == (VehicleSide)1)  // right side rotation
            {
                if (theta < 180)    // vehihle ride side may be flipped due to the direction of the vehicle
                    thetaRad = DegToRad(theta + 90);
                else
                    thetaRad = DegToRad(theta + 180);
            }
            else // left side rotation
            {
                if (theta < 180) // vehicle side amy be flipped due to the rotation of the vehicle
                    thetaRad = DegToRad(theta + 180);
                else
                    thetaRad = DegToRad(theta + 90);
            }

            double X = (cluster.X * Math.Cos(thetaRad)) - (cluster.Y * Math.Sin(thetaRad)); // find the rotated X value
            double Y = (cluster.Y * Math.Cos(thetaRad)) - (cluster.X * Math.Sin(thetaRad)); // find the rotated Y value
            return new ClusteredObject(X, Y, cluster.Time, cluster.VehicleSide, cluster.ClusterStrength, cluster.ClusterSize); // return cluster with rotated XY value
        }

        /// <summary>
        /// combines mmWave and GPS data into one data type if a time correlation is found
        /// </summary>
        /// <param name="mmWave">mmWave data to combine</param>
        /// <param name="GPS">GPS data to combine</param>
        /// <returns>combined data of mmWave and GPS data</returns>
        private ObjectLocation CreateClusterObject(ClusteredObject mmWave, GpsBearingData GPS)
        {
            // attempt to find rotated X and Y values based on bearing test
            mmWave = RotatemmWavePoints(mmWave, GPS.Bearing);
            // convert mmWave XY into lat long, save into the final struct
            double lng = ConvertToDecimalDegrees(mmWave.X) + GPS.Lng;  // lng is supposed to be X, lat as Y
            double lat = ConvertToDecimalDegrees(mmWave.Y) + GPS.Lat;
            // end this conversion

            return new ObjectLocation(lat, lng, mmWave.Time, mmWave.VehicleSide, mmWave.ClusterStrength, mmWave.ClusterSize);
        }

        /// <summary>
        /// combines data from mmWave and GPS channels to get cluster objects
        /// </summary>
        /// <param name="mmWave">mmWave channel data</param>
        /// <param name="GPS">GPS channel data</param>
        /// <param name="outSaveDirectory">save directory of output</param>
        /// <returns>list of clustered objects</returns>
        public List<ObjectLocation> CombinemmWaveGPSChannels(List<ClusteredObject> mmWave, List<GpsBearingData> GPS)
        {
            // final struct for method
            List<ObjectLocation> objects = new List<ObjectLocation>();
            // end var


            // return if not enough data present, need checking/tuning
            if (mmWave.Count < 5 || GPS.Count < 5)
                return objects;


            // create indexes to remove to ensure a point is only correlated with one cluster
            List<int> indexesToRemovemmWave = new List<int>();
            List<int> indexesToRemoveGPS = new List<int>();
            for (int i = 0; i < mmWave.Count; i++)
            {
                for (int j = 0; j < GPS.Count; j++)
                {
                    TimeSpan timeDiff = mmWave[i].Time.Subtract(GPS[j].Time); // find time difference between two observed data types
                    if (Math.Abs(timeDiff.TotalMilliseconds) <= 300)
                    {
                        objects.Add(CreateClusterObject(mmWave[i], GPS[j]));   // adds new combined cluster set to list
                        indexesToRemovemmWave.Add(i);

                        break;
                    }
                    else if (Math.Abs(timeDiff.TotalMilliseconds) >= 1000 && !indexesToRemoveGPS.Contains(j) &&
                            mmWave[i].Time > GPS[j].Time)   // time difference is too large to be considered any longer, remove the data
                        indexesToRemoveGPS.Add(j);
                    else
                    {
                        continue;
                    }
                }
            }

            // remove all mmWave points that need to be removed
            for (int i = indexesToRemovemmWave.Count - 1; i >= 0; i--)
                mmWave.RemoveAt(indexesToRemovemmWave[i]);  // must go from end to beginning to prevent any exceptions
            indexesToRemovemmWave.Clear();
            // remove all GPS points that need to be removed
            for (int i = indexesToRemoveGPS.Count - 1; i >= 0; i--)
            {
                if (indexesToRemoveGPS[i] < GPS.Count)  // must go from end to beginnign to prevent any exceptions
                    GPS.RemoveAt(indexesToRemoveGPS[i]);
            }
            indexesToRemoveGPS.Clear();
            return objects;
        }

        /// <summary>
        /// finds the distance between two lat/long clusters
        /// </summary>
        /// <param name="c1">geolocated object 1</param>
        /// <param name="c2">geolocated object 2</param>
        /// <returns>distance between the geolocated clusters</returns>
        public static double LatLongDistance(ObjectLocation c1, ObjectLocation c2)
        {
            double lat1 = c1.Lat / (180 / Math.PI);
            double lat2 = c2.Lat / (180 / Math.PI);
            double lng1 = c1.Lng / (180 / Math.PI);
            double lng2 = c2.Lng / (180 / Math.PI);

            return 3963.0 * Math.Acos((Math.Sin(lat1) * Math.Sin(lat2)) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lng2 - lng1));
        }

        #endregion

        // combines objects + RFID
        #region object + RFID

        /// <summary>
        /// combines LocationObjects wth RFID data to create TagLocationObjects
        /// </summary>
        /// <param name="obj">A LocationObject</param>
        /// <param name="RFIDData">A RFID Data set</param>
        /// <returns>TagObjectLocation if found</returns>
        private TagObjectLocation CombineFinalObjectDataNew(ObjectLocation obj, TagPeak RFIDData)
        {
            return new TagObjectLocation(obj.Lat, obj.Lng, obj.Time, obj.VehicleSide, obj.ClusterStrength, obj.ClusterSize, RFIDData.TagId);
        }

        public List<TagObjectLocation> CombineObjectRFIDNew(List<ObjectLocation> objects, List<TagPeak> RFIDData,bool finish)
        {
            List<TagObjectLocation> finalObjects = new List<TagObjectLocation>();

            if (RFIDData != null)
            {
                if (RFIDData != null)
                {
                    //combine all RFID data with LocationObj's
                    for (int j = 0; j < RFIDData.Count; j++)
                    {
                        int bestFit = -1;
                        long firstPeakTime = RFIDData[j].FirstPeak.Time;
                        long lastPeakTime = RFIDData[j].LastPeak.Time;

                        if (firstPeakTime > lastPeakTime)
                        {
                            firstPeakTime = lastPeakTime;
                            lastPeakTime = RFIDData[j].FirstPeak.Time;
                        }

                        // during acquisition, combine RFID with best fit LocationObj.
                        if (!finish && objects.Count > 0 && objects[objects.Count - 1].Time.Ticks - lastPeakTime > 40000000)
                        {
                            //firstPeakTime -= 25000000;
                            //lastPeakTime += 25000000;

                            //increase range in which an object can be found & calculate midpoint
                            firstPeakTime -= 2500000;
                            lastPeakTime += 2500000;
                            long midpoint = (long)((lastPeakTime + firstPeakTime) * 0.5);

                            //loop through all objects to find the best fit
                            for (int i = 0; i < objects.Count; i++)
                            {
                                long objTime = objects[i].Time.Ticks;
                                if (objTime >= firstPeakTime && objTime <= lastPeakTime)    //object within applicable range
                                {
                                    if (bestFit == -1) //set inital best fit object
                                        bestFit = i;
                                    else
                                    {
                                        if (Math.Abs(objects[i].Time.Ticks - midpoint) < Math.Abs(objects[bestFit].Time.Ticks - midpoint)) //object is better fit that previous
                                            bestFit = i;
                                    }
                                }
                            }
                            if (bestFit >= 0)
                            {
                                finalObjects.Add(CombineFinalObjectDataNew(objects[bestFit], RFIDData[j]));   // adds new combined cluster set to list
                                objects.RemoveAt(bestFit);  //remove combined data to eliminate redundancies
                                RFIDData.RemoveAt(j--);
                            }
                        }
                        //after acquisition, combine tags with any object that fits
                        else if(finish && objects.Count > 0)
                        {
                            //increase range in which an object can be found & calculate midpoint
                            firstPeakTime -= 25000000;
                            lastPeakTime += 25000000;

                            //firstPeakTime -= 10000000;
                            //lastPeakTime += 10000000;

                            for (int i = 0; i < objects.Count; i++)
                            {
                                long objTime = objects[i].Time.Ticks;
                                if (objTime >= firstPeakTime && objTime <= lastPeakTime)    // object is within the applicable range
                                {
                                    finalObjects.Add(CombineFinalObjectDataNew(objects[i], RFIDData[j]));   // adds new combined cluster set to list
                                    objects.RemoveAt(i);
                                    RFIDData.RemoveAt(j--);
                                    break;
                                }
                            }
                        }
                    }

                }
            }

            return finalObjects;
            #endregion
        }
    }
}
