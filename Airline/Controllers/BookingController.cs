using Airline.Models;
using Airline.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
            var model = await BuildPassengerInfoModelAsync(
                scheduleId,
                seatId,
                promoCode: null,
                skyMilesToRedeem: 0,
                userId: TryGetCurrentUserId());

            if (model == null) return NotFound();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ApplyPromotion()
        {
            var promotions = await _promotionService.GetPublicPromotionsAsync();

            return View(promotions);
        }

        [HttpGet]
        public async Task<IActionResult> ValidatePromotion(int scheduleId, int seatId, string code, int skyMilesToRedeem = 0)
        {
            var seat = await _context.Seats
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SeatId == seatId && s.ScheduleId == scheduleId);

            if (seat == null)
            {
                return Json(new { success = false, message = "Seat or schedule not found." });
            }

            var userId = TryGetCurrentUserId();
            var currentSkyMiles = await LoadCurrentSkyMilesAsync(userId);

            if (!PromotionService.CanRedeemSkyMiles(currentSkyMiles, skyMilesToRedeem))
            {
                return Json(new { success = false, message = "Selected SkyMiles discount is not available for your current balance." });
            }

            var promotion = await _promotionService.ResolvePromotionAsync(code, userId);
            if (promotion == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Promotion code is invalid, expired, or not owned by your account."
                });
            }

            var pricing = await _promotionService.CalculateSingleTicketAsync(
                scheduleId,
                seat.ClassId,
                code,
                userId: userId,
                skyMilesToRedeem: skyMilesToRedeem);

            return Json(new
            {
                success = true,
                promoId = pricing.AppliedPromotionId,
                promoCode = pricing.AppliedPromotionCode,
                discountPercent = pricing.DiscountPercent,
                baseFare = pricing.BaseFare,
                taxAmount = pricing.TaxAmount,
                discountAmount = pricing.DiscountAmount,
                skyMilesRedeemed = pricing.SkyMilesRedeemed,
                skyMilesDiscountPercent = pricing.SkyMilesDiscountPercent,
                skyMilesDiscountAmount = pricing.SkyMilesDiscountAmount,
                finalAmount = pricing.FinalAmount,
                currentSkyMiles,
                maxRedeemableSkyMiles = PromotionService.GetMaxRedeemableSkyMiles(currentSkyMiles)
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

            var userId = TryGetCurrentUserId();
            if (!userId.HasValue) return Redirect("/");

            if (!await PopulatePassengerInfoStateAsync(model))
            {
                return NotFound();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value);
            if (user == null)
            {
                return Redirect("/");
            }

            var pricing = await _promotionService.CalculateSingleTicketAsync(
                model.ScheduleId,
                model.ClassId ?? 0,
                model.PromoCode,
                userId: userId,
                skyMilesToRedeem: model.SkyMilesToRedeem);

            model.AppliedPromotionId = pricing.AppliedPromotionId;
            model.PromoCode = pricing.HasPromotion ? pricing.AppliedPromotionCode : model.PromoCode;
            model.DiscountPercent = pricing.DiscountPercent;
            model.TaxAmount = pricing.TaxAmount;
            model.DiscountAmount = pricing.DiscountAmount;
            model.SkyMilesToRedeem = pricing.SkyMilesRedeemed;
            model.SkyMilesDiscountPercent = pricing.SkyMilesDiscountPercent;
            model.SkyMilesDiscountAmount = pricing.SkyMilesDiscountAmount;
            model.FinalPrice = pricing.FinalAmount;
            model.CurrentSkyMiles = user.SkyMiles ?? 0;

            Promotion? appliedPromotion = null;
            UserPromotion? ownedPromotion = null;
            if (!string.IsNullOrWhiteSpace(model.PromoCode))
            {
                ownedPromotion = await _promotionService.GetOwnedPromotionAsync(userId.Value, model.PromoCode);
                if (ownedPromotion != null)
                {
                    appliedPromotion = ownedPromotion.Promo;
                }
                else
                {
                    appliedPromotion = await _promotionService.GetPromotionByCodeAsync(
                        model.PromoCode,
                        includeSkyMilesExclusive: false);

                    if (appliedPromotion == null)
                    {
                        ModelState.AddModelError(nameof(model.PromoCode), "Promotion code is invalid or expired.");
                    }
                }
            }

            if (!PromotionService.CanRedeemSkyMiles(model.CurrentSkyMiles, model.SkyMilesToRedeem))
            {
                ModelState.AddModelError(nameof(model.SkyMilesToRedeem), "Selected SkyMiles discount is not available for your current balance.");
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
                         throw new Exception("The seat is no longer available or the schedule does not exist.");
                    }

                    // 1. Create Booking
                    var booking = new Booking
                    {
                        UserId = userId.Value,
                        ScheduleId = model.ScheduleId,
                        BookingDate = DateTime.Now,
                        BookingType = "ONLINE",
                        Status = "PENDING_PAYMENT",
                        SkyMilesRedeemed = model.SkyMilesToRedeem
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
                        FullName = model.FullName ?? "New passenger",
                        PassengerType = model.PassengerType ?? "Adult"
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

                    if (model.SkyMilesToRedeem > 0)
                    {
                        user.SkyMiles = Math.Max(0, (user.SkyMiles ?? 0) - model.SkyMilesToRedeem);
                    }

                    if (ownedPromotion != null)
                    {
                        ownedPromotion.IsRedeemed = true;
                        ownedPromotion.RedeemedAt = DateTime.Now;
                        ownedPromotion.RedeemedBookingId = booking.BookingId;
                    }
                     
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    if (model.SkyMilesToRedeem > 0)
                    {
                        await RefreshAuthenticatedUserClaimsAsync(user);
                    }

                    return View("BookingSuccess", new BookingSuccessViewModel
                    {
                        BookingId = booking.BookingId,
                        SkyMilesRedeemed = model.SkyMilesToRedeem,
                        SkyMilesDiscountPercent = model.SkyMilesDiscountPercent,
                        CurrentSkyMiles = user.SkyMiles ?? 0,
                        FinalAmount = model.FinalPrice,
                        PromoCode = appliedPromotion?.PromoCode
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Booking failed: " + ex.Message);
                    await PopulatePassengerInfoStateAsync(model);
                    return View("PassengerInfo", model);
                }
            }
        }

        private async Task<BookingViewModel?> BuildPassengerInfoModelAsync(
            int scheduleId,
            int seatId,
            string? promoCode,
            int skyMilesToRedeem,
            int? userId)
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

            var currentSkyMiles = await LoadCurrentSkyMilesAsync(userId);
            var normalizedSkyMilesToRedeem = PromotionService.CanRedeemSkyMiles(currentSkyMiles, skyMilesToRedeem)
                ? skyMilesToRedeem
                : 0;

            var pricing = await _promotionService.CalculateSingleTicketAsync(
                scheduleId,
                seat.ClassId,
                promoCode,
                userId: userId,
                skyMilesToRedeem: normalizedSkyMilesToRedeem);

            var promotions = await _promotionService.GetPublicPromotionsAsync();
            var ownedPromotions = userId.HasValue
                ? await _promotionService.GetOwnedSkyMilesPromotionsAsync(userId.Value)
                : new List<Promotion>();

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
                PromoCode = pricing.HasPromotion
                    ? pricing.AppliedPromotionCode
                    : string.IsNullOrWhiteSpace(promoCode) ? null : PromotionService.NormalizePromoCode(promoCode),
                AppliedPromotionId = pricing.AppliedPromotionId,
                TaxAmount = pricing.TaxAmount,
                DiscountAmount = pricing.DiscountAmount,
                DiscountPercent = pricing.DiscountPercent,
                SkyMilesToRedeem = pricing.SkyMilesRedeemed,
                SkyMilesDiscountPercent = pricing.SkyMilesDiscountPercent,
                SkyMilesDiscountAmount = pricing.SkyMilesDiscountAmount,
                FinalPrice = pricing.FinalAmount,
                CurrentSkyMiles = currentSkyMiles,
                AvailablePromotions = promotions,
                OwnedSkyMilesPromotions = ownedPromotions
            };
        }

        private async Task<bool> PopulatePassengerInfoStateAsync(BookingViewModel model)
        {
            if (model.SeatId == null)
            {
                return false;
            }

            var hydratedModel = await BuildPassengerInfoModelAsync(
                model.ScheduleId,
                model.SeatId.Value,
                model.PromoCode,
                model.SkyMilesToRedeem,
                TryGetCurrentUserId());
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
            model.PromoCode = hydratedModel.PromoCode;
            model.AppliedPromotionId = hydratedModel.AppliedPromotionId;
            model.TaxAmount = hydratedModel.TaxAmount;
            model.DiscountAmount = hydratedModel.DiscountAmount;
            model.DiscountPercent = hydratedModel.DiscountPercent;
            model.SkyMilesToRedeem = hydratedModel.SkyMilesToRedeem;
            model.SkyMilesDiscountPercent = hydratedModel.SkyMilesDiscountPercent;
            model.SkyMilesDiscountAmount = hydratedModel.SkyMilesDiscountAmount;
            model.FinalPrice = hydratedModel.FinalPrice;
            model.CurrentSkyMiles = hydratedModel.CurrentSkyMiles;
            model.AvailablePromotions = hydratedModel.AvailablePromotions;
            model.OwnedSkyMilesPromotions = hydratedModel.OwnedSkyMilesPromotions;

            return true;
        }

        private async Task<int> LoadCurrentSkyMilesAsync(int? userId)
        {
            if (!userId.HasValue)
            {
                return 0;
            }

            return await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == userId.Value)
                .Select(u => u.SkyMiles ?? 0)
                .FirstOrDefaultAsync();
        }

        private int? TryGetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private async Task RefreshAuthenticatedUserClaimsAsync(User user)
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return;
            }

            var authResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username ?? string.Empty),
                new Claim("FirstName", user.FirstName ?? string.Empty),
                new Claim("LastName", user.LastName ?? string.Empty),
                new Claim("SkyMiles", (user.SkyMiles ?? 0).ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? string.Empty)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authResult.Properties ?? new AuthenticationProperties());
        }
    }
}
