using DriverTripBackendProject.DTO.Drivers;

namespace DriverTripBackendProject.Service.DriverServices
{
    public interface IDriverService
    {
        Task<IEnumerable<DriverDTO>> GetAllAsync();
        Task<DriverDTO> GetByIdAsync(int id);
        Task AddAsync(DriverCreateDTO driver);
        Task<bool> UpdateAsync(int id,DriverUpdateDTO driver);
        Task DeleteAsync(int id);
    }
}
