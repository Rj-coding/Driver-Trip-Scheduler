using AutoMapper;
using DriverTripSchedulerBackend.DTO.Trips;
using DriverTripSchedulerBackend.DTO;
using DriverTripSchedulerBackend.Models;
using DriverTripSchedulerBackend.Repository.TripRepo;
using DriverTripSchedulerBackend.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;
using DriverTripSchedulerBackend.Service.TripServices;

namespace DriverTripScheduler.Tests
{
    [TestClass]
    public class TripServiceUpdateTests
    {
        private Mock<ITripRepository> _mockRepo;
        private Mock<IMapper> _mockMapper;
        private TripService _service;

        [TestInitialize]
        public void Setup()
        {
            _mockRepo = new Mock<ITripRepository>();
            _mockMapper = new Mock<IMapper>();
            _service = new TripService(_mockRepo.Object, _mockMapper.Object);
        }

        private TripUpdateDTO GetValidTripUpdateDTO()
        {
            return new TripUpdateDTO
            {
                TripId = 1,
                DriverId = 10,
                VehicleId = 5,
                TripStartTime = DateTime.Now.AddHours(1),
                TripEndTime = DateTime.Now.AddHours(3)
            };
        }

        private Trip GetExistingTrip()
        {
            return new Trip { TripId = 1, DriverId = 10, VehicleId = 5 };
        }

        [TestMethod]
        public async Task UpdateTripAsync_ShouldFail_IfTripNotFound()
        {
            var dto = GetValidTripUpdateDTO();
            _mockRepo.Setup(r => r.GetTripByIdAsync(dto.TripId)).ReturnsAsync((Trip)null);

            var result = await _service.UpdateTripAsync(dto);

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual("Trip not found.", result.errorMessage);
        }

        [TestMethod]
        public async Task UpdateTripAsync_ShouldFail_IfStartOrEndTimeInPast()
        {
            var dto = GetValidTripUpdateDTO();
            dto.TripStartTime = DateTime.Now.AddHours(-2);
            dto.TripEndTime = DateTime.Now.AddHours(-1);
            _mockRepo.Setup(r => r.GetTripByIdAsync(dto.TripId)).ReturnsAsync(GetExistingTrip());

            var result = await _service.UpdateTripAsync(dto);

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual("Trip time cannot be in the past.", result.errorMessage);
        }

        [TestMethod]
        public async Task UpdateTripAsync_ShouldFail_IfEndTimeBeforeStart()
        {
            var dto = GetValidTripUpdateDTO();
            dto.TripEndTime = dto.TripStartTime.AddMinutes(-30);
            _mockRepo.Setup(r => r.GetTripByIdAsync(dto.TripId)).ReturnsAsync(GetExistingTrip());

            var result = await _service.UpdateTripAsync(dto);

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual("Trip end time must be after start time.", result.errorMessage);
        }

        [TestMethod]
        public async Task UpdateTripAsync_ShouldFail_IfDriverNotAvailable()
        {
            var dto = GetValidTripUpdateDTO();
            _mockRepo.Setup(r => r.GetTripByIdAsync(dto.TripId)).ReturnsAsync(GetExistingTrip());
            _mockRepo.Setup(r => r.IsDriverAvailable(dto.DriverId,dto.TripId)).ReturnsAsync(false);

            var result = await _service.UpdateTripAsync(dto);

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual("Driver has an active or upcoming trip.", result.errorMessage);
        }

