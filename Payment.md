# Hướng dẫn tích hợp và sử dụng VNPay Payment

Tài liệu này cung cấp cái nhìn chi tiết về chức năng thanh toán trực tuyến qua cổng **VNPay** trong hệ thống Airline Reservation.

## 1. Tính năng (Features)

- **Thanh toán đa phương thức**: Hỗ trợ quét mã QR (VNPay-QR), thẻ ATM/Tài khoản nội địa, và các loại thẻ quốc tế (Visa/MasterCard).
- **Mã hóa bảo mật**: Sử dụng thuật toán băm **HMAC-SHA512** theo tiêu chuẩn bảo mật mới nhất của VNPay.
- **Xác thực trạng thái tin cậy (IPN)**: Cơ chế Instant Payment Notification đảm bảo trạng thái vé được cập nhật tự động ngay cả khi người dùng không quay lại website sau khi thanh toán.
- **Tự động hóa luồng nghiệp vụ**: Khi thanh toán thành công, hệ thống tự động:
  - Cập nhật trạng thái `PAID` cho cả `Booking` và `Ticket`.
  - Ghi nhận lịch sử giao dịch vào bảng `Payments`.

## 2. Luồng hoạt động (Workflow)

```mermaid
sequenceDiagram
    participant User as Khách hàng
    participant Web as SkyWave Website
    participant VNPay as Cổng VNPay

    User->>Web: Nhấn "Pay Now" tại danh sách vé
    Web->>Web: Tạo mã GD unique (TxnRef) & Tính tổng tiền
    Web->>VNPay: Redirect tới VNPay URL (kèm vnp_SecureHash)
    User->>VNPay: Thực hiện thanh toán (ATM/QR/Credit)
    VNPay-->>Web: IPN Callback (Update DB ngầm - Server to Server)
    VNPay->>Web: User Redirect (Trả về PaymentCallback)
    Web->>Web: Kiểm tra Checksum & Hiển thị kết quả
    Web->>User: Thông báo Thành công/Thất bại
```

## 3. Cách cấu hình (Configuration)

### Cấu hình trong `appsettings.json`

Các thông số Sandbox hiện tại đã được thiết lập sẵn trong tệp `appsettings.json`:

```json
"Vnpay": {
    "TmnCode": "TQY3GNN3",
    "HashSecret": "J8WCOFSMU9V3G7Z1Y5I5Z5I5Z5I5Z5I5",
    "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
    "ReturnUrl": "https://localhost:7129/Payment/PaymentCallback"
}
```

- **TmnCode**: Mã Terminal ID đăng ký với VNPay.
- **HashSecret**: Chuỗi bí mật dùng để tạo chữ kýChecksum.
- **ReturnUrl**: Địa chỉ trình duyệt sẽ quay lại sau khi người dùng thanh toán xong.

## 4. Cách sử dụng (How to Use)

1.  **Đăng ký Sandbox**: Bạn có thể sử dụng thông tin thẻ test của VNPay tại trang [VNPAY Sandbox](https://sandbox.vnpayment.vn/apis/vnpay-demo/).
2.  **Thông tin thẻ test**:
    - **Ngân hàng**: NCB
    - **Số thẻ**: `9704198526191432198`
    - **Tên chủ thẻ**: `NGUYEN VAN A`
    - **Ngày phát hành**: `07/15`
    - **Mật khẩu OTP**: `123456`

## 5. Lưu ý kỹ thuật (Technical Notes)

- **Đơn vị tiền tệ**: VNPay yêu cầu giá trị thanh toán nhân với 100 (Ví dụ: `100,000 VND` sẽ gửi lên là `10,000,000`).
- **Xử lý IPN**: Đảm bảo cổng Receiver (`PaymentIPN`) của bạn có thể truy cập được từ bên ngoài (Internet) để VNPay gọi tới. Nếu chạy localhost, hãy sử dụng **Ngrok**.
- **Tính toàn vẹn**: Luôn luôn kiểm tra `vnp_SecureHash` trước khi cập nhật bất kỳ trạng thái nào trong Database để tránh tấn công thay đổi dữ liệu URL.
- **Đối soát giao dịch (Reconciliation)**: Hệ thống ghi nhận mã định danh `TransactionNo` trong bảng `Payments`. Đối với VNPay, đây là mã `vnp_TransactionNo` từ ngân hàng; đối với ADMIN duyệt tay, đây là mã `MANUAL_APPROVAL`.

## 6. Xác nhận Thanh toán Thủ công (Manual Payment)

Trong trường hợp luồng thanh toán VNPay gặp sự cố hoặc khách hàng thanh toán qua phương thức khác (Tiền mặt, Chuyển khoản ngân hàng trực tiếp), hệ thống hỗ trợ cơ chế xác nhận thủ công (Manual Confirmation) dành riêng cho Administrator.

### Luồng xử lý thủ công (Manual Workflow)

1. **Khách hàng** hoàn tất đặt vé đi đến trang chờ thanh toán (Ticket có tình trạng `CONFIRMED`).
2. Khách hàng thực hiện chuyển khoản bằng mã Booking hoặc đến quầy đóng tiền mặt.
3. Quản trị viên vào **Admin Dashboard**, mở trang `Admin/ConfirmTicket` (Xác nhận Đặt chỗ).
4. Tại đây hiển thị danh sách tất cả các vé đang chờ tiền (`Status == "CONFIRMED"`).
5. Quản trị viên nhấn nút Xác nhận. Lệnh `POST /Admin/ProcessConfirm` được thực thi.
6. Hệ thống thực hiện một Database Transaction ngầm:
   - Thay đổi Booking Status thành `PAID`
   - Thay đổi mọi Ticket Status trong Booking thành `PAID`
   - Khởi tạo một bản ghi `Payment` với thông số `PaymentMethod = "MANUAL_ADMIN"`, `PaymentStatus = "SUCCESS"`, và `TransactionNo = "MANUAL_APPROVAL"`.
7. Thông báo thành công được hiển thị cho Admin và vé chính thức chuyển sang hợp lệ.

> [!TIP]
> Việc lưu `MANUAL_ADMIN` vào cột `PaymentMethod` của bảng `Payments` là một thiết kế rất khéo léo để bộ phận Kế toán biết nguồn tiền này không đối soát từ ví điện tử mà do một nhân viên thủ công duyệt. Dựa vào thao tác này, kế toán có thể thu tiền mặt trực tiếp từ Admin/Nhân viên bán vé đó.
