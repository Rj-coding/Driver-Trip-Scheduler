using DriverTripBackendProject.DTO.Trips;
using DriverTripBackendProject.Models;

namespace DriverTripBackendProject.Service.TripServices
{
    public interface ITripService
    {
        Task<IEnumerable<TripResponseDTO>> GetAllTripsAsync();

        Task<(bool isSuccess, string errorMessage, Trip trip)> UpdateTripAsync(TripUpdateDTO dto);

        Task<(bool isSuccess, string errorMessage, Trip trip)> AddTripAsync(TripDTO dto);
        Task<(bool isSuccess, string errorMessage)> DeleteTripAsync(int tripId);

        Task<IEnumerable<TripResponseDTO>> GetTripsByFilterAsync(string driverName, string vehicleNumber);

    }
}
