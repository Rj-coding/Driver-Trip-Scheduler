using DriverTripBackendProject.DTO.Vehicles;

namespace DriverTripBackendProject.Service.VehicleServices
{
    public interface IVehicleService
    {
        Task<IEnumerable<VehicleDTO>> GetAllAsync();
        Task<VehicleDTO> GetByIdAsync(int id);
        Task AddAsync(VehicleCreateDTO vehicleDto);
        Task UpdateAsync(VehicleDTO vehicleDto);
        Task DeleteAsync(int id);
    }
}
