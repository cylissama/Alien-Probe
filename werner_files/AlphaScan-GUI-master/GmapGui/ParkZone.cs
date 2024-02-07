using System;
using System.Collections.Generic;
using System.Diagnostics;

/* Class representing the fundamental parking zone.
 * This includes: parking permissions, names for spaces, geographic area, and any
 * areas contained with in it.
 * 
 * Also implemented is the method for determining equivalency between parking areas
 */

namespace ParkDS
{
    /// <summary>
    /// Node in the parking area data hierarchy. Can represent campus, parking lot, parking space, etc..
    /// </summary>
    public class ParkZone : IComparable<ParkZone>
    {
        /// <summary>
        /// Create default parking zone.
        /// </summary>
        public ParkZone()
        {
            Permissions = new List<PermSlot>();
            
            Centroid = new double[] { 0, 0 };
            Vertices = new double[] { -90, -180, 90, -180, 90, 180, -90, 180 };
            Subareas = null;
            Name = "Earth";
            AreaType = "Parent";
            TagWithin = null;
        }

        /// <summary>
        /// Create parking zone with specified permissions, vertices, and name.
        /// </summary>
        /// <param name="perms">Permissions to use for this object.</param>
        /// <param name="vert">Vertices to use for this object. Counter clockwise. Alternating list {lat, lng, lat lng, ...} in degrees.</param>
        /// <param name="nombre">Name of the zone.</param>
        public ParkZone(List<PermSlot> perms, double[] vert, string nombre, string areaTy)
        {
            Permissions = perms;
            Vertices = vert;
            Subareas = null;
            Name = nombre;
            TagWithin = null;

            AreaType = areaTy;

            //average all the vertices to calculate centroid
            double[] cent = { 0, 0 };
            for (int i = 0; i < vert.Length / 2; i++)
            {
                cent[0] += vert[2 * i];
                cent[1] += vert[2 * i + 1];
            }
            cent[0] = cent[0] / (vert.Length / 2);
            cent[1] = cent[1] / (vert.Length / 2);
            Centroid = cent;
        }

        /// <summary>
        /// List of permissions enforced in this <see cref="ParkZone"/>.
        /// </summary>
        public List<PermSlot> Permissions { get; set; }

        /// <summary>
        /// Centroid of the area. Stored as {latitude, longitude} in degrees.
        /// </summary>
        public double[] Centroid { get; set; }

        /// <summary>
        /// Vertices of the area of the zone. Stored as alternating list of latitude and 
        /// longitude values in degrees (eg. {lat1, lng1, lat2, lng2, ...}). Going counter clockwise.
        /// </summary>
        public double[] Vertices { get; set; }

        /// <summary>
        /// List of subareas of this area.
        /// </summary>
        public List<ParkZone> Subareas { get; set; }

        /// <summary>
        /// Name of this zone.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of area that the lot is in from top to bottom: Parent, ParkingLot, SubArea, ParkingSpace
        /// </summary>
        public string AreaType { get; set; }

        /// <summary>
        /// The RFID tag within this zone.
        /// </summary>
        public Tag TagWithin { get; set; }

        /// <summary>
        /// Compare <see cref="ParkZone"/> objects by name. For alphabetizing lists.
        /// </summary>
        /// <param name="other">The <see cref="ParkZone"/> object to compare against.</param>
        /// <returns>Whether this instance precedes, follows, or appears in the same position 
        /// as <paramref name="other"/>. Less than zero if precedes; equal to 0 if same 
        /// position or <paramref name="other"/> is null; greater than 0 if follows.</returns>
        public int CompareTo(ParkZone other)
        {
            if (other == null)
                return 0;

            return this.Name.CompareTo(other.Name);
        }

        /// <summary>
        /// Determine if two zones are the same by comparing their centroids.
        /// </summary>
        /// <param name="comp">The <see cref="ParkZone"/> object to compare against.</param>
        /// <returns>Whether the two zones have the same centroid.</returns>
        public bool Equals(ParkZone comp)
        {
            return (Math.Abs(this.Centroid[0] - comp.Centroid[0]) < Math.Pow(10, -12)) && (Math.Abs(this.Centroid[1] - comp.Centroid[1]) < Math.Pow(10, -12));
        }

        //$$ I don't think the debug console works outside of VS. Should probably print somewhere else or as string.
        /// <summary>
        /// Print information about the chosen <see cref="ParkZone"/> object ($?as a string).
        /// </summary>
        public void Printer()
        {
            Debug.WriteLine("Name: " + this.Name);
            Debug.WriteLine("Centroid: [ {0} , {1} ]", this.Centroid[0], this.Centroid[1]);
            foreach (PermSlot p in this.Permissions)
                p.Print();
            if (this.TagWithin != null)
            {
                Debug.WriteLine("Tag: " + this.TagWithin.ID);
                if (this.TagWithin.ID != "0")
                    Debug.WriteLine("Validity: {0}", this.TagWithin.Valid);
            }
        }
    }
}