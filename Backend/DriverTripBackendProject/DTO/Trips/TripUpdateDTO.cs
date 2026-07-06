namespace DriverTripBackendProject.DTO.Trips
{
    public class TripUpdateDTO
    {
        public int TripId { get; set; } 
        public int OriginCityId { get; set; }
        public int OriginAreaId { get; set; }
        public int DestinationCityId { get; set; }
        public int DestinationAreaId { get; set; }
        public int DriverId { get; set; }
        public int VehicleId { get; set; }
        public DateTime TripStartTime { get; set; }
        public DateTime TripEndTime { get; set; }
    }
}
