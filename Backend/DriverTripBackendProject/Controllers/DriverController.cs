using DriverTripBackendProject.DTO.Drivers;
using DriverTripBackendProject.Service.DriverServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DriverTripBackendProject.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class DriverController : ControllerBase
    {
        private readonly IDriverService _driverService;

        public DriverController(IDriverService driverService)
        {
            _driverService = driverService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DriverDTO>>> GetAll()
        {
            var drivers = await _driverService.GetAllAsync();
            return Ok(drivers);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DriverDTO>> GetById(int id)
        {
            var driver = await _driverService.GetByIdAsync(id);
            if (driver == null) return NotFound();
            return Ok(driver);
        }

        [Authorize(Roles = "Manager")]
        [HttpPost]
        public async Task<ActionResult> Create(DriverCreateDTO driverDto)
        {
            await _driverService.AddAsync(driverDto);
            return Ok(new { message = "Driver created successfully." });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] DriverUpdateDTO driverDto)
        {
            var success= await _driverService.UpdateAsync(id, driverDto);

            if (!success)
                return NotFound(new { message = "Driver not found." });

            return Ok(new { message = "Driver updated successfully." });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult> Delete(int id)
        {
            await _driverService.DeleteAsync(id);
            return Ok(new { message = "Driver deleted successfully." });
        }
    }

}
