using System.ComponentModel.DataAnnotations;

namespace DriverTripBackendProject.DTO.Drivers
{
    public class DriverCreateDTO
    {
        public string Name { get; set; }
        public string PhoneNumber { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; }
    }
}
