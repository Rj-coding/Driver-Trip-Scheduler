using DriverTripBackendProject.Data;
using DriverTripBackendProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriverTripSchedulerBackend.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class CityController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CityController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<City>>> GetCities()
        {
            return await _context.Cities.ToListAsync();
        }

        [HttpGet("{id}/areas")]
        public async Task<ActionResult<IEnumerable<Area>>> GetAreasByCity(int id)
        {
            var areas = await _context.Areas
       .Where(a => a.CityId == id)
       .Include(a => a.City) 
       .ToListAsync();
            return areas;
        }

    }
}
