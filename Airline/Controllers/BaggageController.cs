using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    public class BaggageController : Controller
    {
        private readonly DataContext _context;

        public BaggageController(DataContext context)
        {
            _context = context;
        }

        // ══════════════════════════════════════════════════════════════
        // GET /Baggage/Register/{id?}
        // ══════════════════════════════════════════════════════════════
        public async Task<IActionResult> Register(int? id)
        {
            if (User?.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Account");

            var username = User.Identity.Name;

            if (id.HasValue)
            {
                var ticket = await _context.Tickets
                    .Include(t => t.Booking)
                        .ThenInclude(b => b.Schedule)
                            .ThenInclude(s => s.Flight)
                    .Include(t => t.Baggages)
                    .FirstOrDefaultAsync(t => t.TicketId == id.Value && t.Booking.User.Username == username);

                if (ticket == null) return NotFound();

                return View("RegisterForm", ticket);
            }
            else
            {
                // Show list of tickets to choose from
                var tickets = await _context.Tickets
                    .Include(t => t.Booking)
                        .ThenInclude(b => b.Schedule)
                            .ThenInclude(s => s.Flight)
                                .ThenInclude(f => f.Route)
                                    .ThenInclude(r => r.DepartureCityNavigation)
                    .Include(t => t.Booking.Schedule.Flight.Route.ArrivalCityNavigation)
                    .Where(t => t.Booking.User.Username == username && t.Status != "CANCELLED")
                    .OrderByDescending(t => t.Booking.BookingDate)
                    .ToListAsync();

                return View("SelectTicket", tickets);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // POST /Baggage/ProcessRegister
        // ══════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRegister(int ticketId, decimal weight)
        {
            if (User?.Identity?.IsAuthenticated != true)
                return Json(new { success = false, message = "Not authenticated" });

            var username = User.Identity.Name;
            var ticket = await _context.Tickets
                .Include(t => t.Booking)
                .FirstOrDefaultAsync(t => t.TicketId == ticketId && t.Booking.User.Username == username);

            if (ticket == null)
                return Json(new { success = false, message = "Ticket not found" });

            // Pricing logic: 20,000 VND per kg
            decimal price = weight * 20000;

            var baggage = new Baggage
            {
                TicketId = ticketId,
                Weight = weight,
                Price = price
            };

            _context.Baggages.Add(baggage);
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = $"Đăng ký thành công {weight}kg hành lý. Tổng phí: {price:N0} VND" 
            });
        }
    }
}
