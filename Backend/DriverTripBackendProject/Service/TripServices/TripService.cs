using AutoMapper;
using DriverTripBackendProject.Data;
using DriverTripBackendProject.DTO.Trips;
using DriverTripBackendProject.Events;
using DriverTripBackendProject.Models;
using DriverTripBackendProject.Outbox;
using DriverTripBackendProject.Repository.TripRepo;

namespace DriverTripBackendProject.Service.TripServices
{
    public class TripService : ITripService
    {
        private readonly ITripRepository _repo;
        private readonly IMapper _mapper;
        private readonly IOutboxWriter _outboxWriter;
        private readonly IUnitOfWork _unitOfWork;

        public TripService(
            ITripRepository repo,
            IMapper mapper,
            IOutboxWriter outboxWriter,
            IUnitOfWork unitOfWork)
        {
            _repo = repo;
            _mapper = mapper;
            _outboxWriter = outboxWriter;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<TripResponseDTO>> GetAllTripsAsync()
        {
            var trips = await _repo.GetAllTripsAsync();
            return _mapper.Map<List<TripResponseDTO>>(trips);
        }

        public async Task<(bool isSuccess, string errorMessage, TripResponseDTO trip)> UpdateTripAsync(TripUpdateDTO dto)
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

            // Re-fetch with navigation properties, then map to a flat DTO for the response
            var fullTrip = await _repo.GetTripWithDetailsByIdAsync(updatedTrip.TripId);
            return (true, null, _mapper.Map<TripResponseDTO>(fullTrip));
        }


        public async Task<(bool isSuccess, string errorMessage, TripResponseDTO trip)> AddTripAsync(TripDTO dto)
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

            // Persist the trip and stage the TripAssigned event in ONE transaction so
            // the trip row and the outbox row commit atomically (or not at all).
            await using var transaction = await _unitOfWork.BeginTransactionAsync();

            var savedTrip = await _repo.AddTripAsync(trip); // SaveChanges #1 -> TripId assigned

            //  Re-fetch trip with navigation properties (names for the event + response DTO)
            var fullTrip = await _repo.GetTripWithDetailsByIdAsync(savedTrip.TripId);

            _outboxWriter.Add(BuildTripAssignedEvent(fullTrip!)); // stage on the same DbContext
            await _unitOfWork.SaveChangesAsync();                 // SaveChanges #2 -> outbox row

            await transaction.CommitAsync();

            return (true, null, _mapper.Map<TripResponseDTO>(fullTrip));
        }

        private static TripAssignedEvent BuildTripAssignedEvent(Trip trip) => new()
        {
            TripId = trip.TripId,
            DriverId = trip.DriverId,
            DriverName = trip.Driver?.Name ?? string.Empty,
            VehicleId = trip.VehicleId,
            VehicleNumber = trip.Vehicle?.VehicleNumber ?? string.Empty,
            OriginCityId = trip.OriginCityId,
            OriginCityName = trip.OriginCity?.Name ?? string.Empty,
            OriginAreaId = trip.OriginAreaId,
            OriginAreaName = trip.OriginArea?.Name ?? string.Empty,
            DestinationCityId = trip.DestinationCityId,
            DestinationCityName = trip.DestinationCity?.Name ?? string.Empty,
            DestinationAreaId = trip.DestinationAreaId,
            DestinationAreaName = trip.DestinationArea?.Name ?? string.Empty,
            TripStartTime = trip.TripStartTime,
            TripEndTime = trip.TripEndTime,
            CreatedAt = trip.CreatedAt
        };

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
