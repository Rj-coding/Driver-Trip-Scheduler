using DriverTripBackendProject.Models;

namespace DriverTripBackendProject.Models
{
    public class Trip
    {
        public int TripId { get; set; }

        public int OriginCityId { get; set; }
        public City OriginCity { get; set; }

        public int OriginAreaId { get; set; }
        public Area OriginArea { get; set; }

        public int DestinationCityId { get; set; }
        public City DestinationCity { get; set; }

        public int DestinationAreaId { get; set; }
        public Area DestinationArea { get; set; }

        public int DriverId { get; set; }
        public Driver Driver { get; set; }

        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }

        public DateTime TripStartTime { get; set; }
        public DateTime TripEndTime { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
