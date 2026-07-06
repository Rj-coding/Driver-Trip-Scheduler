using DriverTripBackendProject.DTO.Users;

namespace DriverTripBackendProject.Service.UserServices
{
    public interface IUserService
    {
        Task<(bool isSuccess, string errorMessage)> RegisterAsync(UserRegisterDTO dto);
        Task<UserResponseDTO> LoginAsync(UserLoginDTO dto);
    }
}
