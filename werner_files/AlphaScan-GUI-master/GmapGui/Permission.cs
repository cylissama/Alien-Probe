using System;
using System.Collections.Generic;
using System.Diagnostics;
namespace ParkDS
{
    /// <summary>
    /// Contains data about a parking permission. Matches name of 
    /// the permission with rules about which RFID tag IDs are valid.
    /// </summary>
    public class Permission : IComparable<Permission>
    {
        /// <summary>
        /// Create default permission.
        /// </summary>
        public Permission()
        {
            Name = "NPR";
            Keys = new string[2] { "00000", "00000" }; 
            AddPermits = null;
        }

        /// <summary>
        /// Create permission with specified values.
        /// </summary>
        /// <param name="nombre">Name of the permission.</param>
        /// <param name="permKey">Range of RFID tag IDs that are valid. In form: {Lowest ID, Highest ID}.</param>
        /// <param name="additionalPermits"></param>
        public Permission(string nombre, string[] permKey, List<string> additionalPermits)
        {
            Name = nombre;
            Keys = permKey;
            AddPermits = additionalPermits;
        }

        /// <summary>
        /// Name of the permission.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Range of RFID tag IDs that are valid. In form: {Lowest ID, Highest ID}.
        /// </summary>
        public string[] Keys { get; set; }

       /// <summary>
       /// Addtional permits which should be considered valid in the space
       /// </summary>
        public List<string> AddPermits { get; set; }

        /// <summary>
        /// Compare <see cref="Permission"/> objects by name, alphabetically.
        /// </summary>
        /// <param name="other">The <see cref="Permission"/> to compare against.</param>
        /// <returns>Whether this instance precedes, follows, or appears in the same position 
        /// as <paramref name="other"/>. Less than zero if precedes; equal to 0 if same 
        /// position or <paramref name="other"/> is null; greater than 0 if follows.</returns>
        public int CompareTo(Permission other)
        {
            if (other == null)
                return 0;

            return this.Name.CompareTo(other.Name);
        }

        /// <summary>
        /// Determines whether <see cref="Permission"/> objects are the same by comparing <see cref="Name"/> values.
        /// </summary>
        /// <param name="other">The <see cref="Permission"/> to compare against.</param>
        /// <returns>Whether the two <see cref="Permission"/> objects have the same name.</returns>
        public bool Equals(Permission other)
        {
            return this.Name.Equals(other.Name);
        }

        /// <summary>
        /// Print information about this object to the debug console.
        /// </summary>
        public void Print()
        {
            Debug.Write("Name: " + this.Name);
            Debug.Write("  Key: " + this.Keys);
            Debug.Write("  Other Allowed Permits: [");
            foreach (string s in this.AddPermits)
                Debug.Write(" " + s);
            Debug.WriteLine(" ]");
        }
    }
}