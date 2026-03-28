using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    [Route("Admin")]
    public class AdminFlightController : AdminBaseController
    {
        public AdminFlightController(DataContext context) : base(context) { }

        [HttpGet("ManageFlights")]
        public async Task<IActionResult> ManageFlights()
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var flights = await _context.Flights
                .Include(x => x.Route)
                .OrderByDescending(x => x.FlightId)
                .ToListAsync();

            return View("~/Views/Admin/ManageFlights.cshtml", flights);
        }
    }
}
