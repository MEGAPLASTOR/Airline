using System.Data;
using Airline.Models;
using Microsoft.EntityFrameworkCore;

namespace Airline.Services
{
    public sealed class BookingPaymentService
    {
        private const string BaggagePaymentMethod = "VNPAY_BAGGAGE";
        private const string SkyMilesRedemptionPaymentMethod = "SKYMILES_REDEEM";
        private const string SkyMilesAwardedMarker = "|SKYMILES_AWARDED";
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
                    .Include(b => b.Payments)
                    .Include(b => b.User)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                if (booking == null)
                {
                    await transaction.RollbackAsync();
                    return BookingPaymentResult.Failed;
                }

                var pricing = await _promotionService.CalculateBookingAsync(booking);
                var wasUpdated = false;

                if (!string.Equals(booking.Status, "PAID", StringComparison.OrdinalIgnoreCase))
                {
                    booking.Status = "PAID";
                    foreach (var ticket in booking.Tickets)
                    {
                        ticket.Status = "PAID";
                    }

                    wasUpdated = true;
                }

                var successfulBookingPayments = booking.Payments
                    .Where(IsSuccessfulMainBookingPayment)
                    .OrderByDescending(p => p.PaymentDate ?? DateTime.MinValue)
                    .ThenByDescending(p => p.PaymentId)
                    .ToList();

                var alreadyAwardedSkyMiles =
                    successfulBookingPayments.Any(HasSkyMilesAwardMarker) ||
                    successfulBookingPayments.Any(IsSkyMilesRedemptionPayment);
                var awardedSkyMiles = 0;

                if (!alreadyAwardedSkyMiles)
                {
                    awardedSkyMiles = CalculateSkyMiles(pricing.FinalAmount);
                    if (awardedSkyMiles > 0)
                    {
                        booking.User.SkyMiles = (booking.User.SkyMiles ?? 0) + awardedSkyMiles;
                    }

                    var paymentToMark = successfulBookingPayments.FirstOrDefault();
                    if (paymentToMark == null)
                    {
                        _context.Payments.Add(new Payment
                        {
                            BookingId = booking.BookingId,
                            Amount = pricing.FinalAmount,
                            PaymentDate = DateTime.Now,
                            PaymentMethod = MarkSkyMilesAwarded(paymentMethod),
                            PaymentStatus = "SUCCESS",
                            TransactionNo = transactionNo
                        });
                    }
                    else
                    {
                        paymentToMark.PaymentMethod = MarkSkyMilesAwarded(paymentToMark.PaymentMethod ?? paymentMethod);
                        paymentToMark.PaymentStatus = "SUCCESS";
                        paymentToMark.Amount ??= pricing.FinalAmount;
                        paymentToMark.PaymentDate ??= DateTime.Now;

                        if (string.IsNullOrWhiteSpace(paymentToMark.TransactionNo))
                        {
                            paymentToMark.TransactionNo = transactionNo;
                        }
                    }

                    wasUpdated = true;
                }

                if (!wasUpdated)
                {
                    await transaction.RollbackAsync();
                    return BookingPaymentResult.Handled(booking.User, awardedSkyMiles, wasUpdated: false);
                }

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

        public async Task<BookingPaymentResult> ReconcileSkyMilesAsync(int userId)
        {
            var bookingIds = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.UserId == userId && b.Status == "PAID")
                .OrderBy(b => b.BookingId)
                .Select(b => b.BookingId)
                .ToListAsync();

            BookingPaymentResult? latestResult = null;
            var anyHandled = false;
            var anyUpdated = false;
            var totalAwardedSkyMiles = 0;

            foreach (var bookingId in bookingIds)
            {
                var result = await FinalizeSuccessfulBookingPaymentAsync(
                    bookingId,
                    "SKYMILES_RECONCILE",
                    $"RECONCILE_{bookingId}");

                if (!result.IsHandled)
                {
                    continue;
                }

                latestResult = result;
                anyHandled = true;
                anyUpdated |= result.WasUpdated;
                totalAwardedSkyMiles += result.SkyMilesAwarded;
            }

            if (!anyHandled || latestResult == null)
            {
                return BookingPaymentResult.Failed;
            }

            return new BookingPaymentResult
            {
                IsHandled = true,
                WasUpdated = anyUpdated,
                SkyMilesAwarded = totalAwardedSkyMiles,
                CurrentSkyMiles = latestResult.CurrentSkyMiles,
                UserId = latestResult.UserId,
                Username = latestResult.Username,
                FirstName = latestResult.FirstName,
                LastName = latestResult.LastName,
                Role = latestResult.Role
            };
        }

        private static bool IsSuccessfulMainBookingPayment(Payment payment)
        {
            if (payment == null)
            {
                return false;
            }

            var isSuccessful =
                string.Equals(payment.PaymentStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(payment.PaymentStatus, "PAID", StringComparison.OrdinalIgnoreCase);

            var isBaggagePayment = string.Equals(payment.PaymentMethod, BaggagePaymentMethod, StringComparison.OrdinalIgnoreCase);
            return isSuccessful && !isBaggagePayment;
        }

        private static bool HasSkyMilesAwardMarker(Payment payment)
        {
            return payment.PaymentMethod?.Contains(SkyMilesAwardedMarker, StringComparison.OrdinalIgnoreCase) == true;
        }

        private static bool IsSkyMilesRedemptionPayment(Payment payment)
        {
            return payment.PaymentMethod?.Contains(SkyMilesRedemptionPaymentMethod, StringComparison.OrdinalIgnoreCase) == true;
        }

        private static string MarkSkyMilesAwarded(string paymentMethod)
        {
            if (paymentMethod.Contains(SkyMilesAwardedMarker, StringComparison.OrdinalIgnoreCase))
            {
                return paymentMethod;
            }

            return paymentMethod + SkyMilesAwardedMarker;
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
