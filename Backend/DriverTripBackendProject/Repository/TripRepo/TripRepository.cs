using DriverTripBackendProject.Data;
using DriverTripBackendProject.Models;
using Microsoft.EntityFrameworkCore;

namespace DriverTripBackendProject.Repository.TripRepo
{
    public class TripRepository : ITripRepository
    {
        private readonly AppDbContext _context;

        public TripRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Trip>> GetAllTripsAsync()
        {

            return await _context.Trips
                .Include(t => t.OriginCity)
   .Include(t => t.OriginArea)
   .Include(t => t.DestinationCity)
   .Include(t => t.DestinationArea)
   .Include(t => t.Driver)
   .Include(t => t.Vehicle)
   .ToListAsync();

        }

        public async Task<Trip?> GetTripWithDetailsByIdAsync(int id)
        {
            return await _context.Trips
                .Include(t => t.OriginCity)
                .Include(t => t.OriginArea)
                .Include(t => t.DestinationCity)
                .Include(t => t.DestinationArea)
                .Include(t => t.Driver)
                .Include(t => t.Vehicle)
                .FirstOrDefaultAsync(t => t.TripId == id);
        }

        public async Task<Trip> GetTripByIdAsync(int id)
        {
            return await _context.Trips.FindAsync(id);
        }

        public async Task<Trip> UpdateTripAsync(Trip trip)
        {
            _context.Trips.Update(trip);
            await _context.SaveChangesAsync();
            return trip;
        }


        public async Task<Trip> AddTripAsync(Trip trip)
        {
            trip.CreatedAt = DateTime.Now;
            _context.Trips.Add(trip);
            await _context.SaveChangesAsync();
            return trip;
        }

        public async Task<bool> DeleteTripAsync(int tripId)
        {
            var trip = await _context.Trips.FindAsync(tripId);
            if (trip == null)
                return false;

            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasOverlappingTripForDriver(int driverId, DateTime start, DateTime end, int? excludeTripId = null)
        {
            return await _context.Trips.AnyAsync(t =>
                t.DriverId == driverId && (!excludeTripId.HasValue || t.TripId != excludeTripId) &&
                ((start >= t.TripStartTime && start < t.TripEndTime) ||
                 (end > t.TripStartTime && end <= t.TripEndTime) ||
                 (start <= t.TripStartTime && end >= t.TripEndTime)));
        }

        public async Task<bool> HasOverlappingTripForVehicle(int vehicleId, DateTime start, DateTime end, int? excludeTripId = null)
        {
            return await _context.Trips.AnyAsync(t =>
                t.VehicleId == vehicleId && (!excludeTripId.HasValue || t.TripId != excludeTripId) &&
                ((start >= t.TripStartTime && start < t.TripEndTime) ||
                 (end > t.TripStartTime && end <= t.TripEndTime) ||
                 (start <= t.TripStartTime && end >= t.TripEndTime)));
        }

        public async Task<bool> IsDriverAvailable(int driverId, int? excludeTripId = null)
        {
            var now = DateTime.Now;
            return !await _context.Trips.AnyAsync(t =>
                t.DriverId == driverId && (!excludeTripId.HasValue || t.TripId != excludeTripId) &&
                t.TripEndTime > now); // active or upcoming trip exists
        }

        public async Task<bool> IsAreaInCity(int areaId, int cityId)
        {
            var area = await _context.Areas.FindAsync(areaId);
            return area != null && area.CityId == cityId;
        }


        public async Task<IEnumerable<Trip>> GetTripsByFilterAsync(string driverName, string vehicleNumber)
        {
            var query = _context.Trips
                .Include(t => t.OriginCity)
                .Include(t => t.OriginArea)
                .Include(t => t.DestinationCity)
                .Include(t => t.DestinationArea)
                .Include(t => t.Driver)
                .Include(t => t.Vehicle)
                .AsQueryable();

            if (!string.IsNullOrEmpty(driverName))
                query = query.Where(t => t.Driver.Name.Contains(driverName));

            if (!string.IsNullOrEmpty(vehicleNumber))
                query = query.Where(t => t.Vehicle.VehicleNumber.Contains(vehicleNumber));

            return await query.ToListAsync();
        }



    }
}
