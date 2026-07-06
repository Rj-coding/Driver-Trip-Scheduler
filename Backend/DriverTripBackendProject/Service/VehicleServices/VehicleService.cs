using DriverTripBackendProject.DTO.Vehicles;
using DriverTripBackendProject.Models;
using DriverTripBackendProject.Repository.VehicleRepo;


namespace DriverTripBackendProject.Service.VehicleServices
{
    public class VehicleService : IVehicleService
    {
        private readonly IVehicleRepository _repository;

        public VehicleService(IVehicleRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<VehicleDTO>> GetAllAsync()
        {
            var vehicles = await _repository.GetAllAsync();
            return vehicles.Select(v => new VehicleDTO
            {
                VehicleId = v.VehicleId,
                VehicleNumber = v.VehicleNumber,
                Type = v.Type,
                DriverId = v.DriverId ?? 0
            });
        }

        public async Task<VehicleDTO> GetByIdAsync(int id)
        {
            var vehicle = await _repository.GetByIdAsync(id);
            if (vehicle == null) return null;

            return new VehicleDTO
            {
                VehicleId = vehicle.VehicleId,
                VehicleNumber = vehicle.VehicleNumber,
                Type = vehicle.Type,
                DriverId = vehicle.DriverId ?? 0
            };
        }

        public async Task AddAsync(VehicleCreateDTO vehicleDto)
        {
            var vehicle = new Vehicle
            {
                VehicleNumber = vehicleDto.VehicleNumber,
                Type = vehicleDto.Type,
                DriverId = null
            };

            await _repository.AddAsync(vehicle);
        }

        public async Task UpdateAsync(VehicleDTO vehicleDto)
        {
            var vehicle = await _repository.GetByIdAsync(vehicleDto.VehicleId);
            if (vehicle == null) return;

            vehicle.VehicleNumber = vehicleDto.VehicleNumber;
            vehicle.Type = vehicleDto.Type;
            vehicle.DriverId = vehicleDto.DriverId;

            await _repository.UpdateAsync(vehicle);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}
