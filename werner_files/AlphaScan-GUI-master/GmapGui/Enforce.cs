using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace ParkDS
{
    class Enforce
    {
        /// <summary>
        /// Determine if a tag is valid for a lot using its detected time and position.
        /// </summary>
        /// <param name="space">Space to determine validity of. Checks against tag in this space.</param>
        /// <param name="day">Day of the week. 0 = Monday to 6 = Sunday.</param>
        /// <param name="permitManage">Permit manager to use for determining permissions for validity check.</param>
        public static void DetermineValidity(ParkZone space, int day, PermManager permitManage)
        {
            //List<PermSlot> perm = space.Permissions;
            DateTime time = space.TagWithin.Time;

            //$? I'm not sure if the DateTime struct will ever allow this. May be unnecessary check.
            if (time.Hour >= 24)
            {
                Debug.WriteLine("Invalid Time");
                return;
            }

            string key = space.TagWithin.ID;

            //load relevant permission due to the time and the parking space
            int timeSlot = TimeValid(time, space.Permissions, 0, space.Permissions.Count);
            Permission relevantPerm = permitManage.FindPerm(space.Permissions[timeSlot].Name);
            if (relevantPerm == null)
            {
                return;
            }
            //compare ID to that of the relevant permission
            space.TagWithin.Valid = (((key.CompareTo(relevantPerm.Keys[0]) >= 0) && (key.CompareTo(relevantPerm.Keys[1]) <= 0)) || space.Permissions[timeSlot].Name.Equals("NPR"));
        }

        /// <summary>
        /// search space's permission list to determine the appropriate permission with which to determine validity
        /// </summary>
        /// <param name="time">Time that the tag was read</param>
        /// <param name="timeslots">List of timeslots to search</param>
        /// <param name="start">first slot to look at</param>
        /// <param name="last">last slot to look at</param>
        /// <returns>the index of the relevant permission slot</returns>
        private static int TimeValid(DateTime time, List<PermSlot> timeslots, int start, int last)
        {
            int mid = (start + last) / 2;

            int endTimeVal = timeslots[mid].ValidTimes[1].Hour;
            if (timeslots[mid].ValidTimes[1].Hour < timeslots[mid].ValidTimes[0].Hour)
                endTimeVal += 24;

            if (timeslots[mid].ValidTimes[0].Hour <= time.Hour && endTimeVal > time.Hour)
                return mid;
            else if (timeslots[mid].ValidTimes[0].Hour == time.Hour)
            {
                if (timeslots[mid].ValidTimes[0].Minute <= time.Hour)
                    return mid;
            }
            else if (endTimeVal == time.Hour)
            {
                if (timeslots[mid].ValidTimes[1].Minute > time.Hour)
                    return mid;
            }
            else if (timeslots[mid].ValidTimes[0].Hour > time.Hour)
                return TimeValid(time, timeslots, start, mid - 1);
            else
                return TimeValid(time, timeslots, mid + 1, last);

            // UNSURE IF THIS IS RIGHT
            return -1;
        }
    }
}

