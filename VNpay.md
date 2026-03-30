# Hướng dẫn Cấu hình và Tích hợp Tích hợp VNPay

Tài liệu này cung cấp hướng dẫn từng bước để thiết lập, cấu hình và sử dụng cổng thanh toán VNPay trong hệ thống Airline Reservation.

## 1. Đăng ký & Nhận thông tin cấu hình Sandbox

Trong môi trường phát triển (Development), hệ thống sử dụng môi trường Sandbox của VNPay. 
Để quản lý các giao dịch test của riêng bạn, hãy đăng ký tài khoản tại [VNPay Sandbox](https://sandbox.vnpayment.vn/devreg/).

Sau khi đăng ký, bạn sẽ nhận được 2 thông tin quan trọng qua email:
- `TmnCode`: Mã định danh Website trên hệ thống VNPay (Ví dụ: `TQY3GNN3`)
- `HashSecret`: Chuỗi bí mật dùng để tạo chữ kýChecksum (Ví dụ: `J8WCOFSMU9V3G7Z1Y5I5Z5I5Z5I5Z5I5`)

## 2. Cấu hình appsettings.json

Mở tệp `appsettings.json` trong thư mục gốc của project (cột ngang hàng với `Program.cs`) và thêm block cấu hình sau:

```json
  "Vnpay": {
    "TmnCode": "NHAP_TMNCODE_CUA_BAN", 
    "HashSecret": "NHAP_HASHSECRET_CUA_BAN",
    "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
    "ReturnUrl": "https://localhost:7263/Payment/PaymentCallback"
  }
```

> [!IMPORTANT]
> - Cổng `7263` trong `ReturnUrl` phải khớp chính xác với cổng (Port) ứng dụng ASP.NET Core của bạn đang chạy ở trình duyệt (`launchSettings.json`). Nếu bạn dùng IIS Express hoặc Kestrel ở cổng khác (ví dụ: `5045`), bạn phải cập nhật lại liên kết này, nếu không sau khi thanh toán xong VNPay sẽ không thể chuyển hướng bạn về lại trang Kết quả.

## 3. Kiến trúc Thư viện và Services

Hệ thống sử dụng Class phụ trợ `VnPayLibrary.cs` (nằm trong thư mục `Services/VnPayLibrary.cs` hoặc file đính kèm). Thư viện này có hai tác dụng chính:
- **Tạo URL (`CreateRequestUrl`)**: Nối các dữ liệu đơn hàng (Mã ĐH, Số tiền, Ngày) chung lại và băm cùng `HashSecret` để tạo ra chuỗi Signature đẩy sang VNPay.
- **Xác thực (`ValidateSignature`)**: Kiểm tra chuỗi trả về từ VNPay để phòng ngừa việc hacker cố tình truyền tham số ảo giả mạo kết quả thanh toán. 

Dữ liệu được xử lý tập trung tại `PaymentController.cs` với 3 Action cốt lõi:
1. `CreatePayment`: Gọi khi người dùng ấn thanh toán. Gom vé, tính tiền và redirect người dùng qua VNPay.
2. `PaymentCallback`: Trang hứng người dùng sau khi thanh toán xong. 
3. `PaymentIPN`: Giao thức Server-to-Server ngầm (Backend tới Backend) để cập nhật trạng thái chắc chắn 100%.

## 4. Xử lý Môi trường Test (Localhost IPN Issue)

> [!WARNING]
> Cổng IPN (`/Payment/PaymentIPN`) là một cổng nhận dữ liệu hoàn toàn tự động trực tiếp từ hệ thống Server máy chủ của nền tảng VNPay. 
> Vì thế máy chủ VNPay trên Internet sẽ **KHÔNG THỂ** gọi tới `https://localhost` của bạn.

Để test hoàn chỉnh luồng cập nhật DB qua IPN trên máy trạm cá nhân, bạn cần tải về **Ngrok**:
1. Cài đặt Ngrok.
2. Tại Terminal gõ: `ngrok http https://localhost:7263`
3. Lấy URL Public do Ngrok cung cấp (ví dụ: `https://a1b2c3d4.ngrok.app`).
4. Truy cập cấu hình Sandbox trên giao diện VNPay Đăng nhập, trỏ domain URL Webhook/IPN về địa chỉ Ngrok này.

## 5. Dữ liệu Thẻ Test (Dành cho Cổng Sandbox)

Khi cổng chuyển hướng tới VNPay để yêu cầu nhập thẻ ngân hàng, vui lòng sử dụng duy nhất Thẻ Nội Địa (ATM) có thông tin cấp sẵn trên hệ thống Sandbox của VNPay như sau:

- **Ngân hàng**: NCB
- **Số thẻ**: `9704198526191432198`
- **Tên chủ thẻ**: `NGUYEN VAN A`
- **Ngày phát hành**: `07/15`
- **Mật khẩu OTP**: `123456`

Thanh toán bằng thẻ này sẽ luôn thành công giả lập để bạn test luồng (Happy Case). Để test các luồng thất bại, hủy giao dịch hoặc sai số dư, bạn có thể nhập sai OTP cố ý trên màn hình.
