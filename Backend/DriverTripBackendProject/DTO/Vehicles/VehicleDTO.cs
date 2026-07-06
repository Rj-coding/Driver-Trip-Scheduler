namespace DriverTripBackendProject.DTO.Vehicles
{
    public class VehicleDTO
    {

        public int VehicleId { get; set; }
        public string VehicleNumber { get; set; } // ✅ same as in the entity
        public string Type { get; set; }
        public int? DriverId { get; set; }
    }
}
