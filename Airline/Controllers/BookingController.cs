using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Airline.Models;
using System.Security.Claims;
using Airline.Services;

namespace Airline.Controllers
{
    public class BookingController : Controller
    {
        private readonly DataContext _context;
        private readonly SeatService _seatService;
        private readonly PromotionService _promotionService;

        public BookingController(
            DataContext context,
            SeatService seatService,
            PromotionService promotionService)
        {
            _context = context;
            _seatService = seatService;
            _promotionService = promotionService;
        }

        // 1. Search and Select Flight
        public async Task<IActionResult> BookFlight(string origin, string destination)
        {
            var query = _context.FlightSchedules
                .Include(s => s.Flight)
                    .ThenInclude(f => f.Route)
                        .ThenInclude(r => r.DepartureCityNavigation)
                .Include(s => s.Flight.Route.ArrivalCityNavigation)
                .Include(s => s.TicketPrices)
                .Where(s => s.DepartureTime > DateTime.Now && s.Status == "SCHEDULED" && s.TicketPrices.Any())
                .OrderBy(s => s.DepartureTime)
                .AsQueryable();

            if (!string.IsNullOrEmpty(origin))
            {
                query = query.Where(s => s.Flight.Route.DepartureCityNavigation.CityName.Contains(origin));
            }

            if (!string.IsNullOrEmpty(destination))
            {
                query = query.Where(s => s.Flight.Route.ArrivalCityNavigation.CityName.Contains(destination));
            }

            var schedules = await query.ToListAsync();
            ViewBag.Origin = origin;
            ViewBag.Destination = destination;

            return View(schedules);
        }

        // 2. Select Seat
        public async Task<IActionResult> SelectSeat(int id)
        {
            // Ensure seats exist for this schedule (4-cabin generation)
            await _seatService.GenerateSeatsAsync(id);

            var schedule = await _context.FlightSchedules
                .Include(s => s.Flight)
                    .ThenInclude(f => f.Route)
                        .ThenInclude(r => r.DepartureCityNavigation)
                .Include(s => s.Flight.Route.ArrivalCityNavigation)
                .Include(s => s.Seats)
                    .ThenInclude(seat => seat.Class)
                .Include(s => s.TicketPrices)
                    .ThenInclude(p => p.Class)
                .FirstOrDefaultAsync(s => s.ScheduleId == id);

            if (schedule == null) return NotFound();

            return View(schedule);
        }

        // 3. Passenger Information
        [HttpPost]
        public async Task<IActionResult> PassengerInfo(int scheduleId, int seatId)
        {
            var model = await BuildPassengerInfoModelAsync(scheduleId, seatId);
            if (model == null) return NotFound();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ApplyPromotion()
        {
            var promotions = await _context.Promotions
                .AsNoTracking()
                .OrderByDescending(p => p.DiscountPercent ?? 0)
                .ThenBy(p => p.StartDate)
                .ToListAsync();

            return View(promotions);
        }

        [HttpGet]
        public async Task<IActionResult> ValidatePromotion(int scheduleId, int seatId, string code)
        {
            var seat = await _context.Seats
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SeatId == seatId && s.ScheduleId == scheduleId);

            if (seat == null)
            {
                return Json(new { success = false, message = "Seat or schedule not found." });
            }

            var promotion = await _promotionService.GetPromotionByCodeAsync(code);
            if (promotion == null)
            {
                return Json(new { success = false, message = "Promotion code is invalid or expired." });
            }

            var pricing = await _promotionService.CalculateSingleTicketAsync(scheduleId, seat.ClassId, code);

            return Json(new
            {
                success = true,
                promoId = pricing.AppliedPromotionId,
                promoCode = pricing.AppliedPromotionCode,
                discountPercent = pricing.DiscountPercent,
                baseFare = pricing.BaseFare,
                taxAmount = pricing.TaxAmount,
                discountAmount = pricing.DiscountAmount,
                finalAmount = pricing.FinalAmount
            });
        }

        // GET: ConfirmBooking (Redirect back if hit directly or refreshed)
        [HttpGet]
        public IActionResult ConfirmBooking()
        {
            return RedirectToAction("BookFlight");
        }

