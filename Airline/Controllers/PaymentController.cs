using Airline.Models;
using Airline.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Airline.Controllers
{
    public class PaymentController : Controller
    {
        private const string BookingTransactionPrefix = "BOOK_";
        private const string BaggageTransactionPrefix = "BAG_";
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;
        private readonly BookingPaymentService _bookingPaymentService;
        private readonly PromotionService _promotionService;

        public PaymentController(
            DataContext context,
            IConfiguration configuration,
            PromotionService promotionService,
            BookingPaymentService bookingPaymentService)
        {
            _context = context;
            _configuration = configuration;
            _promotionService = promotionService;
            _bookingPaymentService = bookingPaymentService;
        }

        public async Task<IActionResult> CreatePayment(int id)
        {
            if (User?.Identity?.IsAuthenticated != true)
                return Redirect("/");

            var username = User.Identity?.Name;
            var booking = await _context.Bookings
                .Include(b => b.Schedule)
                .Include(b => b.Tickets)
                .Include(b => b.BookingPromotions)
                    .ThenInclude(bp => bp.Promo)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.User.Username == username);

            if (booking == null) return NotFound();
            if (booking.Status == "PAID") return RedirectToAction("ViewConfirmation", "Ticket");

            var pricing = await _promotionService.CalculateBookingAsync(booking);
            return Redirect(CreatePaymentUrl(
                pricing.FinalAmount,
                $"Airfare payment for booking #{booking.BookingId}",
                $"{BookingTransactionPrefix}{booking.BookingId}_{DateTime.Now.Ticks}"));
        }

        public async Task<IActionResult> CreateBaggagePayment(int id)
        {
            if (User?.Identity?.IsAuthenticated != true)
                return Redirect("/");

            var username = User.Identity?.Name;
            var baggage = await _context.Baggages
                .Include(b => b.Ticket)
                    .ThenInclude(t => t.Booking)
                        .ThenInclude(bk => bk.User)
                .FirstOrDefaultAsync(b =>
                    b.BaggageId == id &&
                    b.Ticket.Booking.User.Username == username);

            if (baggage == null) return NotFound();

            if (await IsBaggagePaymentSuccessfulAsync(baggage.BaggageId, baggage.Ticket.BookingId))
            {
                TempData["SuccessMessage"] = $"Baggage #{baggage.BaggageId} has already been paid.";
                return RedirectToAction("Register", "Baggage");
            }

            var amount = baggage.Price ?? 0m;
            if (amount <= 0)
            {
                TempData["ErrorMessage"] = "The baggage fee is invalid for payment.";
                return RedirectToAction("Register", "Baggage");
            }

            return Redirect(CreatePaymentUrl(
                amount,
                $"Baggage payment for ticket #{baggage.TicketId}",
                $"{BaggageTransactionPrefix}{baggage.BaggageId}_{DateTime.Now.Ticks}"));
        }

        public async Task<IActionResult> PaymentCallback()
        {
            var vnpay = BuildResponseLibrary();
            var config = _configuration.GetSection("Vnpay");
            var secureHash = Request.Query["vnp_SecureHash"];
            var isValidSignature = vnpay.ValidateSignature(secureHash!, config["HashSecret"]!);

            ViewBag.ReturnUrl = Url.Action("ViewConfirmation", "Ticket");
            ViewBag.ReturnText = "Go to My Tickets";

            if (isValidSignature)
            {
                var responseCode = vnpay.GetResponseData("vnp_ResponseCode");
                var txnRef = vnpay.GetResponseData("vnp_TxnRef");
                var transactionNo = vnpay.GetResponseData("vnp_TransactionNo");

                if (TryParseBaggageTxnRef(txnRef, out var baggageId))
                {
                    ViewBag.ReturnUrl = Url.Action("Register", "Baggage");
                    ViewBag.ReturnText = "Back to Baggage";

                    if (responseCode == "00")
                    {
                        await UpdateBaggagePaymentStatus(baggageId, transactionNo);
                        ViewBag.Message = "Baggage payment was successful.";
                        ViewBag.Success = true;
                    }
                    else
                    {
                        ViewBag.Message = "Baggage payment failed. Error code: " + responseCode;
                        ViewBag.Success = false;
                    }
                }
                else
                {
                    var bookingId = ParseBookingId(txnRef);
                    if (responseCode == "00")
                    {
                        var paymentResult = await _bookingPaymentService.FinalizeSuccessfulBookingPaymentAsync(
                            bookingId,
                            "VNPAY",
                            transactionNo);

                        if (paymentResult.IsHandled)
                        {
                            await RefreshAuthenticatedUserClaimsAsync(paymentResult);
                            ViewBag.Message = BuildBookingPaymentSuccessMessage(paymentResult);
                            ViewBag.Success = true;
                        }
                        else
                        {
                            ViewBag.Message = "Payment was accepted, but we could not update your booking. Please contact support.";
                            ViewBag.Success = false;
                        }
                    }
                    else
                    {
                        ViewBag.Message = "Payment failed. Error code: " + responseCode;
                        ViewBag.Success = false;
                    }
                }
            }
            else
            {
                ViewBag.Message = "Invalid signature. The transaction may have been tampered with.";
                ViewBag.Success = false;
            }

            return View("PaymentResult");
        }

        [HttpGet]
        public async Task<IActionResult> PaymentIPN()
        {
            var vnpay = BuildResponseLibrary();
            var config = _configuration.GetSection("Vnpay");
            var secureHash = Request.Query["vnp_SecureHash"];
            var isValidSignature = vnpay.ValidateSignature(secureHash!, config["HashSecret"]!);

            if (isValidSignature)
            {
                var responseCode = vnpay.GetResponseData("vnp_ResponseCode");
                var txnRef = vnpay.GetResponseData("vnp_TxnRef");
                var transactionNo = vnpay.GetResponseData("vnp_TransactionNo");

                if (responseCode == "00")
                {
                    if (TryParseBaggageTxnRef(txnRef, out var baggageId))
                    {
                        await UpdateBaggagePaymentStatus(baggageId, transactionNo);
                    }
                    else
                    {
                        var paymentResult = await _bookingPaymentService.FinalizeSuccessfulBookingPaymentAsync(
                            ParseBookingId(txnRef),
                            "VNPAY",
                            transactionNo);

                        if (!paymentResult.IsHandled)
                        {
                            return Json(new { RspCode = "01", Message = "Booking update failed" });
                        }
                    }

                    return Json(new { RspCode = "00", Message = "Confirm Success" });
                }
            }

            return Json(new { RspCode = "97", Message = "Invalid Signature or Failed" });
        }

        private string CreatePaymentUrl(decimal amount, string orderInfo, string transactionRef)
        {
            var vnpay = new VnPayLibrary();
            var config = _configuration.GetSection("Vnpay");

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", config["TmnCode"]!);
            vnpay.AddRequestData("vnp_Amount", ((long)(amount * 100)).ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
            vnpay.AddRequestData("vnp_Locale", "en");
            vnpay.AddRequestData("vnp_OrderInfo", orderInfo);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", config["ReturnUrl"]!);
            vnpay.AddRequestData("vnp_TxnRef", transactionRef);

            return vnpay.CreateRequestUrl(config["BaseUrl"]!, config["HashSecret"]!);
        }

        private VnPayLibrary BuildResponseLibrary()
        {
            var vnpay = new VnPayLibrary();
            foreach (var (key, value) in Request.Query)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value!);
                }
            }

            return vnpay;
        }

        private async Task RefreshAuthenticatedUserClaimsAsync(BookingPaymentResult paymentResult)
        {
            if (User?.Identity?.IsAuthenticated != true || !paymentResult.UserId.HasValue)
            {
                return;
            }

            var authenticatedUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var canRefreshByUserId =
                int.TryParse(authenticatedUserId, out var currentUserId) &&
                currentUserId == paymentResult.UserId.Value;
            var canRefreshByUsername = string.Equals(User.Identity?.Name, paymentResult.Username, StringComparison.OrdinalIgnoreCase);

            if (!canRefreshByUserId && !canRefreshByUsername)
            {
                return;
            }

            var authResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, paymentResult.UserId.Value.ToString()),
                new Claim(ClaimTypes.Name, paymentResult.Username),
                new Claim("FirstName", paymentResult.FirstName),
                new Claim("LastName", paymentResult.LastName),
                new Claim("SkyMiles", paymentResult.CurrentSkyMiles.ToString()),
                new Claim(ClaimTypes.Role, paymentResult.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authResult.Properties ?? new AuthenticationProperties());
        }

        private static string BuildBookingPaymentSuccessMessage(BookingPaymentResult paymentResult)
        {
            if (paymentResult.SkyMilesAwarded > 0)
            {
                return $"Payment was successful. {paymentResult.SkyMilesAwarded:N0} SkyMiles have been added to your account. Current balance: {paymentResult.CurrentSkyMiles:N0} miles.";
            }

            if (!paymentResult.WasUpdated)
            {
                return "Payment was successful. Your booking had already been confirmed earlier.";
            }

            return "Payment was successful.";
        }

        private async Task UpdateBaggagePaymentStatus(int baggageId, string transactionNo)
        {
            var baggage = await _context.Baggages
                .Include(b => b.Ticket)
                .FirstOrDefaultAsync(b => b.BaggageId == baggageId);

            if (baggage == null)
            {
                return;
            }

            if (await IsBaggagePaymentSuccessfulAsync(baggageId, baggage.Ticket.BookingId))
            {
                return;
            }

            var payment = new Payment
            {
                BookingId = baggage.Ticket.BookingId,
                Amount = baggage.Price ?? 0m,
                PaymentDate = DateTime.Now,
                PaymentMethod = "VNPAY_BAGGAGE",
                PaymentStatus = "SUCCESS",
                TransactionNo = $"{BaggageTransactionPrefix}{baggageId}_{transactionNo}"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
        }

        private async Task<bool> IsBaggagePaymentSuccessfulAsync(int baggageId, int bookingId)
        {
            return await _context.Payments
                .AsNoTracking()
                .AnyAsync(p =>
                    p.BookingId == bookingId &&
                    p.TransactionNo != null &&
                    p.TransactionNo.StartsWith($"{BaggageTransactionPrefix}{baggageId}_") &&
                    (p.PaymentStatus == "SUCCESS" || p.PaymentStatus == "PAID"));
        }

        private static bool TryParseBaggageTxnRef(string? txnRef, out int baggageId)
        {
            baggageId = 0;
            if (string.IsNullOrWhiteSpace(txnRef) || !txnRef.StartsWith(BaggageTransactionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var parts = txnRef.Split('_', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 && int.TryParse(parts[1], out baggageId);
        }

        private static int ParseBookingId(string? txnRef)
        {
            if (string.IsNullOrWhiteSpace(txnRef))
            {
                throw new InvalidOperationException("Transaction reference is invalid.");
            }

            var parts = txnRef.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && string.Equals(parts[0], BookingTransactionPrefix.TrimEnd('_'), StringComparison.OrdinalIgnoreCase))
            {
                return int.Parse(parts[1]);
            }

            return int.Parse(parts[0]);
        }
    }
}
