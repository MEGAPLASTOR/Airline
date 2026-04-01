using Airline.Models;
using Microsoft.EntityFrameworkCore;

namespace Airline.Services
{
    public class PromotionService
    {
        private const decimal TaxRate = 0.10m;
        public const decimal SkyMilesValuePerPoint = 1000m;
        private readonly DataContext _context;

        public PromotionService(DataContext context)
        {
            _context = context;
        }

        public async Task<List<Promotion>> GetPublicPromotionsAsync(DateOnly? onDate = null)
        {
            var targetDate = onDate ?? DateOnly.FromDateTime(DateTime.Now);

            return await _context.Promotions
                .AsNoTracking()
                .Where(p =>
                    !p.IsSkyMilesExclusive &&
                    (!p.StartDate.HasValue || p.StartDate.Value <= targetDate) &&
                    (!p.EndDate.HasValue || p.EndDate.Value >= targetDate))
                .OrderByDescending(p => p.DiscountPercent ?? 0)
                .ThenBy(p => p.EndDate)
                .ToListAsync();
        }

        public async Task<List<Promotion>> GetSkyMilesShopPromotionsAsync(DateOnly? onDate = null)
        {
            var targetDate = onDate ?? DateOnly.FromDateTime(DateTime.Now);

            return await _context.Promotions
                .AsNoTracking()
                .Where(p =>
                    p.IsSkyMilesExclusive &&
                    p.OnlyForSkyMilesPayment &&
                    p.SkyMilesCost > 0 &&
                    (!p.StartDate.HasValue || p.StartDate.Value <= targetDate) &&
                    (!p.EndDate.HasValue || p.EndDate.Value >= targetDate))
                .OrderBy(p => p.SkyMilesCost)
                .ThenByDescending(p => p.DiscountPercent ?? 0)
                .ToListAsync();
        }

        public async Task<List<Promotion>> GetOwnedSkyMilesPromotionsAsync(int userId, DateOnly? onDate = null)
        {
            var targetDate = onDate ?? DateOnly.FromDateTime(DateTime.Now);

            return await _context.UserPromotions
                .AsNoTracking()
                .Where(up =>
                    up.UserId == userId &&
                    !up.IsRedeemed &&
                    up.Promo.IsSkyMilesExclusive &&
                    up.Promo.OnlyForSkyMilesPayment &&
                    (!up.Promo.StartDate.HasValue || up.Promo.StartDate.Value <= targetDate) &&
                    (!up.Promo.EndDate.HasValue || up.Promo.EndDate.Value >= targetDate))
                .OrderByDescending(up => up.Promo.DiscountPercent ?? 0)
                .ThenBy(up => up.Promo.EndDate)
                .Select(up => up.Promo)
                .ToListAsync();
        }

        public async Task<Promotion?> GetPromotionByCodeAsync(
            string? promoCode,
            DateOnly? onDate = null,
            bool includeSkyMilesExclusive = true)
        {
            if (string.IsNullOrWhiteSpace(promoCode))
            {
                return null;
            }

            var normalizedCode = NormalizePromoCode(promoCode);
            var targetDate = onDate ?? DateOnly.FromDateTime(DateTime.Now);

            return await _context.Promotions
                .FirstOrDefaultAsync(p =>
                    p.PromoCode == normalizedCode &&
                    (includeSkyMilesExclusive || !p.IsSkyMilesExclusive) &&
                    (!p.StartDate.HasValue || p.StartDate.Value <= targetDate) &&
                    (!p.EndDate.HasValue || p.EndDate.Value >= targetDate));
        }

        public async Task<UserPromotion?> GetOwnedPromotionAsync(int userId, string? promoCode, DateOnly? onDate = null)
        {
            if (string.IsNullOrWhiteSpace(promoCode))
            {
                return null;
            }

            var normalizedCode = NormalizePromoCode(promoCode);
            var targetDate = onDate ?? DateOnly.FromDateTime(DateTime.Now);

            return await _context.UserPromotions
                .Include(up => up.Promo)
                .FirstOrDefaultAsync(up =>
                    up.UserId == userId &&
                    !up.IsRedeemed &&
                    up.Promo.PromoCode == normalizedCode &&
                    up.Promo.IsSkyMilesExclusive &&
                    up.Promo.OnlyForSkyMilesPayment &&
                    (!up.Promo.StartDate.HasValue || up.Promo.StartDate.Value <= targetDate) &&
                    (!up.Promo.EndDate.HasValue || up.Promo.EndDate.Value >= targetDate));
        }

