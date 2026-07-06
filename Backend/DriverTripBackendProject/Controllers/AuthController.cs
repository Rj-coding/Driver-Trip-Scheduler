using DriverTripBackendProject.DTO.Users;
using DriverTripBackendProject.Service.UserServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DriverTripBackendProject.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _service;

        public AuthController(IUserService service)
        {
            _service = service;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterDTO dto)
        {
            var (isSuccess, error) = await _service.RegisterAsync(dto);
            if (!isSuccess)
                return BadRequest(error);

            return Ok("User registered successfully...");
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserResponseDTO>> Login(UserLoginDTO dto)
        {
            var response = await _service.LoginAsync(dto);
            if (response == null)
                return Unauthorized("Invalid credentials.");

            return Ok(response);
        }
    }
}
