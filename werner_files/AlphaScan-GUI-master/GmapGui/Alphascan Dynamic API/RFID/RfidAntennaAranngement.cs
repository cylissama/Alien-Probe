using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.AlphaScan.Rfid
{
    /// <summary>
    /// Contains information regarding the arrangement of RFID antennas.
    /// </summary>
    public class AntennaArrangement
    {
        /// <summary>
        /// Constructs a new <see cref="AntennaArrangement"/> object from specified lists of antenna IDs.
        /// </summary>
        /// <param name="rightSide">IDs of the antennas on the right side of the vehicle. Ordered from front to back of vehicle.</param>
        /// <param name="leftSide">IDs of the antennas on the left side of the vehicle. Ordered from front to back of vehicle.</param>
        public AntennaArrangement(IList<int> rightSide, IList<int> leftSide)
        {
            _rightSide = rightSide.ToArray();
            _leftSide = leftSide.ToArray();
        }

        /// <summary>
        /// Copies an existing <see cref="AntennaArrangement"/> object.
        /// </summary>
        /// <param name="arrangement"><see cref="AntennaArrangement"/> object to copy.</param>
        public AntennaArrangement(AntennaArrangement arrangement)
        {
            _rightSide = arrangement.RightSide;
            _leftSide = arrangement.LeftSide;
        }

        // this is a hack, i have no idea how to do initialize this - murphey
        public AntennaArrangement()
        {
            // this is assuming 0 is the left antenna (from facing antenntas from back) and 1 is right antenna
            _rightSide = new int[1] { 0 };
            _leftSide = new int[1] { 1 };
        }

        /// <summary>
        /// Total number of antennas in this arrangement.
        /// </summary>
        public int NumberAntennas { get { return RightSide.Count + LeftSide.Count; } }

        /// <summary>
        /// IDs of the antennas on the right side of the vehicle. Ordered from front to back of vehicle.
        /// </summary>
        public IList<int> RightSide { get { return _rightSide.ToArray(); } }
        private IList<int> _rightSide = new int[2] { 0, 1 };

        /// <summary>
        /// IDs of the antennas on the leftt side of the vehicle. Ordered from front to back of vehicle.
        /// </summary>
        public IList<int> LeftSide { get { return _leftSide.ToArray(); } }
        private IList<int> _leftSide = new int[2] { 2, 3 };
    }




}
