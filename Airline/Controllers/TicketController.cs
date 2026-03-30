//using Airline.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace Airline.Controllers
//{
//    public class TicketController : Controller
//    {
//        private readonly DataContext _context;

//        public TicketController(DataContext context)
//        {
//            _context = context;
//        }

//        // ══════════════════════════════════════════════════════════════
//        // GET /Ticket/ViewConfirmation
//        // ══════════════════════════════════════════════════════════════
//        public async Task<IActionResult> ViewConfirmation()
//        {
//            if (User?.Identity?.IsAuthenticated != true)
//                return RedirectToAction("Login", "Account");

//            var username = User.Identity.Name;
//            var tickets = await _context.Tickets
//                .Include(t => t.Booking)
//                    .ThenInclude(b => b.Schedule)
//                        .ThenInclude(s => s.Flight)
//                            .ThenInclude(f => f.Route)
//                                .ThenInclude(r => r.DepartureCityNavigation)
//                .Include(t => t.Booking.Schedule.Flight.Route.ArrivalCityNavigation)
//                .Include(t => t.Passenger)
//                .Include(t => t.Class)
//                .Where(t => t.Booking.User.Username == username)
//                .OrderByDescending(t => t.Booking.BookingDate)
//                .ToListAsync();

//            return View(tickets);
//        }

//        // ══════════════════════════════════════════════════════════════
//        // GET /Ticket/ChangeSeat/{id}
//        // ══════════════════════════════════════════════════════════════
//        public async Task<IActionResult> ChangeSeat(int id)
//        {
//            if (User?.Identity?.IsAuthenticated != true)
//                return RedirectToAction("Login", "Account");

//            var username = User.Identity.Name;
//            var ticket = await _context.Tickets
//                .Include(t => t.Booking)
//                    .ThenInclude(b => b.Schedule)
//                        .ThenInclude(s => s.Flight)
//                .Include(t => t.Passenger)
//                .FirstOrDefaultAsync(t => t.TicketId == id && t.Booking.User.Username == username);

//            if (ticket == null) return NotFound();

//            // Lấy tất cả ghế đã đặt trong lịch trình này
//            var occupiedSeats = await _context.Tickets
//                .Where(t => t.Booking.ScheduleId == ticket.Booking.ScheduleId && t.Status != "CANCELLED")
//                .Select(t => t.SeatNumber)
//                .ToListAsync();
//            ViewBag.OccupiedSeats = occupiedSeats;
//            ViewBag.CurrentSeat = ticket.SeatNumber;

//            return View(ticket);
//        }

//        // ══════════════════════════════════════════════════════════════
//        // POST /Ticket/UpdateSeat
//        // ══════════════════════════════════════════════════════════════
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> UpdateSeat(int ticketId, string newSeatNumber)
//        {
//            if (User?.Identity?.IsAuthenticated != true)
//                return Json(new { success = false, message = "Not authenticated" });

//            var username = User.Identity.Name;
//            var ticket = await _context.Tickets
//                .Include(t => t.Booking)
//                .FirstOrDefaultAsync(t => t.TicketId == ticketId && t.Booking.User.Username == username);

//            if (ticket == null)
//                return Json(new { success = false, message = "Ticket not found" });

//            // Kiểm tra ghế mới có đang trống không
//            var isOccupied = await _context.Tickets
//                .AnyAsync(t => t.Booking.ScheduleId == ticket.Booking.ScheduleId 
//                            && t.SeatNumber == newSeatNumber 
//                            && t.Status != "CANCELLED"
//                            && t.TicketId != ticketId);

//            if (isOccupied)
//                return Json(new { success = false, message = "Ghế này hiện đã được đặt. Vui lòng chọn ghế khác." });

//            // Cập nhật ghế
//            ticket.SeatNumber = newSeatNumber;
//            await _context.SaveChangesAsync();

//            return Json(new { success = true, message = "Đổi chỗ ngồi thành công!" });
//        }
//    }
//}
