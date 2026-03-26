using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    public class SeatController : Controller
    {
        private readonly DataContext _context;

        public SeatController(DataContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var isAuth = User?.Identity?.IsAuthenticated == true;
            var role = User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            return isAuth && string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase);
        }

        public IActionResult Index(int? flightId)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            if (!flightId.HasValue)
            {
                var flights = _context.Flights
                    .Include(f => f.Route)
                        .ThenInclude(r => r.DepartureCityNavigation)
                    .Include(f => f.Route)
                        .ThenInclude(r => r.ArrivalCityNavigation)
                    .Include(f => f.Seats)
                    .ToList();
                return View("ListFlights", flights);
            }

            var flight = _context.Flights
                .Include(f => f.Seats)
                    .ThenInclude(s => s.Class)
                .Include(f => f.Route)
                    .ThenInclude(r => r.DepartureCityNavigation)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ArrivalCityNavigation)
                .FirstOrDefault(f => f.FlightId == flightId.Value);

            if (flight == null) return NotFound();

            ViewBag.TicketClasses = _context.TicketClasses.ToList();
            return View(flight);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(int flightId, string seatNumber, int classId)
        {
            if (!IsAdmin()) return Unauthorized();

            if (string.IsNullOrWhiteSpace(seatNumber))
                return Json(new { success = false, message = "Seat number is required." });

            seatNumber = seatNumber.Trim().ToUpper();

            if (_context.Seats.Any(s => s.FlightId == flightId && s.SeatNumber == seatNumber))
                return Json(new { success = false, message = "Seat number already exists for this flight." });

            try
            {
                var seat = new Seat
                {
                    FlightId = flightId,
                    SeatNumber = seatNumber,
                    ClassId = classId,
                    IsActive = true
                };
                _context.Seats.Add(seat);
                _context.SaveChanges();
                return Json(new { success = true, message = "Seat added successfully." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while adding the seat." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleStatus(int seatId)
        {
            if (!IsAdmin()) return Unauthorized();

            var seat = _context.Seats.Find(seatId);
            if (seat == null) return NotFound();

            try
            {
                seat.IsActive = !seat.IsActive;
                _context.SaveChanges();
                return Json(new { success = true, isActive = seat.IsActive });
            }
            catch (Exception)
            {
                return Json(new { success = false });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int seatId)
        {
            if (!IsAdmin()) return Unauthorized();

            var seat = _context.Seats.Find(seatId);
            if (seat == null) return NotFound();

            try
            {
                _context.Seats.Remove(seat);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false });
            }
        }
    }
}
