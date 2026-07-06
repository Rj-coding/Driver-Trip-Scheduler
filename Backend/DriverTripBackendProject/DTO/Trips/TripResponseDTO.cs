namespace DriverTripBackendProject.DTO.Trips
{
    public class TripResponseDTO
    {
        public int TripId { get; set; }

        public string OriginCityName { get; set; }
        public string OriginAreaName { get; set; }
        public string DestinationCityName { get; set; }
        public string DestinationAreaName { get; set; }
        public int OriginCityId { get; set; }
        public int OriginAreaId { get; set; }
        public int DestinationCityId { get; set; }
        public int DestinationAreaId { get; set; }


        public int DriverId { get; set; }
        public string DriverName { get; set; }

        public int VehicleId { get; set; }
        public string VehicleNumber { get; set; }

        public DateTime TripStartTime { get; set; }
        public DateTime TripEndTime { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
