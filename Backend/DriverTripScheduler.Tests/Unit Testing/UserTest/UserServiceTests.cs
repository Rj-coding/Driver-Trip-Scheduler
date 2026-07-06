using DriverTripSchedulerBackend.DTO.Users;
using DriverTripSchedulerBackend.Helpers;
using DriverTripSchedulerBackend.Models;
using DriverTripSchedulerBackend.Repository.UserRepo;
using DriverTripSchedulerBackend.Service;
using DriverTripSchedulerBackend.Service.UserServices;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;

namespace DriverTripScheduler.Tests
{
    [TestClass]
    public class UserServiceTests
    {
        private Mock<IUserRepository> _mockRepo;
        private JwtHelper _jwtHelper;
        private UserService _userService;

        [TestInitialize]
        public void Setup()
        {
            _mockRepo = new Mock<IUserRepository>();

            // Setup dummy JWT config
            var inMemorySettings = new Dictionary<string, string> {
                {"Jwt:Key", "ThisIsASecureJwtKeyForTesting123456!"},
                {"Jwt:Issuer", "TestIssuer"},
                {"Jwt:Audience", "TestAudience"},
                {"Jwt:ExpiresInMinutes", "60"}
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _jwtHelper = new JwtHelper(configuration);
            _userService = new UserService(_mockRepo.Object, _jwtHelper);
        }

        [TestMethod]
        public async Task RegisterAsync_ShouldReturnSuccess_WhenUsernameIsNew()
        {
            // Arrange
            var dto = new UserRegisterDTO { Username = "testuser", Password = "test123", Role = "Driver" };
            _mockRepo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync((User)null);

            // Act
            var result = await _userService.RegisterAsync(dto);

            // Assert
            Assert.IsTrue(result.isSuccess);
            Assert.IsNull(result.errorMessage);
            _mockRepo.Verify(r => r.AddUserAsync(It.IsAny<User>()), Times.Once);
        }

        [TestMethod]
        public async Task RegisterAsync_ShouldReturnError_WhenUsernameExists()
        {
            // Arrange
            var dto = new UserRegisterDTO { Username = "testuser", Password = "test123", Role = "Driver" };
            _mockRepo.Setup(r => r.GetByUsernameAsync("testuser"))
                     .ReturnsAsync(new User { Username = "testuser" });

            // Act
            var result = await _userService.RegisterAsync(dto);

            // Assert
            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual("Username already exists.", result.errorMessage);
            _mockRepo.Verify(r => r.AddUserAsync(It.IsAny<User>()), Times.Never);
        }

        [TestMethod]
        public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreCorrect()
        {
            // Arrange
            var dto = new UserLoginDTO { Username = "admin", Password = "adminpass" };
            var hashedPassword = Hash("adminpass");

            var user = new User
            {
                Username = "admin",
                PasswordHash = hashedPassword,
                Role = "Manager"
            };

            _mockRepo.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(user);

            // Act
            var result = await _userService.LoginAsync(dto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("admin", result.Username);
            Assert.AreEqual("Manager", result.Role);
            Assert.IsFalse(string.IsNullOrEmpty(result.Token));
        }

        [TestMethod]
        public async Task LoginAsync_ShouldReturnNull_WhenPasswordIsWrong()
        {
            // Arrange
            var dto = new UserLoginDTO { Username = "admin", Password = "wrongpass" };
            var user = new User
            {
                Username = "admin",
                PasswordHash = Hash("correctpass"),
                Role = "Manager"
            };

            _mockRepo.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(user);

            // Act
            var result = await _userService.LoginAsync(dto);

            // Assert
            Assert.IsNull(result);
        }

        private string Hash(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
