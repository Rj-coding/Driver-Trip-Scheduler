using DriverTripBackendProject.DTO.Drivers;
using DriverTripBackendProject.Models;
using DriverTripBackendProject.Repository.DriverRepo;

namespace DriverTripBackendProject.Service.DriverServices
{
    public class DriverService : IDriverService
    {
        private readonly IDriverRepository _repository;

        public DriverService(IDriverRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<DriverDTO>> GetAllAsync()
        {
            var drivers = await _repository.GetAllAsync();
            return drivers.Select(d => new DriverDTO
            {
                DriverId = d.DriverId,
                Name = d.Name,
                PhoneNumber = d.Phone
            });
        }

        public async Task<DriverDTO> GetByIdAsync(int id)
        {
            var d = await _repository.GetByIdAsync(id);
            return d == null ? null : new DriverDTO
            {
                DriverId = d.DriverId,
                Name = d.Name,
                PhoneNumber = d.Phone
            };
        }

        public async Task AddAsync(DriverCreateDTO dto)
        {
            var driver = new Driver { Name = dto.Name, Phone = dto.PhoneNumber };
            await _repository.AddAsync(driver);
        }

        public async Task<bool> UpdateAsync(int DriverId, DriverUpdateDTO driverDto)
        {
            var driver = new Driver
            {
                DriverId = DriverId,
                Name = driverDto.Name,
                Phone = driverDto.PhoneNumber,
                VehicleId = driverDto.VehicleId
            };

            return await _repository.UpdateAsync(driver);
        }

        public async Task DeleteAsync(int id) => await _repository.DeleteAsync(id);
    }

}
