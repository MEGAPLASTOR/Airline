using Airline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Airline.Controllers
{
    public class TicketController : Controller
    {
        private readonly DataContext _context;

        public TicketController(DataContext context)
        {
            _context = context;
        }

        // ══════════════════════════════════════════════════════════════
        // GET /Ticket/ViewConfirmation
        // ══════════════════════════════════════════════════════════════
        public async Task<IActionResult> ViewConfirmation()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) 
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdStr);

            var tickets = await _context.Tickets
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Schedule)
                        .ThenInclude(s => s.Flight)
                            .ThenInclude(f => f.Route)
                                .ThenInclude(r => r.DepartureCityNavigation)
                .Include(t => t.Booking.Schedule.Flight.Route.ArrivalCityNavigation)
                .Include(t => t.Passenger)
                .Include(t => t.Class)
                .Include(t => t.Seat)
                .Where(t => t.Booking.UserId == userId)
                .OrderByDescending(t => t.Booking.BookingDate)
                .ToListAsync();

            return View(tickets);
        }

        // ══════════════════════════════════════════════════════════════
        // GET /Ticket/ChangeSeat/{id}
        // ══════════════════════════════════════════════════════════════
        public async Task<IActionResult> ChangeSeat(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) 
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdStr);

            var ticket = await _context.Tickets
                .Include(t => t.Passenger)
                .Include(t => t.Seat)
                .Include(t => t.Class)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Schedule)
                        .ThenInclude(s => s.Flight)
                            .ThenInclude(f => f.Route)
                .FirstOrDefaultAsync(t => t.TicketId == id && t.Booking.UserId == userId);

            if (ticket == null || ticket.Booking?.Schedule == null) return NotFound("Không tìm thấy thông tin vé hoặc lịch trình");

            // Get all available seats for this schedule and class
            var availableSeats = await _context.Seats
                .Where(s => s.ScheduleId == ticket.Booking.ScheduleId 
                            && s.ClassId == ticket.ClassId 
                            && s.SeatStatus == "AVAILABLE")
                .ToListAsync();

            ViewBag.AvailableSeats = availableSeats;
            ViewBag.CurrentSeatNumber = ticket.Seat?.SeatNumber ?? "Chưa có";

            return View(ticket);
        }

        // ══════════════════════════════════════════════════════════════
        // POST /Ticket/UpdateSeat
        // ══════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSeat(int ticketId, int newSeatId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) 
                return Json(new { success = false, message = "Không có quyền truy cập" });

            int userId = int.Parse(userIdStr);

            var ticket = await _context.Tickets
                .Include(t => t.Booking)
                .Include(t => t.Seat)
                .FirstOrDefaultAsync(t => t.TicketId == ticketId && t.Booking.UserId == userId);

            if (ticket == null)
                return Json(new { success = false, message = "Vé không tồn tại" });

            var newSeat = await _context.Seats.FindAsync(newSeatId);
            if (newSeat == null || newSeat.SeatStatus != "AVAILABLE")
                return Json(new { success = false, message = "Ghế mới không còn sẵn" });

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Release old seat
                    if (ticket.Seat != null)
                    {
                        ticket.Seat.SeatStatus = "AVAILABLE";
                    }

                    // 2. Assign new seat
                    ticket.SeatId = newSeatId;
                    newSeat.SeatStatus = "BOOKED";

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = "Đổi chỗ ngồi thành công!" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Lỗi: " + ex.Message });
                }
            }
        }
    }
}
