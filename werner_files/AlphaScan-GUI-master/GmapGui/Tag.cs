using System;

namespace ParkDS
{
    /// <summary>
    /// Object representing an RFID tag in a parking space.
    /// </summary>
    public class Tag
    {
        /// <summary>
        /// ID of the RFID tag.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Whether the RFID tag is valid for the current location and time.
        /// </summary>
        public bool Valid { get; set; }

        /// <summary>
        /// Time the tag was read in the current location.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Create default tag.
        /// </summary>
        public Tag()
        {
            ID = "0000 0000 0000 0000 0000";
            Valid = false;
            Time = new DateTime(0);
        }

        /// <summary>
        /// Create tag using actual values.
        /// </summary>
        /// <param name="ident">RFID tag ID.</param>
        /// <param name="val">Whether tag is valid.</param>
        /// <param name="thyme">Time the tag was read.</param>
        public Tag(string ident, bool val, DateTime thyme)
        {
            ID = ident;
            Valid = val;
            Time = thyme;
        }
    }
}
