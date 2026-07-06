namespace DriverTripBackendProject.Models
{
    public class Area
    {
        public int AreaId { get; set; }
        public string Name { get; set; }

        public int CityId { get; set; }
        public City City { get; set; }
    }
}