        // POST: Confirm and Save Booking
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ConfirmBooking([FromForm] BookingViewModel model)
        {
            if (model == null) return BadRequest("Model is null");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Redirect("/");
            int userId = int.Parse(userIdStr);

            if (!await PopulatePassengerInfoStateAsync(model))
            {
                return NotFound();
            }

            Promotion? appliedPromotion = null;
            if (!string.IsNullOrWhiteSpace(model.PromoCode))
            {
                appliedPromotion = await _promotionService.GetPromotionByCodeAsync(model.PromoCode);
                if (appliedPromotion == null)
                {
                    ModelState.AddModelError(nameof(model.PromoCode), "Promotion code is invalid or expired.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View("PassengerInfo", model);
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var schedule = await _context.FlightSchedules
                        .Include(s => s.Flight)
                            .ThenInclude(f => f.Route)
                                .ThenInclude(r => r.DepartureCityNavigation)
                        .Include(s => s.Flight.Route.ArrivalCityNavigation)
                        .FirstOrDefaultAsync(s => s.ScheduleId == model.ScheduleId);

                    var seat = await _context.Seats.FindAsync(model.SeatId);

                    if (schedule == null || seat == null || seat.SeatStatus != "AVAILABLE")
                    {
                         throw new Exception("Chỗ ngồi không còn sẵn hoặc lịch trình không tồn tại.");
                    }

                    // 1. Create Booking
                    var booking = new Booking
                    {
                        UserId = userId,
                        ScheduleId = model.ScheduleId,
                        BookingDate = DateTime.Now,
                        BookingType = "ONLINE",
                        Status = "PENDING_PAYMENT"
                    };
                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync();

                    if (appliedPromotion != null)
                    {
                        _context.BookingPromotions.Add(new BookingPromotion
                        {
                            BookingId = booking.BookingId,
                            PromoId = appliedPromotion.PromoId
                        });
                        await _context.SaveChangesAsync();
                    }

                    // 2. Create Passenger
                    var passenger = new Passenger
                    {
                        BookingId = booking.BookingId,
                        FullName = model.FullName ?? "Người mới",
                        PassengerType = model.PassengerType ?? "Người lớn"
                    };
                    _context.Passengers.Add(passenger);
                    await _context.SaveChangesAsync();

                    // 3. Create Ticket
                    var ticket = new Ticket
                    {
                        BookingId = booking.BookingId,
                        PassengerId = passenger.PassengerId,
                        ClassId = seat.ClassId,
                        SeatId = seat.SeatId,
                        Status = "BOOKED"
                    };
                    _context.Tickets.Add(ticket);

                    // 4. Update Seat & Schedule
                    seat.SeatStatus = "BOOKED";
                    schedule.AvailableSeats = Math.Max(0, (schedule.AvailableSeats ?? 0) - 1);
                    
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return View("BookingSuccess", booking.BookingId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Đặt vé thất bại: " + ex.Message);
                    await PopulatePassengerInfoStateAsync(model);
                    return View("PassengerInfo", model);
                }
            }
        }

        private async Task<BookingViewModel?> BuildPassengerInfoModelAsync(int scheduleId, int seatId)
        {
            var schedule = await _context.FlightSchedules
                .AsNoTracking()
                .Include(s => s.Flight)
                    .ThenInclude(f => f.Route)
                        .ThenInclude(r => r.DepartureCityNavigation)
                .Include(s => s.Flight.Route.ArrivalCityNavigation)
                .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);

            var seat = await _context.Seats
                .AsNoTracking()
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.SeatId == seatId && s.ScheduleId == scheduleId);

            if (schedule == null || seat == null)
            {
                return null;
            }

            var pricing = await _promotionService.CalculateSingleTicketAsync(scheduleId, seat.ClassId);
            var promotions = await _promotionService.GetActivePromotionsAsync();

            return new BookingViewModel
            {
                ScheduleId = scheduleId,
                SeatId = seatId,
                ClassId = seat.ClassId,
                SeatNumber = seat.SeatNumber,
                FlightNumber = schedule.Flight?.FlightNumber ?? "Unknown",
                DepartureTime = schedule.DepartureTime,
                Origin = schedule.Flight?.Route?.DepartureCityNavigation?.CityName ?? "Unknown",
                Destination = schedule.Flight?.Route?.ArrivalCityNavigation?.CityName ?? "Unknown",
                Price = pricing.BaseFare,
                TaxAmount = pricing.TaxAmount,
                DiscountAmount = pricing.DiscountAmount,
                DiscountPercent = pricing.DiscountPercent,
                FinalPrice = pricing.FinalAmount,
                AvailablePromotions = promotions
            };
        }

        private async Task<bool> PopulatePassengerInfoStateAsync(BookingViewModel model)
        {
            if (model.SeatId == null)
            {
                return false;
            }

            var hydratedModel = await BuildPassengerInfoModelAsync(model.ScheduleId, model.SeatId.Value);
            if (hydratedModel == null)
            {
                return false;
            }

            model.ClassId = hydratedModel.ClassId;
            model.SeatNumber = hydratedModel.SeatNumber;
            model.Origin = hydratedModel.Origin;
            model.Destination = hydratedModel.Destination;
            model.DepartureTime = hydratedModel.DepartureTime;
            model.FlightNumber = hydratedModel.FlightNumber;
            model.Price = hydratedModel.Price;
            model.TaxAmount = hydratedModel.TaxAmount;
            model.DiscountAmount = hydratedModel.DiscountAmount;
            model.DiscountPercent = hydratedModel.DiscountPercent;
            model.FinalPrice = hydratedModel.FinalPrice;
            model.AvailablePromotions = hydratedModel.AvailablePromotions;

            if (!string.IsNullOrWhiteSpace(model.PromoCode) && model.ClassId.HasValue)
            {
                var normalizedPromoCode = PromotionService.NormalizePromoCode(model.PromoCode);
                var pricing = await _promotionService.CalculateSingleTicketAsync(
                    model.ScheduleId,
                    model.ClassId.Value,
                    normalizedPromoCode);

                model.AppliedPromotionId = pricing.AppliedPromotionId;
                model.PromoCode = pricing.HasPromotion ? pricing.AppliedPromotionCode : normalizedPromoCode;
                model.DiscountPercent = pricing.DiscountPercent;
                model.TaxAmount = pricing.TaxAmount;
                model.DiscountAmount = pricing.DiscountAmount;
                model.FinalPrice = pricing.FinalAmount;
            }
            else
            {
                model.AppliedPromotionId = null;
            }

            return true;
        }
    }
}
