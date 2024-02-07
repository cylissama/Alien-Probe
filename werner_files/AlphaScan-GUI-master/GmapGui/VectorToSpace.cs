using ParkDS;

namespace GmapGui
{
    class VectorToSpace
    {
        private double distance;
        private ParkZone spot;


        public double Distance { get => distance; set => distance = value; }
        public ParkZone Spot { get => spot; set => spot = value; }

        public VectorToSpace(double distanceto, ParkZone space) 
        {
            Distance = distanceto;
            Spot = space;
        }
    }
}
