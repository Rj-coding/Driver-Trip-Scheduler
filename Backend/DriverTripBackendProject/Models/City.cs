using System.Text.Json.Serialization;

namespace DriverTripBackendProject.Models
{
    public class City
    {
        public int CityId { get; set; }
        public string Name { get; set; }

        [JsonIgnore]
        public ICollection<Area> Areas { get; set; }
    }
}
