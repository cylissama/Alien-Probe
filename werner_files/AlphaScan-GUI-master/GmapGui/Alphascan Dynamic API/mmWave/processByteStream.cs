namespace CES.AlphaScan.mmWave
{
    /// <summary>
    /// Contains methods for parsing the mmWave sensor byte stream into a usable data type.
    /// </summary>
    public class ProcessByteStream
    {
        /*
            These functions analyze the headers of the mmWave packets to get fundamental information of the mmWAve packets. The header data should remain mostly constant
            for these tests, and the numTLVs is always desired to be 1. TLV 4 from the found TLVs contain Azimuth Heatmap data which is the data type that the future procesors
            have been created for.
        */ 
         

        #region variables
        // DEFINE VARIABLES
        public static AzimuthProcessing azimuth = new AzimuthProcessing();

        int[] q = new int[4] { 1, 256, 65536, 16777216 };  // create q vector

        /// <summary>
        /// structure of header data in order of types
        /// </summary>
        struct HeaderData        // struct of header data
        {
            public int version;
            public int packetLength;
            public int platform;
            public int frameNum;
            public int cpuCycles;
            public int numDetObj;
            public int numTLVs;
            public int subFrameNum;
        };

        InterpolationData[] processedPacket = new InterpolationData[1];

        HeaderData header = new HeaderData();   // define header struct

        struct TLVData      // struct of TLV data
        {
            public int type;
            public int length;
        }

        TLVData TLV = new TLVData();    // define TLV struct
        #endregion

        /// <summary>
        /// begins analyzing packet, gets initial metadata of the packet
        /// </summary>
        /// <param name="byteVecMatrix">matrix of data to read from</param>
        /// <returns>data to prepare to interpolate</returns>
        public InterpolationData[] AnalyzePacket(byte[] byteVecMatrix)
        {
            int idx = 0;

            idx = GetHeader(byteVecMatrix, idx);    // find all header values
            for (int TLVidx = 0; TLVidx < header.numTLVs; TLVidx++)
            {
                idx = GetTLVType(byteVecMatrix, idx);       // find TLV types
                
                if (TLV.type == 4)
                {
                   

                    processedPacket = azimuth.ProcessAzimuthHeatMap(byteVecMatrix, idx);
                    idx += TLV.length;
                }
            }
            return processedPacket;
        }

        #region headerData

        /// <summary>
        /// gets metadata of the packet to assist in processing
        /// </summary>
        /// <param name="byteVecMatrix">matrix to search from to find the header data from</param>
        /// <param name="idx">index of the beginning of the packet</param>
        /// <returns></returns>
        private int GetHeader(byte[] byteVecMatrix, int idx)
        {
            // find header values using element wise multiplication with q
            idx += 8;
            header.version = (byteVecMatrix[idx + 0] * q[0]) + (byteVecMatrix[idx + 1] * q[1]) + (byteVecMatrix[idx + 2] * q[2]) + (byteVecMatrix[idx + 3] * q[3]);
            idx += 4;
            header.packetLength = (byteVecMatrix[idx + 0] * q[0]) + (byteVecMatrix[idx + 1] * q[1]) + (byteVecMatrix[idx + 2] * q[2]) + (byteVecMatrix[idx + 3] * q[3]);
            idx += 4;
            header.platform = (byteVecMatrix[idx + 0] * q[0]) + (byteVecMatrix[idx + 1] * q[1]) + (byteVecMatrix[idx + 2] * q[2]) + (byteVecMatrix[idx + 3] * q[3]);
            idx += 4;
            header.frameNum = (byteVecMatrix[idx + 0] * q[0]) + (byteVecMatrix[idx + 1] * q[1]) + (byteVecMatrix[idx + 2] * q[2]) + (byteVecMatrix[idx + 3] * q[3]);
            idx += 4;
            header.cpuCycles = (byteVecMatrix[idx + 0] * q[0]) + (byteVecMatrix[idx + 1] * q[1]) + (byteVecMatrix[idx + 2] * q[2]) + (byteVecMatrix[idx + 3] * q[3]);
            idx += 4;
            header.numDetObj = (byteVecMatrix[idx + 0] * q[0]) + (byteVecMatrix[idx + 1] * q[1]) + (byteVecMatrix[idx + 2] * q[2]) + (byteVecMatrix[idx + 3] * q[3]);
            idx += 4;
            header.numTLVs = (byteVecMatrix[idx + 0] * q[0]) + (byteVecMatrix[idx + 1] * q[1]) + (byteVecMatrix[idx + 2] * q[2]) + (byteVecMatrix[idx + 3] * q[3]);
            idx += 4;
            header.subFrameNum = (byteVecMatrix[idx + 0] * q[0]) + (byteVecMatrix[idx + 1] * q[1]) + (byteVecMatrix[idx + 2] * q[2]) + (byteVecMatrix[idx + 3] * q[3]);
            idx += 4;

            return idx;
        }

        #endregion

        #region TLVTypes

        /// <summary>
        /// converts TLV data in packet into usable data
        /// </summary>
        /// <param name="byteVecMatrix">list of data to read from</param>
        /// <param name="idx">index of TLV type</param>
        /// <returns>numerical type of TLV</returns>
        private int GetTLVType(byte[] byteVecMatrix, int idx)
        {
            TLV.type = (byteVecMatrix[idx] * q[0]) + (byteVecMatrix[idx + 1] * q[1]) + (byteVecMatrix[idx + 2] * q[2]) + (byteVecMatrix[idx + 3] * q[3]);
            idx += 4;
            TLV.length = (byteVecMatrix[idx] * q[0]) + (byteVecMatrix[idx + 1] * q[1]) + (byteVecMatrix[idx + 2] * q[2]) + (byteVecMatrix[idx + 3] * q[3]);
            idx += 4;

            return idx;
        }

        #endregion
    }
}
