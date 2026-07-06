using DriverTripBackendProject.Models;

namespace DriverTripBackendProject.Repository.UserRepo
{
    public interface IUserRepository
    {
        Task<User> GetByUsernameAsync(string username);
        Task AddUserAsync(User user);
    }
}
