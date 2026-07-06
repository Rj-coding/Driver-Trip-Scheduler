using DriverTripSchedulerBackend.DTO.Drivers;
using DriverTripSchedulerBackend.Models;
using DriverTripSchedulerBackend.Repository.DriverRepo;
using DriverTripSchedulerBackend.Service;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DriverTripSchedulerBackend.Service.DriverServices;

namespace DriverTripScheduler.Tests.DriverTest
{
    [TestClass]
    public class DriverServiceTests
    {
        private DriverService _driverService;
        private Mock<IDriverRepository> _mockRepo;

        [TestInitialize]
        public void Setup()
        {
            _mockRepo = new Mock<IDriverRepository>();
            _driverService = new DriverService(_mockRepo.Object);
        }

        [TestMethod]
        public async Task AddAsync_ShouldCallRepositoryWithMappedDriver()
        {
            // Arrange
            var dto = new DriverCreateDTO
            {
                Name = "John",
                PhoneNumber = "987458975"
            };

            // Act
            await _driverService.AddAsync(dto);

            // Assert
            _mockRepo.Verify(repo => repo.AddAsync(It.Is<Driver>(d =>
                d.Name == dto.Name && d.Phone == dto.PhoneNumber
            )), Times.Once);
        }

        [TestMethod]
        public async Task GetAllAsync_ShouldReturnAllDriversMappedToDTO()
        {
            // Arrange
            var drivers = new List<Driver>
            {
                new Driver { DriverId = 1, Name = "John", Phone = "12345" },
                new Driver { DriverId = 2, Name = "Jane", Phone = "67890" }
            };

            _mockRepo.Setup(repo => repo.GetAllAsync()).ReturnsAsync(drivers);

            // Act
            var result = await _driverService.GetAllAsync();

            // Assert
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("John", result.First().Name);
        }

        [TestMethod]
        public async Task GetByIdAsync_ShouldReturnCorrectDriverDTO()
        {
            // Arrange
            var driver = new Driver { DriverId = 1, Name = "John", Phone = "12345" };
            _mockRepo.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(driver);

            // Act
            var result = await _driverService.GetByIdAsync(1);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("John", result.Name);
        }

        [TestMethod]
        public async Task UpdateAsync_ShouldCallRepositoryWithUpdatedDriver()
        {
            // Arrange
            var existingDriver = new Driver { DriverId = 1, Name = "Old Name", Phone = "00000" };
            var updateDto = new DriverUpdateDTO { Name = "New Name", PhoneNumber = "99999" };

            _mockRepo.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingDriver);

            // Act
            await _driverService.UpdateAsync(1, updateDto);

            // Assert
            _mockRepo.Verify(repo => repo.UpdateAsync(It.Is<Driver>(d =>
                d.DriverId == 1 && d.Name == "New Name" && d.Phone == "99999"
            )), Times.Once);
        }

        [TestMethod]
        public async Task DeleteAsync_ShouldCallRepositoryDeleteById()
        {
            // Act
            await _driverService.DeleteAsync(1);

            // Assert
            _mockRepo.Verify(repo => repo.DeleteAsync(1), Times.Once);
        }
    }
}
