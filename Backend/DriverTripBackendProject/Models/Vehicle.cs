
namespace DriverTripBackendProject.Models
{
    public class Vehicle
    {
        public int VehicleId { get; set; }
        public string VehicleNumber { get; set; }
        public string Type { get; set; } // e.g., "Truck", "Van"

        public int? DriverId { get; set; }
        public Driver Driver { get; set; }
    }
}
