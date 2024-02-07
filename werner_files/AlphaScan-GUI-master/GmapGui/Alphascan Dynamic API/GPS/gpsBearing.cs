using System;

namespace CES.AlphaScan.Gps
{
    public static class GpsBearing
    {
        /*
           Find bearing of GPS data using these functions. This is used for data combination of mmWave and GPS data to find the rotation needed for the mmWave
           data to accurately gelocate the objects onto the map. These can be achieved using getting an initial angle which uses two sets of GPS data to find a
           bearing angle. Haversine is used similarly but instead can be calculated using the distance between two GPS points for the bearing angle.
       */

        /// <summary>
        /// gets initial angle of GPS bearing using two data sets
        /// </summary>
        /// <param name="lat1">latitude of first packet</param>
        /// <param name="lng1">longitude of first packet</param>
        /// <param name="lat2">latitude of second packet</param>
        /// <param name="lng2">longitude of second packet</param>
        /// <returns>theta value of the angle</returns>
        public static double GetInitialAngle(double lat1, double lng1, double lat2, double lng2)
        {
            lat1 = DegToRad(lat1);
            lng1 = DegToRad(lng1);
            lat2 = DegToRad(lat2);
            lng2 = DegToRad(lng2);
            // find change in lat/long
            double deltLat = lat2 - lat1;
            double deltLng = lng2 - lng1;
            // calcualte the theta angle for the change in rotation
            double theta = Math.Atan2(Math.Sin(deltLng) * Math.Cos(lat2), Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(deltLng));
            return RadToDeg(theta);
        }

        /// <summary>
        /// Haversine value using two data sets, for testing.
        /// Finds the distance in miles between two latlng points.
        /// </summary>
        /// <param name="lat1">latitude of first packet</param>
        /// <param name="lng1">longitude of first packet</param>
        /// <param name="lat2">latitude of second packet</param>
        /// <param name="lng2">longitude of second packet</param>
        /// <returns>theta haversine value</returns>
        public static double Haversine(double lat1, double lng1, double lat2, double lng2)
        {
            // dist calculation in feet
            double theta = lng1 - lng2;
            // find the distance change in miles
            double dist = Math.Sin(DegToRad(lat1)) * Math.Sin(DegToRad(lat2)) + Math.Cos(DegToRad(lat1)) * Math.Cos(DegToRad(lat2)) * Math.Cos(DegToRad(theta));
            return Math.Acos(dist) * 3958.8;        // radius earth miles, dist *= 5280 miles to feet
        }

        /// <summary>
        /// Conversion from degrees to radians.
        /// </summary>
        /// <param name="input">a degree value</param>
        /// <returns>radian version of input</returns>
        private static double DegToRad(double input)
        {
            return input * (Math.PI / 180);
        }

        /// <summary>
        /// Conversion from radians to degrees.
        /// </summary>
        /// <param name="input">a radian value</param>
        /// <returns>degree version of input</returns>
        private static double RadToDeg(double input)
        {
            return input * (180 / Math.PI);
        }
    }
}
