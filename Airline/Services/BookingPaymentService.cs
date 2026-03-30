using System.Data;
using Airline.Models;
using Microsoft.EntityFrameworkCore;

namespace Airline.Services
{
    public sealed class BookingPaymentService
    {
        private const decimal SkyMilesSpendPerPoint = 1000m;
        private readonly DataContext _context;
        private readonly PromotionService _promotionService;

        public BookingPaymentService(DataContext context, PromotionService promotionService)
        {
            _context = context;
            _promotionService = promotionService;
        }

        public async Task<BookingPaymentResult> FinalizeSuccessfulBookingPaymentAsync(
            int bookingId,
            string paymentMethod,
            string transactionNo)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Tickets)
                    .Include(b => b.BookingPromotions)
                        .ThenInclude(bp => bp.Promo)
                    .Include(b => b.User)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                if (booking == null)
                {
                    await transaction.RollbackAsync();
                    return BookingPaymentResult.Failed;
                }

                if (string.Equals(booking.Status, "PAID", StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.RollbackAsync();
                    return BookingPaymentResult.Handled(booking.User, skyMilesAwarded: 0, wasUpdated: false);
                }

                var pricing = await _promotionService.CalculateBookingAsync(booking);
                booking.Status = "PAID";

                foreach (var ticket in booking.Tickets)
                {
                    ticket.Status = "PAID";
                }

                var awardedSkyMiles = CalculateSkyMiles(pricing.FinalAmount);
                if (awardedSkyMiles > 0)
                {
                    booking.User.SkyMiles = (booking.User.SkyMiles ?? 0) + awardedSkyMiles;
                }

                _context.Payments.Add(new Payment
                {
                    BookingId = booking.BookingId,
                    Amount = pricing.FinalAmount,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = paymentMethod,
                    PaymentStatus = "SUCCESS",
                    TransactionNo = transactionNo
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return BookingPaymentResult.Handled(booking.User, awardedSkyMiles, wasUpdated: true);
            }
            catch
            {
                await transaction.RollbackAsync();
                return BookingPaymentResult.Failed;
            }
        }

        private static int CalculateSkyMiles(decimal paidAmount)
        {
            if (paidAmount <= 0)
            {
                return 0;
            }

            return (int)Math.Floor(paidAmount / SkyMilesSpendPerPoint);
        }
    }

    public sealed class BookingPaymentResult
    {
        public static BookingPaymentResult Failed { get; } = new();

        public bool IsHandled { get; init; }
        public bool WasUpdated { get; init; }
        public int SkyMilesAwarded { get; init; }
        public int CurrentSkyMiles { get; init; }
        public int? UserId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;

        public static BookingPaymentResult Handled(User? user, int skyMilesAwarded, bool wasUpdated)
        {
            return new BookingPaymentResult
            {
                IsHandled = true,
                WasUpdated = wasUpdated,
                SkyMilesAwarded = skyMilesAwarded,
                CurrentSkyMiles = user?.SkyMiles ?? 0,
                UserId = user?.UserId,
                Username = user?.Username ?? string.Empty,
                FirstName = user?.FirstName ?? string.Empty,
                LastName = user?.LastName ?? string.Empty,
                Role = user?.Role ?? string.Empty
            };
        }
    }
}
