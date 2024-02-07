using System.Collections.Generic;

namespace CES.AlphaScan.Rfid
{
    /// <summary>
    /// Basic tag requirements for an ID to even be considered. Used to determine if company code and year code match.
    /// </summary>
    public class TagRequirements
    {
        /// <summary>
        /// Small substring required in tag ID. Ex. company code.
        /// </summary>
        public string TagCode { get; set; }

        /// <summary>
        /// Index in ID of where to start looking for <see cref="TagCode"/>. 
        /// Uses length of <see cref="TagCode"/> to know where to stop looking.
        /// Note: This counts spaces, if they are there.
        /// </summary>
        public int TagCodeIdx { get; set; }

        /// <summary>
        /// Small substring required in tag ID. Matches the year.
        /// </summary>
        public string YearCode { get; set; }

        /// <summary>
        /// Index in ID of where to start looking for <see cref="YearCode"/>. 
        /// Uses length of <see cref="YearCode"/> to know where to stop looking.
        /// Note: This counts spaces, if they are there.
        /// </summary>
        public int YearCodeIdx { get; set; }

        /// <summary>
        /// Empty constructor. No useful default.
        /// </summary>
        public TagRequirements()
        {
            TagCode = "";
            TagCodeIdx = 0;
            YearCode = "";
            YearCodeIdx = 0;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="objectToCopy">Object to copy.</param>
        public TagRequirements(TagRequirements objectToCopy)
        {
            TagCode = objectToCopy.TagCode;
            TagCodeIdx = objectToCopy.TagCodeIdx;
            YearCode = objectToCopy.YearCode;
            YearCodeIdx = objectToCopy.YearCodeIdx;
        }

        /// <summary>
        /// Basic constructor when given each property value.
        /// </summary>
        /// <param name="tagCode"></param>
        /// <param name="tagCodeIdx"></param>
        /// <param name="yearCode"></param>
        /// <param name="yearCodeIdx"></param>
        public TagRequirements(string tagCode, int tagCodeIdx, string yearCode, int yearCodeIdx)
        {
            TagCode = tagCode;
            TagCodeIdx = tagCodeIdx;
            YearCode = yearCode;
            YearCodeIdx = yearCodeIdx;
        }

        /// <summary>
        /// Parsing constructor. Parses dictionary of values to hopefully find the correct ones.
        /// No exception handling for failed parsing or dictionary referencing.
        /// </summary>
        /// <param name="values">Dictionary of values to parse through to find values.</param>
        public TagRequirements(IDictionary<string,string> values)
        {
            TagCode = values["TagCode"];
            TagCodeIdx = int.Parse(values["TagCodeIdx"]);
            YearCode = values["YearCode"];
            YearCodeIdx = int.Parse(values["YearCodeIdx"]);
        }

    }
}
