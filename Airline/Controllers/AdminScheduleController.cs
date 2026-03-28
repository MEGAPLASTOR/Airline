using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    [Route("Admin")]
    public class AdminScheduleController : AdminBaseController
    {
        public AdminScheduleController(DataContext context) : base(context) { }

        [HttpGet("FlightSchedules")]
        public async Task<IActionResult> FlightSchedules()
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var schedules = await _context.FlightSchedules
                .Include(x => x.Flight)
                .OrderByDescending(x => x.ScheduleId)
                .ToListAsync();

            return View("~/Views/Admin/FlightSchedules.cshtml", schedules);
        }

        [HttpGet("FlightReschedule")]
        public async Task<IActionResult> FlightReschedule()
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var schedules = await _context.FlightSchedules
                .Include(x => x.Flight)
                .OrderByDescending(x => x.ScheduleId)
                .ToListAsync();

            return View("~/Views/Admin/FlightReschedule.cshtml", schedules);
        }
    }
}
