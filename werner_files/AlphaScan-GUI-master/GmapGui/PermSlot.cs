using System;
using System.Diagnostics;

namespace ParkDS
{
    /// <summary>
    /// Parking space permission time slot. To be matched with a <see cref="Permission"/> object.
    /// </summary>
    public class PermSlot: IComparable<PermSlot>
    {
        /// <summary>
        /// Create default permission time slot.
        /// </summary>
        public PermSlot()
        {
            Name = "NPR";
            ValidTimes = new DateTime[] { new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0,0,0),
                new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 0, 0) };
        }

        /// <summary>
        /// Create permission time slot with specified name and valid times.
        /// </summary>
        /// <param name="nombre">Name of permission. This should match the name of a <see cref="Permission"/> object.</param>
        /// <param name="valTime">Time slot when permission is valid. In form: {Beginning time, Ending time}.</param>
        public PermSlot(string nombre, DateTime[] valTime)
        {
            Name = nombre;
            ValidTimes = valTime;
        }

        /// <summary>
        /// Name of permission. This should match the name of a <see cref="Permission"/> object.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Time slot when permission is valid. In form: {Beginning time, Ending time}.
        /// </summary>
        public DateTime[] ValidTimes { get; set; }

        /// <summary>
        /// Compare permission slots using the beginning times for each slot. If beginning times match, compare end times.
        /// </summary>
        /// <param name="other">Other permission slot to compare against.</param>
        /// <returns>Whether this instance precedes, follows, or appears in the same position 
        /// as <paramref name="other"/>. Less than zero if precedes; equal to 0 if same 
        /// position; greater than 0 if follows or <paramref name="other"/> is null.</returns>
        public int CompareTo(PermSlot other)
        {
            if (this.ValidTimes[0].CompareTo(other.ValidTimes[0]) == 0)
                return this.ValidTimes[1].CompareTo(other.ValidTimes[1]);
            return this.ValidTimes[0].CompareTo(other.ValidTimes[0]);
        }

       
        /// <summary>
        /// Print data about the object to the console.
        /// </summary>
        public void Print()
        {
            Debug.WriteLine("Permit: {0} Time: {1} - {2}",this.Name,this.ValidTimes[0],this.ValidTimes[1]);               
        }
    }
}
