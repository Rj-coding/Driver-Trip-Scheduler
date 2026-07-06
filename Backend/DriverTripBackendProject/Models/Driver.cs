using System.Text.Json.Serialization;

namespace DriverTripBackendProject.Models
{
    public class Driver
    {
        public int DriverId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }

        public int? VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }

        [JsonIgnore]
        public ICollection<Trip> Trips { get; set; }
    }
}
