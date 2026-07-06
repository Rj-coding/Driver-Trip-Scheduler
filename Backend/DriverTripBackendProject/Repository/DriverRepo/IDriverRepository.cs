using DriverTripBackendProject.Models;

namespace DriverTripBackendProject.Repository.DriverRepo
{
    public interface IDriverRepository
    {
        Task<IEnumerable<Driver>> GetAllAsync();
        Task<Driver> GetByIdAsync(int id);
        Task AddAsync(Driver driver);
        Task<bool> UpdateAsync(Driver driver);
        Task DeleteAsync(int id);
    }
}
