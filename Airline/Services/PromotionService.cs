using Airline.Models;
using Microsoft.EntityFrameworkCore;

namespace Airline.Services
{
    public class PromotionService
    {
        private const decimal TaxRate = 0.10m;
        private readonly DataContext _context;

        public PromotionService(DataContext context)
        {
            _context = context;
        }

        public async Task<List<Promotion>> GetActivePromotionsAsync(DateOnly? onDate = null)
        {
            var targetDate = onDate ?? DateOnly.FromDateTime(DateTime.Now);

            return await _context.Promotions
                .AsNoTracking()
                .Where(p =>
                    (!p.StartDate.HasValue || p.StartDate.Value <= targetDate) &&
                    (!p.EndDate.HasValue || p.EndDate.Value >= targetDate))
                .OrderByDescending(p => p.DiscountPercent ?? 0)
                .ThenBy(p => p.EndDate)
                .ToListAsync();
        }

        public async Task<Promotion?> GetPromotionByCodeAsync(string? promoCode, DateOnly? onDate = null)
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
                    (!p.StartDate.HasValue || p.StartDate.Value <= targetDate) &&
                    (!p.EndDate.HasValue || p.EndDate.Value >= targetDate));
        }

        public async Task<PromotionPricingResult> CalculateSingleTicketAsync(
            int scheduleId,
            int classId,
            string? promoCode = null,
            DateOnly? onDate = null)
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

            return await BuildPricingAsync(baseFare, promoCode, onDate);
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

            var promoCode = booking.BookingPromotions
                .OrderBy(bp => bp.Id)
                .Select(bp => bp.Promo?.PromoCode)
                .FirstOrDefault();

            return await BuildPricingAsync(baseFare, promoCode, onDate);
        }

        public static string NormalizePromoCode(string promoCode)
        {
            return promoCode.Trim().ToUpperInvariant();
        }

        private async Task<PromotionPricingResult> BuildPricingAsync(
            decimal baseFare,
            string? promoCode,
            DateOnly? onDate)
        {
            var promotion = await GetPromotionByCodeAsync(promoCode, onDate);
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