        [TestMethod]
        public async Task UpdateTripAsync_ShouldFail_IfDriverHasOverlappingTrip()
        {
            var dto = GetValidTripUpdateDTO();
            _mockRepo.Setup(r => r.GetTripByIdAsync(dto.TripId)).ReturnsAsync(GetExistingTrip());
            _mockRepo.Setup(r => r.IsDriverAvailable(dto.DriverId,dto.TripId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.HasOverlappingTripForDriver(dto.DriverId, dto.TripStartTime, dto.TripEndTime, dto.TripId)).ReturnsAsync(true);

            var result = await _service.UpdateTripAsync(dto);

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual("Trip time overlaps with another trip for the driver.", result.errorMessage);
        }

        [TestMethod]
        public async Task UpdateTripAsync_ShouldFail_IfVehicleHasOverlappingTrip()
        {
            var dto = GetValidTripUpdateDTO();
            _mockRepo.Setup(r => r.GetTripByIdAsync(dto.TripId)).ReturnsAsync(GetExistingTrip());
            _mockRepo.Setup(r => r.IsDriverAvailable(dto.DriverId,dto.TripId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.HasOverlappingTripForDriver(dto.DriverId, dto.TripStartTime, dto.TripEndTime, dto.TripId)).ReturnsAsync(false);
            _mockRepo.Setup(r => r.HasOverlappingTripForVehicle(dto.VehicleId, dto.TripStartTime, dto.TripEndTime, dto.TripId)).ReturnsAsync(true);

            var result = await _service.UpdateTripAsync(dto);

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual("Trip time overlaps with another trip for the vehicle.", result.errorMessage);
        }

        [TestMethod]
        public async Task UpdateTripAsync_ShouldSucceed_WithValidInput()
        {
            var dto = GetValidTripUpdateDTO();
            var existingTrip = GetExistingTrip();
            var updatedTrip = GetExistingTrip();

            _mockRepo.Setup(r => r.GetTripByIdAsync(dto.TripId)).ReturnsAsync(existingTrip);
            _mockRepo.Setup(r => r.IsDriverAvailable(dto.DriverId,dto.TripId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.HasOverlappingTripForDriver(dto.DriverId, dto.TripStartTime, dto.TripEndTime, dto.TripId)).ReturnsAsync(false);
            _mockRepo.Setup(r => r.HasOverlappingTripForVehicle(dto.VehicleId, dto.TripStartTime, dto.TripEndTime, dto.TripId)).ReturnsAsync(false);
            _mockRepo.Setup(r => r.UpdateTripAsync(existingTrip)).ReturnsAsync(updatedTrip);
            _mockRepo.Setup(r => r.GetTripWithDetailsByIdAsync(dto.TripId)).ReturnsAsync(updatedTrip);

            var result = await _service.UpdateTripAsync(dto);

            Assert.IsTrue(result.isSuccess);
            Assert.IsNull(result.errorMessage);
            Assert.IsNotNull(result.trip);
        }

        [TestMethod]
        public async Task GetTripsByFilterAsync_ShouldReturnFilteredTrips()
        {
            // Arrange
            var driverName = "John";
            var vehicleNumber = "ABC123";

            var mockTrips = new List<Trip>
    {
        new Trip
        {
            TripId = 1,
            Driver = new Driver { DriverId = 1, Name = "John" },
            Vehicle = new Vehicle { VehicleId = 1, VehicleNumber = "ABC123" },
            TripStartTime = DateTime.Now.AddHours(1),
            TripEndTime = DateTime.Now.AddHours(2)
        },
        new Trip
        {
            TripId = 2,
            Driver = new Driver { DriverId = 2, Name = "Jane" },
            Vehicle = new Vehicle { VehicleId = 2, VehicleNumber = "XYZ789" },
            TripStartTime = DateTime.Now.AddHours(3),
            TripEndTime = DateTime.Now.AddHours(4)
        }
    };

            _mockRepo.Setup(r => r.GetTripsByFilterAsync(driverName, vehicleNumber))
                     .ReturnsAsync(mockTrips.Where(t => t.Driver.Name == driverName && t.Vehicle.VehicleNumber == vehicleNumber).ToList());

            _mockMapper.Setup(m => m.Map<List<TripResponseDTO>>(It.IsAny<List<Trip>>()))
                       .Returns((List<Trip> srcTrips) =>
                       {
                           return srcTrips.Select(t => new TripResponseDTO
                           {
                               TripId = t.TripId,
                               DriverName = t.Driver.Name,
                               VehicleNumber = t.Vehicle.VehicleNumber,
                               TripStartTime = t.TripStartTime,
                               TripEndTime = t.TripEndTime
                           }).ToList();
                       });

            // Act
            var result = await _service.GetTripsByFilterAsync(driverName, vehicleNumber);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("John", result.First().DriverName);
            Assert.AreEqual("ABC123", result.First().VehicleNumber);
        }

    }
}
