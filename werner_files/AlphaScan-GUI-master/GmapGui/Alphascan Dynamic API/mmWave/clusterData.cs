using System;
using System.Collections.Generic;
using CES.AlphaScan.Base;

namespace CES.AlphaScan.mmWave
{
    /*
        Functions for analyzing interpolated heatmap data to find the clusters within the packet. Usually we use the heatmap max strength to be the point to cluster around.
        This is implemented by using a sweep of strength checks around the maximum packet point, if it is within range, to see if there is a cluster present, meaning that 
        it passes a cluster size thresholding check. This data is combined into a single cluster and is exported for combination with GPS data to create a geolcated object.
    */

    /// <summary>
    /// Methods for clustering mmWave objects on the close (right) vehicle side.
    /// </summary>
    public class RightClustering
    {
        #region static vars
        // default clustering thresholding
        static int minClusterSize = 5;
        static int minStrTresh = 200;
        #endregion

        #region clustering around max
        /// <summary>
        /// Find right side clustering that is based around strongest heatmap from packet, is the vehicle if one is present
        /// </summary>
        /// <param name="input">An input array of processed heatmap data</param>
        /// <param name="packet">Information of input packet, e.g. time</param>
        /// <returns></returns>
        public List<ClusteredObject> ClusterAroundMax(InterpolationData[] input, PacketData packet)
        {
            
            List<InterpolationData> filterPoints = new List<InterpolationData>();
            List<ClusteredObject> clusterList = new List<ClusteredObject>();

            if (input.Length == 0)
                return clusterList;

            // get differential distance between points
            double dx = Math.Abs(input[200].X - input[0].X);
            double dy = Math.Abs(input[1].Y - input[0].Y);

            // Checks to see whether in bounds of right side
            for (int i = 0; i < input.Length; i++)
            {
                // the right side bounds will be from 0.25 to 4 m in the Y direction and limited to within 2 and -2 meters on the X direction of the grid
                if (!double.IsNaN(input[i].Strength) && Math.Abs(input[i].X) < 2 &&
                    input[i].Y < 4.5 && input[i].Y > 0.25)
                {
                    // if the strength is above the threshold, filter it to the next stage
                    if (input[i].Strength > minStrTresh)
                        filterPoints.Add(input[i]);
                }
                else
                    continue;
            }

            if (filterPoints.Count == 0)
                return clusterList;

            // find max and minimum value for the packet
            InterpolationData max = FindHeatmapMax(filterPoints);
            // sensitivity of the system to see if point strength is in fact valid
            double sensitivity = minStrTresh * 0.5;
            double idxStr = 0;

            
            List<int> clusterIdx = new List<int>();
            // checks for points within a 9 size box around the max point, if points above heat sensitivity, add idx to possibl cluster list
            for (int i = 0; i < filterPoints.Count; i++)
            {
                // check within a 9x9 grid around the poitn with its points to see if the number
                if ((max.X + (dx * -3)) <= filterPoints[i].X && (max.X + (dx * 3)) >= filterPoints[i].X)
                {
                    if ((max.Y + (dy * -3)) <= filterPoints[i].Y && (max.Y + (dy * 3)) >= filterPoints[i].Y)
                    {
                        // see if the strength is above the sensitivity threadhold, if so add it to the cluster list for the next stage
                        if (filterPoints[i].Strength >= sensitivity)
                        {
                            clusterIdx.Add(i);
                            idxStr = (idxStr + filterPoints[i].Strength) * 0.5;
                        }
                    }
                }
            }

            // checks if possible points are more than min cluster size or if very high strength
            if (clusterIdx.Count >= minClusterSize)
            {
                // save the values to the temps to create the cluster objects
                double tmpX = filterPoints[clusterIdx[0]].X;
                double tmpY = filterPoints[clusterIdx[0]].Y;
                double tmpStr = filterPoints[clusterIdx[0]].Strength;
                int tmpSize = 1;
                for (int i = 1; i < clusterIdx.Count; i++)
                {
                    tmpX = filterPoints[clusterIdx[i]].X;
                    tmpY = filterPoints[clusterIdx[i]].Y;

                    tmpStr = (tmpStr + filterPoints[clusterIdx[i]].Strength);
                    tmpSize += 1;
                }
                clusterList.Add(new ClusteredObject(tmpX, tmpY, packet.Time, (VehicleSide)1, tmpStr / tmpSize, tmpSize));
            }
            

            return clusterList;
        }

