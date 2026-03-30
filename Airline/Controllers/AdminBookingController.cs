using Airline.Models;
using Airline.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    [Route("Admin")]
    public class AdminBookingController : AdminBaseController
    {
        private readonly BookingPaymentService _bookingPaymentService;
        private readonly PromotionService _promotionService;

        public AdminBookingController(
            DataContext context,
            PromotionService promotionService,
            BookingPaymentService bookingPaymentService) : base(context)
        {
            _promotionService = promotionService;
            _bookingPaymentService = bookingPaymentService;
        }

        // ==============================================================
        // GET /Admin/ConfirmTicket
        // ==============================================================
        [HttpGet("ConfirmTicket")]
        public async Task<IActionResult> ConfirmTicket()
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            // Load bookings that were created but still need payment or manual confirmation.
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

            // Calculate the fare for each booking because TotalAmount is not stored directly.
            foreach (var booking in pendingBookings)
            {
                var pricing = await _promotionService.CalculateBookingAsync(booking);
                ViewData[$"Total_{booking.BookingId}"] = pricing.FinalAmount;
            }

            return View("~/Views/Admin/ConfirmTicket.cshtml", pendingBookings);
        }

        // ==============================================================
        // POST /Admin/ProcessConfirm
        // ==============================================================
        [HttpPost("ProcessConfirm")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessConfirm(int bookingId)
        {
            if (!IsAdmin()) return Unauthorized();

            var booking = await _context.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return NotFound();
            if (booking.Status == "PAID")
            {
                TempData["ErrorMessage"] = "This booking is already paid.";
                return RedirectToAction(nameof(ConfirmTicket));
            }

            var paymentResult = await _bookingPaymentService.FinalizeSuccessfulBookingPaymentAsync(
                bookingId,
                "MANUAL_ADMIN",
                $"MANUAL_APPROVAL_{bookingId}_{DateTime.Now.Ticks}");

            if (!paymentResult.IsHandled)
            {
                TempData["ErrorMessage"] = $"Error confirming booking #{bookingId}.";
            }
            else
            {
                TempData["SuccessMessage"] = paymentResult.SkyMilesAwarded > 0
                    ? $"Successfully confirmed Booking #{bookingId} and awarded {paymentResult.SkyMilesAwarded:N0} SkyMiles."
                    : $"Successfully confirmed Booking #{bookingId}.";
            }

            return RedirectToAction(nameof(ConfirmTicket));
        }
    }
}
