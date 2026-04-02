using Airline.Models;
using Airline.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Airline.Controllers
{
    [Route("Admin")]
    public class AdminSkyMilesController : AdminBaseController
    {
        public AdminSkyMilesController(DataContext context) : base(context)
        {
        }

        [HttpGet("ManageSkyMiles")]
        public async Task<IActionResult> ManageSkyMiles(int? editId = null)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var viewModel = await BuildViewModelAsync(editId);
            return View("~/Views/Admin/ManageSkyMiles.cshtml", viewModel);
        }

        [HttpPost("SaveSkyMilesPromotion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSkyMilesPromotion(SkyMilesPromotionFormModel form)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            if (!ModelState.IsValid)
            {
                var invalidViewModel = await BuildViewModelAsync(form: form);
                return View("~/Views/Admin/ManageSkyMiles.cshtml", invalidViewModel);
            }

            var validationError = await ValidateFormAsync(form);
            if (validationError != null)
            {
                ModelState.AddModelError(string.Empty, validationError);
                var invalidViewModel = await BuildViewModelAsync(form: form);
                return View("~/Views/Admin/ManageSkyMiles.cshtml", invalidViewModel);
            }

            Promotion promotion;

            if (form.PromoId.HasValue)
            {
                promotion = await _context.Promotions
                    .FirstOrDefaultAsync(p => p.PromoId == form.PromoId.Value && p.IsSkyMilesExclusive)
                    ?? throw new InvalidOperationException("SkyMiles promotion not found.");
            }
            else
            {
                promotion = new Promotion();
                _context.Promotions.Add(promotion);
            }

            promotion.Title = form.Title.Trim();
            promotion.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
            promotion.PromoCode = PromotionService.NormalizePromoCode(form.PromoCode);
            promotion.DiscountPercent = form.DiscountPercent;
            promotion.SkyMilesCost = form.SkyMilesCost;
            promotion.IsSkyMilesExclusive = true;
            promotion.OnlyForSkyMilesPayment = false;
            promotion.StartDate = form.StartDate;
            promotion.EndDate = form.EndDate;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = form.PromoId.HasValue
                ? "SkyMiles shop promotion updated successfully."
                : "SkyMiles shop promotion created successfully.";

            return RedirectToAction(nameof(ManageSkyMiles));
        }

        [HttpPost("DeleteSkyMilesPromotion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSkyMilesPromotion(int id)
        {
            if (!IsAdmin()) return RedirectIfNotAdmin();

            var promotion = await _context.Promotions
                .Include(p => p.BookingPromotions)
                .Include(p => p.UserPromotions)
                .FirstOrDefaultAsync(p => p.PromoId == id && p.IsSkyMilesExclusive);

            if (promotion == null)
            {
                TempData["ErrorMessage"] = "SkyMiles promotion not found.";
                return RedirectToAction(nameof(ManageSkyMiles));
            }

            if (promotion.UserPromotions.Any())
            {
                TempData["ErrorMessage"] = "This reward code has already been purchased by users and cannot be deleted.";
                return RedirectToAction(nameof(ManageSkyMiles));
            }

            if (promotion.BookingPromotions.Any())
            {
                TempData["ErrorMessage"] = "This reward code has already been used in bookings and cannot be deleted.";
                return RedirectToAction(nameof(ManageSkyMiles));
            }

            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "SkyMiles shop promotion deleted successfully.";
            return RedirectToAction(nameof(ManageSkyMiles));
        }

        private async Task<ManageSkyMilesViewModel> BuildViewModelAsync(int? editId = null, SkyMilesPromotionFormModel? form = null)
        {
            var promotions = await _context.Promotions
                .AsNoTracking()
                .Include(p => p.UserPromotions)
                .Where(p => p.IsSkyMilesExclusive)
                .OrderBy(p => p.SkyMilesCost)
                .ThenByDescending(p => p.DiscountPercent)
                .Select(p => new AdminSkyMilesPromotionRowViewModel
                {
                    PromoId = p.PromoId,
                    PromoCode = p.PromoCode ?? string.Empty,
                    Title = p.Title ?? p.PromoCode ?? string.Empty,
                    Description = p.Description ?? string.Empty,
                    DiscountPercent = p.DiscountPercent ?? 0,
                    SkyMilesCost = p.SkyMilesCost,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    TotalPurchased = p.UserPromotions.Count,
                    TotalAvailable = p.UserPromotions.Count(up => !up.IsRedeemed),
                    TotalRedeemed = p.UserPromotions.Count(up => up.IsRedeemed)
                })
                .ToListAsync();

            if (form != null)
            {
                return new ManageSkyMilesViewModel
                {
                    Form = form,
                    Promotions = promotions
                };
            }

            if (!editId.HasValue)
            {
                return new ManageSkyMilesViewModel
                {
                    Promotions = promotions
                };
            }

            var promotion = await _context.Promotions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PromoId == editId.Value && p.IsSkyMilesExclusive);

            if (promotion == null)
            {
                return new ManageSkyMilesViewModel
                {
                    Promotions = promotions
                };
            }

            return new ManageSkyMilesViewModel
            {
                Promotions = promotions,
                Form = new SkyMilesPromotionFormModel
                {
                    PromoId = promotion.PromoId,
                    Title = promotion.Title ?? promotion.PromoCode ?? string.Empty,
                    Description = promotion.Description,
                    PromoCode = promotion.PromoCode ?? string.Empty,
                    DiscountPercent = promotion.DiscountPercent ?? 0,
                    SkyMilesCost = promotion.SkyMilesCost,
                    StartDate = promotion.StartDate ?? DateOnly.FromDateTime(DateTime.Today),
                    EndDate = promotion.EndDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(30))
                }
            };
        }

        private async Task<string?> ValidateFormAsync(SkyMilesPromotionFormModel form)
        {
            if (form.EndDate < form.StartDate)
            {
                return "End date must be later than or equal to start date.";
            }

            var normalizedCode = PromotionService.NormalizePromoCode(form.PromoCode);
            var duplicateCode = await _context.Promotions.AnyAsync(p =>
                p.PromoId != form.PromoId &&
                p.PromoCode == normalizedCode);

            if (duplicateCode)
            {
                return "Promotion code already exists.";
            }

            return null;
        }
    }
}