        public async Task<Promotion?> ResolvePromotionAsync(
            string? promoCode,
            int? userId = null,
            bool useSkyMilesPayment = false,
            DateOnly? onDate = null)
        {
            if (string.IsNullOrWhiteSpace(promoCode))
            {
                return null;
            }

            if (!useSkyMilesPayment)
            {
                return await GetPromotionByCodeAsync(promoCode, onDate, includeSkyMilesExclusive: false);
            }

            if (!userId.HasValue)
            {
                return null;
            }

            return (await GetOwnedPromotionAsync(userId.Value, promoCode, onDate))?.Promo;
        }

        public async Task<PromotionPricingResult> CalculateSingleTicketAsync(
            int scheduleId,
            int classId,
            string? promoCode = null,
            DateOnly? onDate = null,
            int? userId = null,
            bool useSkyMilesPayment = false)
        {
            var baseFare = await _context.TicketPrices
                .AsNoTracking()
                .Where(p => p.ScheduleId == scheduleId && p.ClassId == classId)
                .Select(p => p.Price ?? 1500000m)
                .FirstOrDefaultAsync();

            if (baseFare <= 0)
            {
                baseFare = 1500000m;
            }

            return await BuildPricingAsync(baseFare, promoCode, onDate, userId, useSkyMilesPayment);
        }

        public async Task<PromotionPricingResult> CalculateBookingAsync(Booking booking, DateOnly? onDate = null)
        {
            if (booking == null)
            {
                throw new ArgumentNullException(nameof(booking));
            }

            decimal baseFare = 0m;

            foreach (var ticket in booking.Tickets)
            {
                var priceEntry = await _context.TicketPrices
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ScheduleId == booking.ScheduleId && p.ClassId == ticket.ClassId);

                baseFare += priceEntry?.Price ?? 1500000m;
            }

            var promotion = booking.BookingPromotions
                .OrderBy(bp => bp.Id)
                .Select(bp => bp.Promo)
                .FirstOrDefault();

            if (promotion == null)
            {
                var promoId = booking.BookingPromotions
                    .OrderBy(bp => bp.Id)
                    .Select(bp => (int?)bp.PromoId)
                    .FirstOrDefault();

                if (promoId.HasValue)
                {
                    promotion = await _context.Promotions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.PromoId == promoId.Value);
                }
            }

            return BuildPricing(baseFare, promotion);
        }

        public static string NormalizePromoCode(string promoCode)
        {
            return promoCode.Trim().ToUpperInvariant();
        }

        public static int CalculateRequiredSkyMiles(decimal finalAmount)
        {
            if (finalAmount <= 0)
            {
                return 0;
            }

            return (int)Math.Ceiling(finalAmount / SkyMilesValuePerPoint);
        }

        private async Task<PromotionPricingResult> BuildPricingAsync(
            decimal baseFare,
            string? promoCode,
            DateOnly? onDate,
            int? userId,
            bool useSkyMilesPayment)
        {
            var promotion = await ResolvePromotionAsync(promoCode, userId, useSkyMilesPayment, onDate);
            return BuildPricing(baseFare, promotion);
        }

        private static PromotionPricingResult BuildPricing(decimal baseFare, Promotion? promotion)
        {
            var discountPercent = promotion?.DiscountPercent ?? 0;
            var taxAmount = RoundVnd(baseFare * TaxRate);
            var discountAmount = RoundVnd(baseFare * discountPercent / 100m);
            var finalAmount = Math.Max(0m, RoundVnd(baseFare + taxAmount - discountAmount));

            return new PromotionPricingResult
            {
                BaseFare = RoundVnd(baseFare),
                TaxAmount = taxAmount,
                DiscountPercent = discountPercent,
                DiscountAmount = discountAmount,
                FinalAmount = finalAmount,
                RequiredSkyMiles = CalculateRequiredSkyMiles(finalAmount),
                AppliedPromotionId = promotion?.PromoId,
                AppliedPromotionCode = promotion?.PromoCode
            };
        }

        private static decimal RoundVnd(decimal amount)
        {
            return Math.Round(amount, 0, MidpointRounding.AwayFromZero);
        }
    }
}
