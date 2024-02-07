using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace ParkDS
{
    /// <summary>
    /// Class of utility functions for the parking data structure.
    /// </summary>
    class Utilities
    {
        /// <summary>
        /// Assess if two given line segments intersect.
        /// </summary>
        /// <param name="p">Starting coordinate of line 1.</param>
        /// <param name="p2">Ending coordinate of line 1.</param>
        /// <param name="q">Starting coordinate of line 2.</param>
        /// <param name="q2">Ending coordinate of line 2.</param>
        /// <param name="intersection">Output: the coordinates where the intersection is.</param>
        /// <param name="considerCollinearOverlapAsIntersect">Whether collinear segments are considered overlapping.</param>
        /// <returns>Whether the two segments intersect.</returns>
        public static bool LineSegmentsIntersect(Vector p, Vector p2, Vector q, Vector q2, 
            out Vector intersection, bool considerCollinearOverlapAsIntersect = false)
        {
            intersection = new Vector();

            var r = p2 - p;
            var s = q2 - q;
            var rxs = r.Cross(s);
            var qpxr = (q - p).Cross(r);

            // If r x s = 0 and (q - p) x r = 0, then the two lines are collinear.
            if (rxs.IsZero() && qpxr.IsZero())
            {
                // 1. If either  0 <= (q - p) * r <= r * r or 0 <= (p - q) * s <= * s
                // then the two lines are overlapping,
                if (considerCollinearOverlapAsIntersect)
                    if ((0 <= (q - p) * r && (q - p) * r <= r * r) || (0 <= (p - q) * s && (p - q) * s <= s * s))
                        return true;

                // 2. If neither 0 <= (q - p) * r = r * r nor 0 <= (p - q) * s <= s * s
                // then the two lines are collinear but disjoint.
                // No need to implement this expression, as it follows from the expression above.
                return false;
            }

            // 3. If r x s = 0 and (q - p) x r != 0, then the two lines are parallel and non-intersecting.
            if (rxs.IsZero() && !qpxr.IsZero())
                return false;

            // t = (q - p) x s / (r x s)
            var t = (q - p).Cross(s) / rxs;

            // u = (q - p) x r / (r x s)

            var u = (q - p).Cross(r) / rxs;

            // 4. If r x s != 0 and 0 <= t <= 1 and 0 <= u <= 1
            // the two line segments meet at the point p + t r = q + u s.
            if (!rxs.IsZero() && (0 <= t && t <= 1) && (0 <= u && u <= 1))
            {
                // We can calculate the intersection point using either t or u.
                intersection = p + t * r;

                // An intersection was found.
                return true;
            }

            // 5. Otherwise, the two line segments are not parallel but do not intersect.
            return false;
        }

        //Using a list of permissions and the times that they are permitted within the lot, generate a list of permissions based on time.
        /// <summary>
        /// Generates a full permission time table. Takes permission time slots from 
        /// the given list and fills in the gaps with the free permission.
        /// </summary>
        /// <param name="permList">List of permissions to start with.</param>
        /// <returns>New permission time table with gaps filled. If an error occurs, 
        /// returns time table that is only the free permission.</returns>
        public static List<PermSlot> GeneratePermissionList(List<PermSlot> permList)
        {
            //sort incoming permissions and create initial variables
            permList.Sort();

            string freePerm = "NPR";
            List<PermSlot> finalList = new List<PermSlot>();

            //determine if the given permission list is valid
            for (int i = 1; i < permList.Count; i++)
            {
                if (permList[i-1].ValidTimes[1] > permList[i - 1].ValidTimes[1])
                {
                    //If time slots overlap, return list of just the free permission.
                    Debug.WriteLine("Overlapping time periods are not supported");
                    return new List<PermSlot> { new PermSlot(freePerm, new DateTime[] { new DateTime(0,0,0,0,0,0), new DateTime(0,0,0,23,0,0)}) };
                }
            }
            
            //if the first permission does not start at midnight, create NPR slot until the first slot.
            DateTime regionStart = permList[0].ValidTimes[0];
            if (finalList.Count == 0 && regionStart.Hour > 0)
            {
                AddPermit(finalList, new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,0,0,0) , regionStart, freePerm);
            }

            //run through the entire list of permissions, adding NPR slots between time slots if the start & ending times do not align
            foreach (PermSlot p in permList)
            {
                regionStart = p.ValidTimes[0];
                if (finalList.Count > 0 && finalList[finalList.Count - 1].ValidTimes[1] < regionStart)
                    AddPermit(finalList, finalList[finalList.Count - 1].ValidTimes[1], regionStart, freePerm);
                AddPermit(finalList, regionStart, p.ValidTimes[1], p.Name);
            }

            //if the final timeslot does not end at midnight, create a NPR slot from the end of the last slot to midnight
            if (finalList[finalList.Count - 1].ValidTimes[1].Hour < 23)
            {
                AddPermit(finalList, finalList[finalList.Count - 1].ValidTimes[1], new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23,0,0), freePerm);
            }
            return finalList;
        }

        //Add permission timeslot to a space, copying the current list of permitted tags into the timeslot
        /// <summary>
        /// Add a permission time slot to a list.
        /// </summary>
        /// <param name="finalList">List to add to.</param>
        /// <param name="regionStart">Start of time slot.</param>
        /// <param name="regionEnd">End of time slot.</param>
        /// <param name="perms">Name of permission.</param>
        private static void AddPermit(List<PermSlot> finalList, DateTime regionStart, DateTime regionEnd, string perms)
        {
            finalList.Add(new PermSlot(perms, new DateTime[] { regionStart, regionEnd }));
        }
    }
}
