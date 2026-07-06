using DriverTripBackendProject.Data;
using DriverTripBackendProject.Models;

using Microsoft.EntityFrameworkCore;
namespace DriverTripBackendProject.Repository.DriverRepo
{
    public class DriverRepository : IDriverRepository
    {
        private readonly AppDbContext _context;

        public DriverRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Driver>> GetAllAsync() =>
            await _context.Drivers.ToListAsync();

        public async Task<Driver> GetByIdAsync(int id) =>
            await _context.Drivers.FindAsync(id);

        public async Task AddAsync(Driver driver)
        {
            await _context.Drivers.AddAsync(driver);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateAsync(Driver driver)
        {
           
            var existingDriver = await _context.Drivers.FindAsync(driver.DriverId);
            if (existingDriver == null)
                return false;

            existingDriver.Name = driver.Name;
            existingDriver.Phone = driver.Phone;
            existingDriver.VehicleId = driver.VehicleId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task DeleteAsync(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver != null)
            {
                _context.Drivers.Remove(driver);
                await _context.SaveChangesAsync();
            }
        }
    }
}
