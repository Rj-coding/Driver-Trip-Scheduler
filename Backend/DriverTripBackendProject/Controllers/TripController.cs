using DriverTripBackendProject.DTO.Trips;
using DriverTripBackendProject.Models;
using DriverTripBackendProject.Service.TripServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DriverTripSchedulerBackend.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripController : ControllerBase
    {
        private readonly ITripService _service;

        public TripController(ITripService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TripResponseDTO>>> GetAll()
        {
            var trips = await _service.GetAllTripsAsync();
            return Ok(trips);
        }

        [Authorize(Roles = "Manager")]
        [HttpPut("{id}")]
        public async Task<ActionResult<Trip>> UpdateTrip(int id, [FromBody] TripUpdateDTO dto)
        {
            if (id != dto.TripId)
                return BadRequest("Trip ID mismatch.");

            var (isSuccess, errorMessage, updatedTrip) = await _service.UpdateTripAsync(dto);
            if (!isSuccess)
                return Conflict(errorMessage);

            return Ok(updatedTrip);
        }

        [Authorize(Roles = "Manager")]
        [HttpPost]
        public async Task<ActionResult<Trip>> CreateTrip([FromBody] TripDTO dto)
        {
            var (isSuccess, errorMessage, trip) = await _service.AddTripAsync(dto);
            if (!isSuccess)
                return Conflict(errorMessage);

            return CreatedAtAction(nameof(GetAll), new { id = trip.TripId }, trip);
        }



        [Authorize(Roles = "Manager")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTrip(int id)
        {
            var (isSuccess, errorMessage) = await _service.DeleteTripAsync(id);
            if (!isSuccess)
                return NotFound(errorMessage);

            return Ok("Trip deleted successfully.");
        }



        [Authorize(Roles = "Manager,Driver")]
        [HttpGet("filter")]
        public async Task<ActionResult<IEnumerable<TripResponseDTO>>> GetTripsByFilter(
        [FromQuery] string? driverName,
        [FromQuery] string? vehicleNumber)
        {
            if (string.IsNullOrWhiteSpace(driverName) && string.IsNullOrWhiteSpace(vehicleNumber))
            {
                return BadRequest("At least one filter (driverName or vehicleNumber) must be provided.");
            }
            var result = await _service.GetTripsByFilterAsync(driverName, vehicleNumber);
            return Ok(result);
        }



    }
}
