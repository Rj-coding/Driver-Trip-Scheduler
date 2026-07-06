using DriverTripBackendProject.DTO.Users;
using DriverTripBackendProject.Helpers;
using DriverTripBackendProject.Models;
using DriverTripBackendProject.Repository.UserRepo;
using System.Security.Cryptography;
using System.Text;

namespace DriverTripBackendProject.Service.UserServices
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        private readonly JwtHelper _jwtHelper;

        public UserService(IUserRepository repo, JwtHelper jwtHelper)
        {
            _repo = repo;
            _jwtHelper = jwtHelper;
        }

        public async Task<(bool isSuccess, string errorMessage)> RegisterAsync(UserRegisterDTO dto)
        {
            var existingUser = await _repo.GetByUsernameAsync(dto.Username);
            if (existingUser != null)
                return (false, "Username already exists.");

            var user = new User
            {
                Username = dto.Username,
                PasswordHash = HashPassword(dto.Password),
                Role = dto.Role
            };

            await _repo.AddUserAsync(user);
            return (true, null);
        }

        public async Task<UserResponseDTO> LoginAsync(UserLoginDTO dto)
        {
            var user = await _repo.GetByUsernameAsync(dto.Username);
            if (user == null || user.PasswordHash != HashPassword(dto.Password))
                return null;

            var token = _jwtHelper.GenerateToken(user);
            return new UserResponseDTO
            {
                Username = user.Username,
                Role = user.Role,
                Token = token
            };
        }

        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
