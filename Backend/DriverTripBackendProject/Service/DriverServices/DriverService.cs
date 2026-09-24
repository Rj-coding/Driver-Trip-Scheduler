using DriverTripBackendProject.Data;
using DriverTripBackendProject.DTO.Drivers;
using DriverTripBackendProject.Events;
using DriverTripBackendProject.Models;
using DriverTripBackendProject.Outbox;
using DriverTripBackendProject.Repository.DriverRepo;

namespace DriverTripBackendProject.Service.DriverServices
{
    public class DriverService : IDriverService
    {
        private readonly IDriverRepository _repository;
        private readonly IOutboxWriter _outboxWriter;
        private readonly IUnitOfWork _unitOfWork;

        public DriverService(
            IDriverRepository repository,
            IOutboxWriter outboxWriter,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _outboxWriter = outboxWriter;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<DriverDTO>> GetAllAsync()
        {
            var drivers = await _repository.GetAllAsync();
            return drivers.Select(d => new DriverDTO
            {
                DriverId = d.DriverId,
                Name = d.Name,
                PhoneNumber = d.Phone,
                Email = d.Email
            });
        }

        public async Task<DriverDTO> GetByIdAsync(int id)
        {
            var d = await _repository.GetByIdAsync(id);
            return d == null ? null : new DriverDTO
            {
                DriverId = d.DriverId,
                Name = d.Name,
                PhoneNumber = d.Phone,
                Email = d.Email
            };
        }

        public async Task AddAsync(DriverCreateDTO dto)
        {
            var driver = new Driver { Name = dto.Name, Phone = dto.PhoneNumber, Email = dto.Email };

            // Persist the driver and stage the DriverCreated event in ONE transaction so
            // the driver row and the outbox row commit atomically (or neither does).
            await using var transaction = await _unitOfWork.BeginTransactionAsync();

            await _repository.AddAsync(driver);  // SaveChanges #1 -> DriverId assigned (inside tx)

            _outboxWriter.Add(BuildDriverCreatedEvent(driver)); // stage on the same DbContext
            await _unitOfWork.SaveChangesAsync();               // SaveChanges #2 -> outbox row

            await transaction.CommitAsync();
        }

        private static DriverCreatedEvent BuildDriverCreatedEvent(Driver driver) => new()
        {
            DriverId = driver.DriverId,
            Name = driver.Name ?? string.Empty,
            Email = driver.Email ?? string.Empty,
            PhoneNumber = driver.Phone ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        public async Task<bool> UpdateAsync(int DriverId, DriverUpdateDTO driverDto)
        {
            var driver = new Driver
            {
                DriverId = DriverId,
                Name = driverDto.Name,
                Phone = driverDto.PhoneNumber,
                VehicleId = driverDto.VehicleId
            };

            return await _repository.UpdateAsync(driver);
        }

        public async Task DeleteAsync(int id) => await _repository.DeleteAsync(id);
    }

}
