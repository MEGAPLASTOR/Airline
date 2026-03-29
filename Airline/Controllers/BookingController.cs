using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Airline.Models;
using System.Security.Claims;

namespace Airline.Controllers
{
    public class BookingController : Controller
    {
        private readonly DataContext _context;

        public BookingController(DataContext context)
        {
            _context = context;
        }

        // 1. Search and Select Flight
        public async Task<IActionResult> BookFlight()
        {
            var schedules = await _context.FlightSchedules
                .Include(s => s.Flight)
                    .ThenInclude(f => f.Route)
                        .ThenInclude(r => r.DepartureCityNavigation)
                .Include(s => s.Flight.Route.ArrivalCityNavigation)
                .Where(s => s.DepartureTime > DateTime.Now && s.AvailableSeats > 0 && s.Status == "SCHEDULED")
                .OrderBy(s => s.DepartureTime)
                .ToListAsync();

            return View(schedules);
        }

        // 2. Select Seat
        public async Task<IActionResult> SelectSeat(int id)
        {
            var schedule = await _context.FlightSchedules
                .Include(s => s.Flight)
                    .ThenInclude(f => f.Route)
                        .ThenInclude(r => r.DepartureCityNavigation)
                .Include(s => s.Flight.Route.ArrivalCityNavigation)
                .Include(s => s.Bookings)
                    .ThenInclude(b => b.Tickets)
                .FirstOrDefaultAsync(s => s.ScheduleId == id);

            if (schedule == null) return NotFound();

            return View(schedule);
        }

        // 3. Passenger Information
        [HttpPost]
        public IActionResult PassengerInfo(int scheduleId, string seatNumber)
        {
            if (string.IsNullOrEmpty(seatNumber))
            {
                return RedirectToAction("SelectSeat", new { id = scheduleId });
            }

            var schedule = _context.FlightSchedules
                .Include(s => s.Flight)
                .FirstOrDefault(s => s.ScheduleId == scheduleId);

            var model = new BookingViewModel
            {
                ScheduleId = scheduleId,
                SeatNumber = seatNumber,
                FlightNumber = schedule?.Flight?.FlightNumber ?? "Unknown",
                DepartureTime = schedule?.DepartureTime ?? DateTime.Now,
                //Price = 1500000
            };

            return View(model);
        }

        // 4. Confirm and Save Booking
        [HttpPost]
        public async Task<IActionResult> ConfirmBooking(BookingViewModel model)
        {
            if (!ModelState.IsValid) return View("PassengerInfo", model);

            // Authentication check
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");
            int userId = int.Parse(userIdStr);

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var schedule = await _context.FlightSchedules.FindAsync(model.ScheduleId);
                    if (schedule == null || schedule.AvailableSeats <= 0)
                        throw new Exception("Flight no longer available.");

                    // 1. Create Booking
                    var booking = new Booking
                    {
                        UserId = userId,
                        ScheduleId = model.ScheduleId,
                        BookingDate = DateTime.Now,
                        BookingType = "ONLINE",
                        Status = "CONFIRMED"
                    };
                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync();

                    // 2. Create Passenger
                    var passenger = new Passenger
                    {
                        BookingId = booking.BookingId,
                        FullName = model.FullName,
                        PassengerType = model.PassengerType
                    };
                    _context.Passengers.Add(passenger);
                    await _context.SaveChangesAsync();

                    // 3. Create Ticket
                    var ticket = new Ticket
                    {
                        BookingId = booking.BookingId,
                        PassengerId = passenger.PassengerId,
                        ClassId = (await _context.TicketClasses.FirstOrDefaultAsync())?.ClassId ?? 1,
                        SeatNumber = model.SeatNumber,
                        Status = "ACTIVE"
                    };
                    _context.Tickets.Add(ticket);

                    // 4. Update Seats
                    schedule.AvailableSeats = (schedule.AvailableSeats ?? 0) - 1;
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return View("BookingSuccess", booking.BookingId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Booking failed: " + ex.Message);
                    return View("PassengerInfo", model);
                }
            }
        }
    }
}