        /// <summary>
        /// Find the max value of the heatmap 
        /// </summary>
        /// <param name="input">List of interpolated data per the packet</param>
        /// <returns>Maximum strength grid point within the frmae</returns>
        private InterpolationData FindHeatmapMax(List<InterpolationData> input)
        {
            InterpolationData max = input[0];
            foreach (InterpolationData data in input)
            {
                if (data.Strength > max.Strength)
                {
                    max = data;
                }
            }
            return max;
        }

        #endregion
    }

    /// <summary>
    /// Methods for clustering mmWave objects on the far (left) vehicle side.
    /// </summary>
    public class LeftClustering
    {
        // default clustering thresholding
        #region static vars
        static int minClusterSize = 1;
        static int minStrTresh = 75;
        #endregion

        #region clustering around max

        /// <summary>
        /// Find left side clustering that is based around strongest heatmap from packet, is the vehicle if one is present
        /// </summary>
        /// <param name="input">An input array of processed heatmap data</param>
        /// <param name="packet">Information of input packet, e.g. time</param>
        /// <returns></returns>
        public List<ClusteredObject> ClusterAroundMax(InterpolationData[] input, PacketData packet)
        {
            // see right side comments for information, works functionally the same just slight thresholding differences
            List<InterpolationData> filterPoints = new List<InterpolationData>();
            List<ClusteredObject> clusterList = new List<ClusteredObject>();

            if (input.Length == 0)
                return clusterList;

            // get differential distance between points
            double dx = Math.Abs(input[200].X - input[0].X);
            double dy = Math.Abs(input[1].Y - input[0].Y);

            for (int i = 0; i < input.Length; i++)
            {
                // set bounds for clustering up close
                if (!double.IsNaN(input[i].Strength) && Math.Abs(input[i].X) < 2 &&
                    input[i].Y < 10 && input[i].Y > 0.5)
                {
                    if (input[i].Strength > minStrTresh)
                        filterPoints.Add(input[i]);
                }
                else
                    continue;
            }

            if (filterPoints.Count == 0)
                return clusterList;

            InterpolationData max = FindHeatmapMax(filterPoints);
            double sensitivity = minStrTresh * 0.5;
            double idxStr = 0;


            List<int> clusterIdx = new List<int>();

            for (int i = 0; i < filterPoints.Count; i++)
            {
                if ((max.X + (dx * -3)) <= filterPoints[i].X && (max.X + (dx * 3)) >= filterPoints[i].X)
                {
                    if ((max.Y + (dy * -3)) <= filterPoints[i].Y && (max.Y + (dy * 3)) >= filterPoints[i].Y)
                    {
                        if (filterPoints[i].Strength >= sensitivity)
                        {
                            clusterIdx.Add(i);
                            idxStr = (idxStr + filterPoints[i].Strength) * 0.5;
                        }
                    }
                }
            }

            if (clusterIdx.Count >= minClusterSize)
            {
                double tmpX = filterPoints[clusterIdx[0]].X;
                double tmpY = filterPoints[clusterIdx[0]].Y;
                double tmpStr = filterPoints[clusterIdx[0]].Strength;
                int tmpSize = 1;
                for (int i = 1; i < clusterIdx.Count; i++)
                {
                    tmpX = filterPoints[clusterIdx[i]].X;
                    tmpY = filterPoints[clusterIdx[i]].Y;


                    tmpStr = (tmpStr + filterPoints[clusterIdx[i]].Strength);
                    tmpSize += 1;
                }

                clusterList.Add(new ClusteredObject(tmpX, tmpY, packet.Time, (VehicleSide)0, tmpStr / tmpSize, tmpSize));
            }

            return clusterList;
        }

        private InterpolationData FindHeatmapMax(List<InterpolationData> input)
        {
            InterpolationData max = input[0];
            foreach (InterpolationData data in input)
            {
                if (data.Strength > max.Strength)
                {
                    max = data;
                }
            }
            return max;
        }

        #endregion
    }

    /// <summary>
    /// Methods used for math in mmWave object clustering.
    /// </summary>
    public static class ClusteringMath
    {
        /// <summary>
        /// Get distance between two cluster points using distance formula
        /// </summary>
        /// <param name="tmpX">X coordiante of 1st cluster</param>
        /// <param name="tmpY">Y coordinate of 1st cluster</param>
        /// <param name="c2">2nd cluster to find distance from</param>
        /// <returns></returns>
        public static double ClusterDistance(double tmpX, double tmpY, ClusteredObject c2)
        {
            return Math.Sqrt((c2.X - tmpX) * (c2.X - tmpX) + (c2.Y - tmpY) * (c2.Y - tmpY));
        }
    }
}
