using AutoMapper;
using DriverTripSchedulerBackend.DTO.Trips;
using DriverTripSchedulerBackend.Models;
using DriverTripSchedulerBackend.Repository.TripRepo;
using DriverTripSchedulerBackend.Service.TripServices;
using Moq;


namespace DriverTripScheduler.Tests
{
    [TestClass]
    public class TripServiceTests
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

        private TripDTO GetValidTripDTO()
        {
            return new TripDTO
            {
                DriverId = 1,
                VehicleId = 1,
                OriginCityId = 100,
                OriginAreaId = 101,
                DestinationCityId = 200,
                DestinationAreaId = 201,
                TripStartTime = DateTime.Now.AddHours(1),
                TripEndTime = DateTime.Now.AddHours(2)
            };
        }

        private void SetupValidAreaCityChecks(TripDTO dto)
        {
            _mockRepo.Setup(r => r.IsAreaInCity(dto.OriginAreaId, dto.OriginCityId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.IsAreaInCity(dto.DestinationAreaId, dto.DestinationCityId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.IsDriverAvailable(dto.DriverId,null)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.HasOverlappingTripForDriver(dto.DriverId, dto.TripStartTime, dto.TripEndTime, null)).ReturnsAsync(false);
            _mockRepo.Setup(r => r.HasOverlappingTripForVehicle(dto.VehicleId, dto.TripStartTime, dto.TripEndTime, null)).ReturnsAsync(false);

        }


        [TestMethod]
        public async Task GetAllTripsAsync_ShouldReturnMappedTripResponseDTOs()
        {
            // Arrange
            var tripEntities = new List<Trip>
    {
        new Trip { TripId = 1, DriverId = 1, VehicleId = 1 },
        new Trip { TripId = 2, DriverId = 2, VehicleId = 2 }
    };

            var tripResponseDTOs = new List<TripResponseDTO>
    {
        new TripResponseDTO { TripId = 1, DriverId = 1, VehicleId = 1 },
        new TripResponseDTO { TripId = 2, DriverId = 2, VehicleId = 2 }
    };

            _mockRepo.Setup(r => r.GetAllTripsAsync()).ReturnsAsync(tripEntities);
            _mockMapper.Setup(m => m.Map<List<TripResponseDTO>>(tripEntities)).Returns(tripResponseDTOs);

            // Act
            var result = await _service.GetAllTripsAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(1, result.First().TripId);
            Assert.AreEqual(2, result.Last().TripId);

            _mockRepo.Verify(r => r.GetAllTripsAsync(), Times.Once);
            _mockMapper.Verify(m => m.Map<List<TripResponseDTO>>(tripEntities), Times.Once);
        }


        [TestMethod]
        public async Task AddTripAsync_ShouldFail_IfTripTimeIsInPast()
        {
            var dto = GetValidTripDTO();
            dto.TripStartTime = DateTime.Now.AddMinutes(-30);
            dto.TripEndTime = DateTime.Now.AddMinutes(-10);

            var result = await _service.AddTripAsync(dto);

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual("Trip time cannot be in the past.", result.errorMessage);
        }

        [TestMethod]
        public async Task AddTripAsync_ShouldFail_IfEndTimeIsBeforeStartTime()
        {
            var dto = GetValidTripDTO();
            dto.TripEndTime = dto.TripStartTime.AddMinutes(-5);

            var result = await _service.AddTripAsync(dto);

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual("Trip end time must be after start time.", result.errorMessage);
        }

        [TestMethod]
        public async Task AddTripAsync_ShouldFail_IfOriginAreaIsInvalid()
        {
            var dto = GetValidTripDTO();
            _mockRepo.Setup(r => r.IsAreaInCity(dto.OriginAreaId, dto.OriginCityId)).ReturnsAsync(false);

            _mockRepo.Setup(r => r.IsAreaInCity(dto.DestinationAreaId, dto.DestinationCityId)).ReturnsAsync(true);

            var result = await _service.AddTripAsync(dto);

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual("Origin area does not belong to the selected origin city.", result.errorMessage);
        }

        [TestMethod]
        public async Task AddTripAsync_ShouldFail_IfDestinationAreaIsInvalid()
        {
            var dto = GetValidTripDTO();
            _mockRepo.Setup(r => r.IsAreaInCity(dto.OriginAreaId, dto.OriginCityId)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.IsAreaInCity(dto.DestinationAreaId, dto.DestinationCityId)).ReturnsAsync(false);

            var result = await _service.AddTripAsync(dto);

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual("Destination area does not belong to the selected destination city.", result.errorMessage);
        }

        [TestMethod]
        public async Task AddTripAsync_ShouldFail_IfDriverNotAvailable()
        {
            var dto = GetValidTripDTO();
            SetupValidAreaCityChecks(dto);
            _mockRepo.Setup(r => r.IsDriverAvailable(dto.DriverId,null)).ReturnsAsync(false);

            var result = await _service.AddTripAsync(dto);

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual("Driver has an active or upcoming trip.", result.errorMessage);
        }

        [TestMethod]
        public async Task AddTripAsync_ShouldFail_IfDriverHasOverlappingTrip()
        {
            var dto = GetValidTripDTO();
            SetupValidAreaCityChecks(dto);
            _mockRepo.Setup(r => r.HasOverlappingTripForDriver(dto.DriverId, dto.TripStartTime, dto.TripEndTime, null)).ReturnsAsync(true);

            var result = await _service.AddTripAsync(dto);

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual("Trip time overlaps with another trip for the driver.", result.errorMessage);
        }

        [TestMethod]
        public async Task AddTripAsync_ShouldFail_IfVehicleHasOverlappingTrip()
        {
            var dto = GetValidTripDTO();
            SetupValidAreaCityChecks(dto);
            _mockRepo.Setup(r => r.HasOverlappingTripForVehicle(dto.VehicleId, dto.TripStartTime, dto.TripEndTime, null)).ReturnsAsync(true);

            var result = await _service.AddTripAsync(dto);

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual("Trip time overlaps with another trip for the vehicle.", result.errorMessage);
        }

        [TestMethod]
        public async Task AddTripAsync_ShouldSucceed_WhenAllValidationsPass()
        {
            var dto = GetValidTripDTO();
            SetupValidAreaCityChecks(dto);

            var tripEntity = new Trip(); // Simulate saved trip
            var fullTrip = new Trip();   // Simulate full trip with navigation

            _mockMapper.Setup(m => m.Map<Trip>(dto)).Returns(tripEntity);
            _mockRepo.Setup(r => r.AddTripAsync(tripEntity)).ReturnsAsync(tripEntity);
            _mockRepo.Setup(r => r.GetTripWithDetailsByIdAsync(tripEntity.TripId)).ReturnsAsync(fullTrip);

            var result = await _service.AddTripAsync(dto);

            Assert.IsTrue(result.isSuccess);
            Assert.IsNull(result.errorMessage);
            Assert.IsNotNull(result.trip);
        }
    }
}
