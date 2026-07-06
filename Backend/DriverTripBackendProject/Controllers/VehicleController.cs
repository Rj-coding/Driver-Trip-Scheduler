using DriverTripBackendProject.DTO.Vehicles;
using DriverTripBackendProject.Service.VehicleServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DriverTripSchedulerBackend.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;

        public VehicleController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleDTO>>> GetAll()
        {
            var vehicles = await _vehicleService.GetAllAsync();
            return Ok(vehicles);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleDTO>> GetById(int id)
        {
            var vehicle = await _vehicleService.GetByIdAsync(id);
            if (vehicle == null) return NotFound();
            return Ok(vehicle);
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult> Create([FromBody] VehicleCreateDTO vehicleDto)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _vehicleService.AddAsync(vehicleDto);
            //return StatusCode(201, new { message = "Vehicle created successfully." });

            return CreatedAtAction(nameof(GetById), new { id = vehicleDto.VehicleNumber },
                 new
                 {
                     message = "Vehicle created successfully.",
                     vehicle = vehicleDto
                 });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult> Update(int id, VehicleDTO vehicleDto)
        {
            if (id != vehicleDto.VehicleId) return BadRequest("Vehicle ID mismatch");
            await _vehicleService.UpdateAsync(vehicleDto);
            return Ok(new { message = "Vehicle updated successfully." });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult> Delete(int id)
        {
            await _vehicleService.DeleteAsync(id);
            return Ok(new { message = "Vehicle deleted successfully." });
        }

    }
}
