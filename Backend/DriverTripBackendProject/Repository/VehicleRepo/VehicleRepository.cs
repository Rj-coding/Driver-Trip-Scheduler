using DriverTripBackendProject.Data;
using DriverTripBackendProject.Models;
using Microsoft.EntityFrameworkCore;

namespace DriverTripBackendProject.Repository.VehicleRepo
{
    public class VehicleRepository : IVehicleRepository
    {
            private readonly AppDbContext _context;

            public VehicleRepository(AppDbContext context)
            {
                _context = context;
            }

            public async Task<IEnumerable<Vehicle>> GetAllAsync()
            {
                return await _context.Vehicles.ToListAsync();
            }

            public async Task<Vehicle> GetByIdAsync(int id)
            {
                return await _context.Vehicles.FindAsync(id);
            }

            public async Task AddAsync(Vehicle vehicle)
            {
                await _context.Vehicles.AddAsync(vehicle);
                await _context.SaveChangesAsync();
            }

            public async Task UpdateAsync(Vehicle vehicle)
            {
                _context.Vehicles.Update(vehicle);
                await _context.SaveChangesAsync();
            }

            public async Task DeleteAsync(int id)
            {
                var vehicle = await _context.Vehicles.FindAsync(id);
                if (vehicle != null)
                {
                    _context.Vehicles.Remove(vehicle);
                    await _context.SaveChangesAsync();
                }
            }
        }
}
