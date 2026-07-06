using DriverTripBackendProject.Models;

namespace DriverTripBackendProject.Repository.TripRepo
{
    public interface ITripRepository
    {
        Task<IEnumerable<Trip>> GetAllTripsAsync();
        Task<Trip?> GetTripWithDetailsByIdAsync(int id);
        Task<Trip> GetTripByIdAsync(int id);
        Task<Trip> UpdateTripAsync(Trip trip);


        Task<Trip> AddTripAsync(Trip trip);
        Task<bool> DeleteTripAsync(int tripId);

        Task<bool> HasOverlappingTripForDriver(int driverId, DateTime start, DateTime end, int? excludeTripId = null);
        Task<bool> HasOverlappingTripForVehicle(int vehicleId, DateTime start, DateTime end, int? excludeTripId = null);
        Task<bool> IsDriverAvailable(int driverId, int? excludeTripId = null);

        Task<bool> IsAreaInCity(int areaId, int cityId);

        Task<IEnumerable<Trip>> GetTripsByFilterAsync(string driverName, string vehicleNumber);


    }
}
