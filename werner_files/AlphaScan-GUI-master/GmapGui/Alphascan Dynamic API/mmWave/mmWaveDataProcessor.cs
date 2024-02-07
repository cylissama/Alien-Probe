using System.Collections.Generic;
using CES.AlphaScan.Base;

namespace CES.AlphaScan.mmWave
{
    public class mmWaveDataProcessor
    {
        // init of all different classes needed to access
        readonly RightClustering rClustering = new RightClustering();
        readonly LeftClustering lClustering = new LeftClustering();
        readonly ProcessByteStream process = new ProcessByteStream();
        
        /// <summary>
        /// Root for finding mmWave clusters, needs to check vehicle side
        /// </summary>
        /// <param name="data">Packet of unprocessed mmWave data to cluster</param>
        /// <param name="vehicleSide">Side of vehicle being analyzed</param>
        /// <returns></returns>
        public List<ClusteredObject> FindmmWaveClusters(PacketData data, VehicleSide vehicleSide)
        {
            List<ClusteredObject> tempClusters;

            if (vehicleSide.Equals((VehicleSide)1)) // right side
            {
                tempClusters = rClustering.ClusterAroundMax(process.AnalyzePacket(data.FullPacket), data);  // find the temporary clusters
            }
            else // left side
            {
                tempClusters =  lClustering.ClusterAroundMax(process.AnalyzePacket(data.FullPacket), data); // find the temporary clsuters
            }

            List<ClusteredObject> clusterTime = new List<ClusteredObject>();    // correlate all of the clusters found with their valid times

            foreach (ClusteredObject item in tempClusters)   // adds each found cluster with time
            {
                clusterTime.Add(new ClusteredObject(item.X, item.Y, data.Time, item.VehicleSide, item.ClusterStrength, item.ClusterSize));  // get cluster with a time value
            }

            return clusterTime;
        }

    }
}
