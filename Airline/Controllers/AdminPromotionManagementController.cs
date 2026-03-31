using Airline.Models;
using Airline.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    [Route("Admin")]
    public class AdminPromotionManagementController : AdminBaseController
    {
        public AdminPromotionManagementController(DataContext context) : base(context)
        {
        }

        [HttpGet("ManagePromotions")]
        public async Task<IActionResult> ManagePromotions()
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var promotions = await _context.Promotions
                .Include(p => p.BookingPromotions)
                .OrderByDescending(p => p.StartDate)
                .ThenByDescending(p => p.DiscountPercent)
                .ToListAsync();

            return View("~/Views/Admin/ManagePromotions.cshtml", promotions);
        }

        [HttpGet("GetPromotion/{id}")]
        public async Task<IActionResult> GetPromotion(int id)
        {
            if (!IsAdmin()) return Unauthorized();

            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null) return NotFound();

            return Json(new
            {
                promotion.PromoId,
                promotion.PromoCode,
                promotion.DiscountPercent,
                startDate = promotion.StartDate?.ToString("yyyy-MM-dd"),
                endDate = promotion.EndDate?.ToString("yyyy-MM-dd")
            });
        }

        [HttpPost("CreatePromotion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePromotion(
            [FromForm] string promoCode,
            [FromForm] int discountPercent,
            [FromForm] DateOnly startDate,
            [FromForm] DateOnly endDate)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            var validationError = await ValidatePromotionInputAsync(0, promoCode, discountPercent, startDate, endDate);
            if (validationError != null)
            {
                return Json(new { success = false, message = validationError });
            }

            _context.Promotions.Add(new Promotion
            {
                PromoCode = PromotionService.NormalizePromoCode(promoCode),
                DiscountPercent = discountPercent,
                StartDate = startDate,
                EndDate = endDate
            });

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost("EditPromotion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPromotion(
            [FromForm] int id,
            [FromForm] string promoCode,
            [FromForm] int discountPercent,
            [FromForm] DateOnly startDate,
            [FromForm] DateOnly endDate)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null) return Json(new { success = false, message = "Promotion not found." });

            var validationError = await ValidatePromotionInputAsync(id, promoCode, discountPercent, startDate, endDate);
            if (validationError != null)
            {
                return Json(new { success = false, message = validationError });
            }

            promotion.PromoCode = PromotionService.NormalizePromoCode(promoCode);
            promotion.DiscountPercent = discountPercent;
            promotion.StartDate = startDate;
            promotion.EndDate = endDate;

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost("DeletePromotion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePromotion([FromForm] int id)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            var promotion = await _context.Promotions
                .Include(p => p.BookingPromotions)
                .FirstOrDefaultAsync(p => p.PromoId == id);

            if (promotion == null) return Json(new { success = false, message = "Promotion not found." });

            if (promotion.BookingPromotions.Any())
            {
                return Json(new { success = false, message = "This promotion is already used in bookings and cannot be deleted." });
            }

            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        private async Task<string?> ValidatePromotionInputAsync(
            int currentPromotionId,
            string promoCode,
            int discountPercent,
            DateOnly startDate,
            DateOnly endDate)
        {
            if (string.IsNullOrWhiteSpace(promoCode))
            {
                return "Promotion code is required.";
            }

            if (discountPercent <= 0 || discountPercent > 100)
            {
                return "Discount percent must be between 1 and 100.";
            }

            if (endDate < startDate)
            {
                return "End date must be later than or equal to start date.";
            }

            var normalizedCode = PromotionService.NormalizePromoCode(promoCode);
            var isDuplicate = await _context.Promotions
                .AnyAsync(p => p.PromoId != currentPromotionId && p.PromoCode == normalizedCode);

            if (isDuplicate)
            {
                return "Promotion code already exists.";
            }

            return null;
        }
    }
}
