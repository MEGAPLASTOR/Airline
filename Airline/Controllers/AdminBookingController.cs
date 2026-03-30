using Airline.Models;
using Airline.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    [Route("Admin")]
    public class AdminBookingController : AdminBaseController
    {
        private readonly PromotionService _promotionService;

        public AdminBookingController(DataContext context, PromotionService promotionService) : base(context)
        {
            _promotionService = promotionService;
        }

        // ══════════════════════════════════════════════════════════════
        // GET /Admin/ConfirmTicket
        // ══════════════════════════════════════════════════════════════
        [HttpGet("ConfirmTicket")]
        public async Task<IActionResult> ConfirmTicket()
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            // Lấy các Booking có trạng thái CONFIRMED (đã đặt nhưng chưa thanh toán/xác nhận)
            // Loại bỏ các booking đã có bản ghi thanh toán SUCCESS
            var pendingBookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Flight)
                        .ThenInclude(f => f.Route)
                            .ThenInclude(r => r.DepartureCityNavigation)
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Flight)
                        .ThenInclude(f => f.Route)
                            .ThenInclude(r => r.ArrivalCityNavigation)
                .Include(b => b.Tickets)
                .Include(b => b.Passengers)
                .Include(b => b.BookingPromotions)
                    .ThenInclude(bp => bp.Promo)
                .Where(b => b.Status == "CONFIRMED" || b.Status == "PENDING_PAYMENT")
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            // Tính toán giá vé cho từng booking để hiển thị (do DB không lưu TotalAmount trực tiếp)
            foreach (var booking in pendingBookings)
            {
                var pricing = await _promotionService.CalculateBookingAsync(booking);
                ViewData[$"Total_{booking.BookingId}"] = pricing.FinalAmount;
            }

            return View("~/Views/Admin/ConfirmTicket.cshtml", pendingBookings);
        }

        // ══════════════════════════════════════════════════════════════
        // POST /Admin/ProcessConfirm
        // ══════════════════════════════════════════════════════════════
        [HttpPost("ProcessConfirm")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessConfirm(int bookingId)
        {
            if (!IsAdmin()) return Unauthorized();

            var booking = await _context.Bookings
                .Include(b => b.Tickets)
                .Include(b => b.BookingPromotions)
                    .ThenInclude(bp => bp.Promo)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return NotFound();
            if (booking.Status == "PAID") 
            {
                TempData["ErrorMessage"] = "This booking is already paid.";
                return RedirectToAction(nameof(ConfirmTicket));
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Cập nhật trạng thái Booking
                    booking.Status = "PAID";

                    decimal totalAmount = 0;
                    // 2. Cập nhật trạng thái từng Ticket
                    foreach (var ticket in booking.Tickets)
                    {
                        ticket.Status = "PAID";
                        
                        // Lấy giá thực tế
                        var priceEntry = await _context.TicketPrices
                            .FirstOrDefaultAsync(p => p.ScheduleId == booking.ScheduleId && p.ClassId == ticket.ClassId);
                        totalAmount += priceEntry?.Price ?? 1500000;
                    }

                    // 3. Tạo bản ghi Payment (Xác nhận thủ công bởi Admin)
                    var payment = new Payment
                    {
                        BookingId = bookingId,
                        Amount = (await _promotionService.CalculateBookingAsync(booking)).FinalAmount,
                        PaymentDate = DateTime.Now,
                        PaymentMethod = "MANUAL_ADMIN", // Đánh dấu xác nhận bởi Admin
                        PaymentStatus = "SUCCESS",
                        TransactionNo = "MANUAL_APPROVAL"
                    };
                    _context.Payments.Add(payment);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Successfully confirmed Booking #{bookingId}.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = $"Error confirming booking: {ex.Message}";
                }
            }

            return RedirectToAction(nameof(ConfirmTicket));
        }
    }
}
