using Airline.Models;
using Airline.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Airline.Controllers
{
    public class PaymentController : Controller
    {
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;
        private readonly PromotionService _promotionService;

        public PaymentController(DataContext context, IConfiguration configuration, PromotionService promotionService)
        {
            _context = context;
            _configuration = configuration;
            _promotionService = promotionService;
        }

        // ══════════════════════════════════════════════════════════════
        // GET /Payment/CreatePayment/{bookingId}
        // ══════════════════════════════════════════════════════════════
        public async Task<IActionResult> CreatePayment(int id)
        {
            if (User?.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Account");

            var username = User.Identity.Name;
            var booking = await _context.Bookings
                .Include(b => b.Schedule)
                .Include(b => b.Tickets)
                .Include(b => b.BookingPromotions)
                    .ThenInclude(bp => bp.Promo)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.User.Username == username);

            if (booking == null) return NotFound();
            if (booking.Status == "PAID") return RedirectToAction("ViewConfirmation", "Ticket");

            // Tính tổng tiền từ bảng TicketPrice
            decimal totalAmount = 0;
            foreach (var ticket in booking.Tickets)
            {
                var priceEntry = await _context.TicketPrices
                    .FirstOrDefaultAsync(p => p.ScheduleId == booking.ScheduleId && p.ClassId == ticket.ClassId);
                
                totalAmount += priceEntry?.Price ?? 1500000; // Fallback if price not found
            }

            // Tích hợp VNPay
            var vnpay = new VnPayLibrary();
            var config = _configuration.GetSection("Vnpay");

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", config["TmnCode"]!);
            vnpay.AddRequestData("vnp_Amount", ((long)((await _promotionService.CalculateBookingAsync(booking)).FinalAmount * 100)).ToString()); // VNPay uses cents (VND * 100)
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan ve may bay cho Booking #{booking.BookingId}");
            vnpay.AddRequestData("vnp_OrderType", "other"); // Default type
            vnpay.AddRequestData("vnp_ReturnUrl", config["ReturnUrl"]!);
            vnpay.AddRequestData("vnp_TxnRef", $"{booking.BookingId}_{DateTime.Now.Ticks}"); // Unique ref

            var paymentUrl = vnpay.CreateRequestUrl(config["BaseUrl"]!, config["HashSecret"]!);

            return Redirect(paymentUrl);
        }

        // ══════════════════════════════════════════════════════════════
        // GET /Payment/PaymentCallback
        // ══════════════════════════════════════════════════════════════
        public async Task<IActionResult> PaymentCallback()
        {
            var vnpay = new VnPayLibrary();
            foreach (var (key, value) in Request.Query)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value!);
                }
            }

            var config = _configuration.GetSection("Vnpay");
            var vnp_SecureHash = Request.Query["vnp_SecureHash"];
            bool isValidSignature = vnpay.ValidateSignature(vnp_SecureHash!, config["HashSecret"]!);

            if (isValidSignature)
            {
                var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                var vnp_TxnRef = vnpay.GetResponseData("vnp_TxnRef");
                var bookingId = int.Parse(vnp_TxnRef.Split('_')[0]);

                if (vnp_ResponseCode == "00")
                {
                    // Thanh toán thành công
                    await UpdateBookingStatus(bookingId, "PAID", vnpay.GetResponseData("vnp_TransactionNo"));
                    ViewBag.Message = "Thanh toán thành công!";
                    ViewBag.Success = true;
                }
                else
                {
                    ViewBag.Message = "Thanh toán không thành công. Mã lỗi: " + vnp_ResponseCode;
                    ViewBag.Success = false;
                }
            }
            else
            {
                ViewBag.Message = "Chữ ký không hợp lệ. Giao dịch có thể đã bị can thiệp.";
                ViewBag.Success = false;
            }

            return View("PaymentResult");
        }

        // ══════════════════════════════════════════════════════════════
        // GET /Payment/PaymentIPN
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> PaymentIPN()
        {
            var vnpay = new VnPayLibrary();
            foreach (var (key, value) in Request.Query)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value!);
                }
            }

            var config = _configuration.GetSection("Vnpay");
            var vnp_SecureHash = Request.Query["vnp_SecureHash"];
            bool isValidSignature = vnpay.ValidateSignature(vnp_SecureHash!, config["HashSecret"]!);

            if (isValidSignature)
            {
                var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                var vnp_TxnRef = vnpay.GetResponseData("vnp_TxnRef");
                var bookingId = int.Parse(vnp_TxnRef.Split('_')[0]);

                if (vnp_ResponseCode == "00")
                {
                    await UpdateBookingStatus(bookingId, "PAID", vnpay.GetResponseData("vnp_TransactionNo"));
                    return Json(new { RspCode = "00", Message = "Confirm Success" });
                }
            }

            return Json(new { RspCode = "97", Message = "Invalid Signature or Failed" });
        }

        private async Task UpdateBookingStatus(int bookingId, string status, string transactionNo)
        {
            var booking = await _context.Bookings
                .Include(b => b.Tickets)
                .Include(b => b.BookingPromotions)
                    .ThenInclude(bp => bp.Promo)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking != null && booking.Status != "PAID")
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        booking.Status = status;
                        decimal totalAmount = 0;
                        foreach (var ticket in booking.Tickets)
                        {
                            ticket.Status = status;
                            var priceEntry = await _context.TicketPrices
                                .FirstOrDefaultAsync(p => p.ScheduleId == booking.ScheduleId && p.ClassId == ticket.ClassId);
                            totalAmount += priceEntry?.Price ?? 1500000;
                        }

                        // Tạo bản ghi Payment
                        var payment = new Payment
                        {
                            BookingId = bookingId,
                            Amount = (await _promotionService.CalculateBookingAsync(booking)).FinalAmount,
                            PaymentDate = DateTime.Now,
                            PaymentMethod = "VNPAY",
                            PaymentStatus = "SUCCESS",
                            TransactionNo = transactionNo
                        };
                        _context.Payments.Add(payment);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                    }
                }
            }
        }
    }
}
