using DriverTripSchedulerBackend.DTO;
using DriverTripSchedulerBackend.DTO.Vehicles;
using DriverTripSchedulerBackend.Models;
using DriverTripSchedulerBackend.Repository.VehicleRepo;
using DriverTripSchedulerBackend.Service;
using DriverTripSchedulerBackend.Service.VehicleServices;
using Moq;


namespace DriverTripScheduler.Tests.Service
{
    [TestClass]
    public class VehicleServiceTests
    {
        private Mock<IVehicleRepository> _vehicleRepoMock;
        private VehicleService _vehicleService;

        [TestInitialize]
        public void Setup()
        {
            _vehicleRepoMock = new Mock<IVehicleRepository>();
            _vehicleService = new VehicleService(_vehicleRepoMock.Object);
        }

        [TestMethod]
        public async Task GetAllAsync_ReturnsAllVehicles()
        {
            // Arrange
            var vehicles = new List<Vehicle>
            {
                new Vehicle { VehicleId = 1, VehicleNumber = "AB123", Type = "Car", DriverId = 5 },
                new Vehicle { VehicleId = 2, VehicleNumber = "CD456", Type = "Truck", DriverId = null }
            };
            _vehicleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(vehicles);

            // Act
            var result = (await _vehicleService.GetAllAsync()).ToList();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("AB123", result[0].VehicleNumber);
            Assert.AreEqual("Truck", result[1].Type);
        }

        [TestMethod]
        public async Task GetByIdAsync_VehicleExists_ReturnsVehicleDTO()
        {
            // Arrange
            var vehicle = new Vehicle { VehicleId = 1, VehicleNumber = "AB123", Type = "Car", DriverId = 3 };
            _vehicleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vehicle);

            // Act
            var result = await _vehicleService.GetByIdAsync(1);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("AB123", result.VehicleNumber);
            Assert.AreEqual("Car", result.Type);
            Assert.AreEqual(3, result.DriverId);
        }

        [TestMethod]
        public async Task GetByIdAsync_VehicleDoesNotExist_ReturnsNull()
        {
            // Arrange
            _vehicleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Vehicle)null);

            // Act
            var result = await _vehicleService.GetByIdAsync(1);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task AddAsync_ValidDTO_CallsAddAsync()
        {
            // Arrange
            var dto = new VehicleCreateDTO { VehicleNumber = "XY789", Type = "Van" };

            // Act
            await _vehicleService.AddAsync(dto);

            // Assert
            _vehicleRepoMock.Verify(r => r.AddAsync(It.Is<Vehicle>(
                v => v.VehicleNumber == "XY789" &&
                     v.Type == "Van" &&
                     v.DriverId == null)), Times.Once);
        }

        [TestMethod]
        public async Task UpdateAsync_VehicleExists_UpdatesVehicle()
        {
            // Arrange
            var existingVehicle = new Vehicle
            {
                VehicleId = 1,
                VehicleNumber = "OLD123",
                Type = "Truck",
                DriverId = null
            };

            var dto = new VehicleDTO
            {
                VehicleId = 1,
                VehicleNumber = "NEW456",
                Type = "SUV",
                DriverId = 4
            };

            _vehicleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingVehicle);

            // Act
            await _vehicleService.UpdateAsync(dto);

            // Assert
            Assert.AreEqual("NEW456", existingVehicle.VehicleNumber);
            Assert.AreEqual("SUV", existingVehicle.Type);
            Assert.AreEqual(4, existingVehicle.DriverId);

            _vehicleRepoMock.Verify(r => r.UpdateAsync(existingVehicle), Times.Once);
        }

        [TestMethod]
        public async Task UpdateAsync_VehicleNotFound_DoesNotCallUpdate()
        {
            // Arrange
            var dto = new VehicleDTO
            {
                VehicleId = 1,
                VehicleNumber = "NEW123",
                Type = "Van",
                DriverId = 2
            };

            _vehicleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Vehicle)null);

            // Act
            await _vehicleService.UpdateAsync(dto);

            // Assert
            _vehicleRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Vehicle>()), Times.Never);
        }

        [TestMethod]
        public async Task DeleteAsync_CallsRepositoryDelete()
        {
            // Arrange
            int vehicleId = 1;

            // Act
            await _vehicleService.DeleteAsync(vehicleId);

            // Assert
            _vehicleRepoMock.Verify(r => r.DeleteAsync(vehicleId), Times.Once);
        }
    }
}
