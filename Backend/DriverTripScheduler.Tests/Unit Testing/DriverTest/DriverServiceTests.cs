using DriverTripBackendProject.Data;
using DriverTripBackendProject.DTO.Drivers;
using DriverTripBackendProject.Events;
using DriverTripBackendProject.Models;
using DriverTripBackendProject.Outbox;
using DriverTripBackendProject.Repository.DriverRepo;
using DriverTripBackendProject.Service;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DriverTripBackendProject.Service.DriverServices;

namespace DriverTripScheduler.Tests.DriverTest
{
    [TestClass]
    public class DriverServiceTests
    {
        private DriverService _driverService;
        private Mock<IDriverRepository> _mockRepo;
        private Mock<IOutboxWriter> _mockOutbox;
        private Mock<IUnitOfWork> _mockUow;

        [TestInitialize]
        public void Setup()
        {
            _mockRepo = new Mock<IDriverRepository>();
            _mockOutbox = new Mock<IOutboxWriter>();
            _mockUow = new Mock<IUnitOfWork>();

            var tx = new Mock<IDbContextTransaction>();
            tx.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tx.Object);
            _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _driverService = new DriverService(_mockRepo.Object, _mockOutbox.Object, _mockUow.Object);
        }

        [TestMethod]
        public async Task AddAsync_ShouldCallRepositoryWithMappedDriver()
        {
            // Arrange
            var dto = new DriverCreateDTO
            {
                Name = "John",
                PhoneNumber = "987458975",
                Email = "john@example.com"
            };

            // Act
            await _driverService.AddAsync(dto);

            // Assert
            _mockRepo.Verify(repo => repo.AddAsync(It.Is<Driver>(d =>
                d.Name == dto.Name && d.Phone == dto.PhoneNumber && d.Email == dto.Email
            )), Times.Once);

            // The DriverCreated event is staged on the outbox in the same unit of work.
            _mockOutbox.Verify(o => o.Add(
                It.Is<DriverCreatedEvent>(e =>
                    e.Name == dto.Name && e.PhoneNumber == dto.PhoneNumber && e.Email == dto.Email),
                It.IsAny<string>()), Times.Once);
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
