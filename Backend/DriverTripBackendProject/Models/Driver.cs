using System.Text.Json.Serialization;

namespace DriverTripBackendProject.Models
{
    public class Driver
    {
        public int DriverId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }

        // Nullable so existing driver rows (which have no email) remain valid.
        // New drivers are required to provide an email via DriverCreateDTO validation.
        public string? Email { get; set; }

        public int? VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }

        [JsonIgnore]
        public ICollection<Trip> Trips { get; set; }
    }
}
