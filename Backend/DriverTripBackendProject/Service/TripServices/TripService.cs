using AutoMapper;
using DriverTripBackendProject.DTO.Trips;
using DriverTripBackendProject.Models;
using DriverTripBackendProject.Repository.TripRepo;

namespace DriverTripBackendProject.Service.TripServices
{
    public class TripService : ITripService
    {
        private readonly ITripRepository _repo;
        private readonly IMapper _mapper;

        public TripService(ITripRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TripResponseDTO>> GetAllTripsAsync()
        {
            var trips = await _repo.GetAllTripsAsync();
            return _mapper.Map<List<TripResponseDTO>>(trips);
        }

        public async Task<(bool isSuccess, string errorMessage, Trip trip)> UpdateTripAsync(TripUpdateDTO dto)
        {
            var existingTrip = await _repo.GetTripByIdAsync(dto.TripId);
            if (dto.TripStartTime < DateTime.Now || dto.TripEndTime < DateTime.Now)
                return (false, "Trip time cannot be in the past.", null);

            
            if (dto.TripEndTime <= dto.TripStartTime)
                return (false, "Trip end time must be after start time.", null);

            if (existingTrip == null)
                return (false, "Trip not found.", null);

            if (!await _repo.IsDriverAvailable(dto.DriverId, dto.TripId))
                return (false, "Driver has an active or upcoming trip.", null);

            if (await _repo.HasOverlappingTripForDriver(dto.DriverId, dto.TripStartTime, dto.TripEndTime, dto.TripId))
                return (false, "Trip time overlaps with another trip for the driver.", null);

            if (await _repo.HasOverlappingTripForVehicle(dto.VehicleId, dto.TripStartTime, dto.TripEndTime, dto.TripId))
                return (false, "Trip time overlaps with another trip for the vehicle.", null);

            _mapper.Map(dto, existingTrip);
            var updatedTrip = await _repo.UpdateTripAsync(existingTrip);

            // Re-fetch with navigation properties
            var fullTrip = await _repo.GetTripWithDetailsByIdAsync(updatedTrip.TripId);
            return (true, null, fullTrip);
        }


        public async Task<(bool isSuccess, string errorMessage, Trip trip)> AddTripAsync(TripDTO dto)
        {

            if (dto.TripStartTime < DateTime.Now || dto.TripEndTime < DateTime.Now)
                return (false, "Trip time cannot be in the past.", null);

            //  Ensure start is before end
            if (dto.TripEndTime <= dto.TripStartTime)
                return (false, "Trip end time must be after start time.", null);
            if (!await _repo.IsAreaInCity(dto.OriginAreaId, dto.OriginCityId))
                return (false, "Origin area does not belong to the selected origin city.", null);

            if (!await _repo.IsAreaInCity(dto.DestinationAreaId, dto.DestinationCityId))
                return (false, "Destination area does not belong to the selected destination city.", null);

            if (!await _repo.IsDriverAvailable(dto.DriverId))
                return (false, "Driver has an active or upcoming trip.", null);

            if (await _repo.HasOverlappingTripForDriver(dto.DriverId, dto.TripStartTime, dto.TripEndTime))
                return (false, "Trip time overlaps with another trip for the driver.", null);

            if (await _repo.HasOverlappingTripForVehicle(dto.VehicleId, dto.TripStartTime, dto.TripEndTime))
                return (false, "Trip time overlaps with another trip for the vehicle.", null);



            var trip = _mapper.Map<Trip>(dto);
            var savedTrip = await _repo.AddTripAsync(trip);

            //  Re-fetch trip with navigation properties
            var fullTrip = await _repo.GetTripWithDetailsByIdAsync(savedTrip.TripId);
            return (true, null, fullTrip);
        }

        public async Task<(bool isSuccess, string errorMessage)> DeleteTripAsync(int tripId)
        {
            var trip = await _repo.GetTripByIdAsync(tripId);
            if (trip == null)
                return (false, "Trip not found.");

            var success = await _repo.DeleteTripAsync(tripId);
            return success ? (true, null) : (false, "Failed to delete trip.");
        }

        public async Task<IEnumerable<TripResponseDTO>> GetTripsByFilterAsync(string driverName, string vehicleNumber)
        {
            var trips = await _repo.GetTripsByFilterAsync(driverName, vehicleNumber);
            return _mapper.Map<List<TripResponseDTO>>(trips);
        }

    }
}
