using Airline.Models;
using Airline.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Airline.Controllers
{
    public class MilesController : Controller
    {
        private readonly DataContext _context;
        private readonly PromotionService _promotionService;

        public MilesController(DataContext context, PromotionService promotionService)
        {
            _context = context;
            _promotionService = promotionService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = TryGetUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            var viewModel = await BuildViewModelAsync(userId.Value);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PurchasePromotion(int promoId)
        {
            var userId = TryGetUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.PromoId == promoId && p.IsSkyMilesExclusive);

            if (user == null || promotion == null)
            {
                TempData["ErrorMessage"] = "SkyMiles reward code could not be found.";
                return RedirectToAction(nameof(Index));
            }

            var today = DateOnly.FromDateTime(DateTime.Now);
            var isActive =
                (!promotion.StartDate.HasValue || promotion.StartDate.Value <= today) &&
                (!promotion.EndDate.HasValue || promotion.EndDate.Value >= today);

            if (!isActive)
            {
                TempData["ErrorMessage"] = "This SkyMiles reward code is not currently available.";
                return RedirectToAction(nameof(Index));
            }

            var alreadyOwned = await _context.UserPromotions.AnyAsync(up =>
                up.UserId == user.UserId &&
                up.PromoId == promoId &&
                !up.IsRedeemed);

            if (alreadyOwned)
            {
                TempData["ErrorMessage"] = "You already own an unused copy of this reward code.";
                return RedirectToAction(nameof(Index));
            }

            var currentMiles = user.SkyMiles ?? 0;
            if (currentMiles < promotion.SkyMilesCost)
            {
                TempData["ErrorMessage"] = $"You need {promotion.SkyMilesCost:N0} SkyMiles to buy this code.";
                return RedirectToAction(nameof(Index));
            }

            user.SkyMiles = currentMiles - promotion.SkyMilesCost;

            _context.UserPromotions.Add(new UserPromotion
            {
                UserId = user.UserId,
                PromoId = promotion.PromoId,
                PurchasedAt = DateTime.Now,
                SkyMilesSpent = promotion.SkyMilesCost,
                IsRedeemed = false
            });

            await _context.SaveChangesAsync();
            await RefreshAuthenticatedUserClaimsAsync(user);

            TempData["SuccessMessage"] = $"You purchased code {promotion.PromoCode} for {promotion.SkyMilesCost:N0} SkyMiles.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<SkyMilesShopViewModel> BuildViewModelAsync(int userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstAsync(u => u.UserId == userId);

            var shopPromotions = await _promotionService.GetSkyMilesShopPromotionsAsync();
            var userPromotions = await _context.UserPromotions
                .AsNoTracking()
                .Include(up => up.Promo)
                .Where(up => up.UserId == userId)
                .OrderByDescending(up => up.PurchasedAt)
                .ToListAsync();

            var activeOwnedPromoIds = userPromotions
                .Where(up =>
                    !up.IsRedeemed &&
                    (!up.Promo.EndDate.HasValue || up.Promo.EndDate.Value >= DateOnly.FromDateTime(DateTime.Now)))
                .Select(up => up.PromoId)
                .ToHashSet();

            return new SkyMilesShopViewModel
            {
                CurrentSkyMiles = user.SkyMiles ?? 0,
                ShopItems = shopPromotions.Select(p => new SkyMilesShopItemViewModel
                {
                    PromoId = p.PromoId,
                    PromoCode = p.PromoCode ?? string.Empty,
                    Title = p.Title ?? p.PromoCode ?? string.Empty,
                    Description = p.Description ?? string.Empty,
                    DiscountPercent = p.DiscountPercent ?? 0,
                    SkyMilesCost = p.SkyMilesCost,
                    ValidityText = BuildValidityText(p.StartDate, p.EndDate),
                    AlreadyOwned = activeOwnedPromoIds.Contains(p.PromoId),
                    CanAfford = (user.SkyMiles ?? 0) >= p.SkyMilesCost
                }).ToList(),
                OwnedPromotions = userPromotions.Select(up =>
                {
                    var isExpired = up.Promo.EndDate.HasValue && up.Promo.EndDate.Value < DateOnly.FromDateTime(DateTime.Now);
                    return new OwnedSkyMilesPromotionViewModel
                    {
                        UserPromotionId = up.UserPromotionId,
                        PromoCode = up.Promo.PromoCode ?? string.Empty,
                        Title = up.Promo.Title ?? up.Promo.PromoCode ?? string.Empty,
                        Description = up.Promo.Description ?? string.Empty,
                        DiscountPercent = up.Promo.DiscountPercent ?? 0,
                        SkyMilesSpent = up.SkyMilesSpent,
                        PurchasedAt = up.PurchasedAt,
                        EndDate = up.Promo.EndDate,
                        IsRedeemed = up.IsRedeemed,
                        RedeemedAt = up.RedeemedAt,
                        IsExpired = isExpired
                    };
                }).ToList()
            };
        }

        private int? TryGetUserId()
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

        private static string BuildValidityText(DateOnly? startDate, DateOnly? endDate)
        {
            if (startDate.HasValue && endDate.HasValue)
            {
                return $"{startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}";
            }

            if (endDate.HasValue)
            {
                return $"Valid until {endDate:dd/MM/yyyy}";
            }

            if (startDate.HasValue)
            {
                return $"Valid from {startDate:dd/MM/yyyy}";
            }

            return "Available now";
        }
    }
}
