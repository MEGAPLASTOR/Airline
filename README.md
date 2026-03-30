# SkyWave Airlines Reservation System

## Giới thiệu

Dự án **Airline** là hệ thống quản lý đặt vé máy bay hiện đại, được xây dựng trên nền tảng .NET 8/9, cung cấp đầy đủ các tính năng cho cả hành khách và quản trị viên. Hệ thống được thiết kế với giao diện cao cấp, chuyên nghiệp và tối ưu trải nghiệm người dùng.

## Cấu trúc thư mục định danh

- Airline/: Mã nguồn chính của ứng dụng ASP.NET Core MVC.
  - Controllers/: Logic điều hướng và xử lý yêu cầu.
    - `AdminBookingController.cs`: Xác nhận thanh toán vé thủ công. **[SANG]**
    - `BookingController.cs`: Quy trình đặt chuyến bay khách hàng. **[SANG-FIX]**
    - `Accountcontroller.cs`: Đăng nhập/Đăng ký & Xác thực Cookie. **[SANG-FIX]**
    - `PaymentController.cs`: Tích hợp cổng VNPay & Xử lý đối soát TransactionNo.
    - `AdminDashboardController.cs`: Bảng điều khiển thống kê tổng quan cho Admin.
    - `AdminAccountController.cs`: Quản lý danh sách tài khoản và phân quyền người dùng.
    - `AdminCityController.cs`: Quản lý thông tin điểm đi/đến (Cities).
    - `AdminFlightController.cs`: Quản lý thông tin kỹ thuật các chuyến bay (Flights).
    - `AdminRouteController.cs`: Quản lý hành trình và các tuyến đường bay (Routes).
    - `AdminScheduleController.cs`: Điều phối lịch trình, thời gian bay và sơ đồ ghế.
    - `AdminBaseController.cs`: Lớp cơ sở kiểm soát quyền truy cập vùng Admin.
    - `TicketPriceController.cs`: Thiết lập bảng giá vé linh hoạt theo lịch trình.
    - `ManageTicketClassController.cs`: Quản lý các hạng ghế (Business/Economy).
    - `TicketController.cs`: Hiển thị thông tin vé và xử lý đổi chỗ ngồi.
    - `BaggageController.cs`: Đăng ký dịch vụ hành lý ký gửi kèm theo vé.
    - `ProfileController.cs`: Chỉnh sửa thông tin cá nhân và mật khẩu hành khách.
    - `HomeController.cs`, `AboutController.cs`, `ContactController.cs`: Các trang thông tin chung.
  - Services/: **[MỚI]**
    - `SeatService.cs`: Logic tự động sinh 180 ghế (30 hàng) và phân bổ khoang ghế linh hoạt.
  - Models/: Thực thể cơ sở dữ liệu và ViewModels.
    - `BookingViewModel.cs`: Lớp trung gian xử lý quy trình đặt vé.
    - `Payment.cs`: Lưu trữ giao dịch thành công kèm `TransactionNo`.
    - `...`: Toàn bộ 18 Entity và ViewModel hỗ trợ nghiệp vụ (User, Flight, Ticket, v.v.).
  - Views/: Hệ thống giao diện Razor (.cshtml) bậc cao.
    - `About/Index.cshtml`: Trang giới thiệu về hãng hàng không SkyWave Airlines.
    - `Account/ChangePassword.cshtml`: Giao diện thay đổi mật khẩu người dùng bảo mật.
    - `Account/EditAccount.cshtml`: Giao diện chỉnh sửa thông tin cá nhân hành khách.
    - `Admin/AdminDashboard.cshtml`: Bảng điều khiển thống kê tổng quan dành cho quản trị.
    - `Admin/ConfirmTicket.cshtml`: Phê duyệt các đơn đặt chỗ thanh toán thủ công. **[SANG]**
    - `Admin/FlightReschedule.cshtml`: Giao diện thay đổi lịch trình và giờ bay linh hoạt.
    - `Admin/FlightSchedules.cshtml`: Quản lý danh sách lịch trình bay tổng thể.
    - `Admin/FlightSeats.cshtml`: Sơ đồ ghế động hỗ trợ đóng/mở chỗ trực quan.
    - `Admin/ManageAccounts.cshtml`: Quản lý danh sách người dùng và phân quyền hệ thống.
    - `Admin/ManageCity.cshtml`: Quản lý thông tin điểm đi/đến (Cities).
    - `Admin/ManageFlights.cshtml`: Quản lý thông tin kỹ thuật của các chuyến bay (Flights).
    - `Admin/ManageRoutes.cshtml`: Quản lý hành trình và các tuyến đường bay (Routes).
    - `Admin/TicketClasses.cshtml`: Cấu hình các hạng vé (Business, Economy, v.v.).
    - `Admin/TicketPrice.cshtml`: Thiết lập bảng giá vé linh động cho từng lịch trình.
    - `Baggage/RegisterForm.cshtml`: Biểu mẫu đăng ký dịch vụ hành lý ký gửi.
    - `Baggage/SelectTicket.cshtml`: Giao diện lựa chọn vé để đăng ký thêm hành lý.
    - `Booking/BookFlight.cshtml`: Trang tìm kiếm và lựa chọn chuyến bay khởi hành. **[SANG-FIX]**
    - `Booking/BookingSuccess.cshtml`: Thông báo hoàn tất quy trình đặt vé thành công. **[SANG-FIX]**
    - `Booking/PassengerInfo.cshtml`: Biểu mẫu nhập thông tin chi tiết các hành khách. **[SANG-FIX]**
    - `Booking/SelectSeat.cshtml`: Sơ đồ ghế cho khách hàng chủ động chọn chỗ ngồi. **[SANG-FIX]**
    - `Contact/Index.cshtml`: Trang liên hệ và gửi yêu cầu hỗ trợ khách hàng.
    - `Home/Index.cshtml`: Trang chủ hiện đại tích hợp bộ lọc tìm kiếm chuyến bay.
    - `Home/Privacy.cshtml`: Chính sách bảo mật và điều khoản sử dụng hệ thống.
    - `Payment/PaymentResult.cshtml`: Hiển thị kết quả thanh toán phản hồi từ cổng VNPay.
    - `Shared/Error.cshtml`: Trang hiển thị thông tin lỗi hệ thống tập trung.
    - `Shared/_AdminLayout.cshtml`: Layout khung chuẩn chuyên nghiệp cho vùng Admin. **[SANG-FIX]**
    - `Shared/_Layout.cshtml`: Layout khung cao cấp (Dark Mode) dành cho khách hàng. **[SANG-FIX]**
    - `Shared/_ValidationScriptsPartial.cshtml`: Thư viện xử lý kiểm tra (Validation) client-side.
    - `Ticket/ChangeSeat.cshtml`: Giao diện hỗ trợ khách hàng tự đổi chỗ ngồi sau khi đặt.
    - `Ticket/ViewConfirmation.cshtml`: Xem chi tiết thông tin và mã xác nhận vé đã đặt.
    - `_ViewImports.cshtml`, `_ViewStart.cshtml`: Các tệp cấu hình mặc định cho Razor View.
  - DataContext.cs: Cấu hình Entity Framework Core.
  - Program.cs: Cấu hình Services & Pipeline.
- Airline.slnx: Tệp giải pháp thực thi dự án.
- payment.md: Tài liệu chi tiết thanh toán (VNPay & Manual).
- VNpay.md: Cấu hình VNPay Sandbox.
- VNpayProblem.md: Vấn đề IPN trên Localhost.
- booking.md: Quy trình đặt vé khách hàng. **[SANG-FIX]**

## Các chức năng chính

### 1. Dành cho Hành khách (Customer)

- **Đăng ký & Đăng nhập**: Hệ thống xác thực bằng Cookie an toàn.
- **Quản lý Tài khoản**: Chỉnh sửa thông tin cá nhân và đổi mật khẩu.
- **SkyMiles**: Hệ thống điểm thưởng tích lũy cho hành khách thân thiết.
- **Tìm kiếm & Đặt vé**: Quy trình 4 bước tối ưu, đã được **Anh hóa (English)** toàn bộ giao diện để tăng tính chuyên nghiệp.
- **Sơ đồ 180 Ghế**: Hệ thống chọn chỗ ngồi mật độ cao (30 hàng) với thanh cuộn tối ưu và cập nhật giá thời gian thực.
- **Thanh toán trực tuyến**: Tích hợp VNPay (Sandbox) hỗ trợ thanh toán vé nhanh chóng.
- **Quản lý vé**: Xem ticket confirmation và thực hiện đổi chỗ ngồi (Change Seat). **[SANG-FIX]**

### 2. Dành cho Quản trị viên (Admin)

- **Xác nhận vé (Confirm Ticket)**: Phê duyệt các đơn đặt chỗ thanh toán thủ công. **[SANG]**
- **Dashboard Premium**: Biểu đồ và thống kê tổng quan trạng thái hệ thống.
- **Quản lý Toàn diện**: Tài khoản, Thành phố, Tuyến bay, Chuyến bay và Lịch trình.
- **Sơ đồ Ghế Động**: Đóng/Mở chỗ ngồi trực tiếp trên sơ đồ trực quan, hỗ trợ hiển thị đầy đủ 180 ghế.
- **Tự động Khởi tạo Ghế**: Hệ thống tự động sinh dữ liệu 180 ghế cho mỗi lịch trình bay mới dựa trên bảng giá.
- **Giá vé Linh hoạt**: Cấu hình giá theo hạng ghế (Business/Economy) và lịch trình cụ thể.

## Công nghệ sử dụng

- **Backend**: .NET 8/9, ASP.NET Core MVC.
- **Database**: SQL Server, EF Core.
- **Frontend**: Razor Pages, Vanilla CSS, JS, JetBrains Mono & Playfair Display fonts.
- **Authentication**: Cookie-based Auth.

## Hướng dẫn chạy dự án

tạo và thêm dữ liệu vào database: dotnet ef database update

1. **Cấu hình SQL Server**: Cập nhật chuỗi kết nối trong appsettings.json.
2. **Khởi tạo Database**: Chạy Migrations hoặc thực thi script SQL đi kèm.
3. **VNPay**: Cập nhật thông số Sandbox trong cấu hình hệ thống.
4. **Chạy ứng dụng**: dotnet run --project Airline.

## Changelog & Cập nhật gần đây

### 2026-03-30: Sửa lỗi Đặt vé, Xác thực & Thanh toán

- **AccountController**: Bổ sung `ClaimTypes.NameIdentifier` chứa `UserId` vào hệ thống Cookie SignIn.
- **BookingController**: Sửa lỗi Form Validation, sập giao diện và tự động tạo hạng vé.
- **Payment System**: Bổ sung `TransactionNo`, cập nhật logic ghi nhận mã giao dịch VNPay/Manual và hỗ trợ Ngrok.

### 2026-03-31: Hệ thống 180 Ghế & Anh hóa Giao diện
- **SeatService**: Triển khai logic tự động sinh 180 ghế (30 hàng) với khả năng phân bổ khoang ghế động dựa trên `TicketPrice`.
- **English UI**: Anh hóa toàn bộ quy trình đặt vé (BookFlight, SelectSeat, PassengerInfo, BookingSuccess) và trang quản lý ghế.
- **UI Optimization**: Tối ưu sơ đồ ghế với vùng cuộn (scroll area) cố định, giúp hiển thị mượt mà 30 hàng ghế trên màn hình.
- **Data Integrity**: Chuyển đổi sang mô hình quan hệ `Seat` giúp quản lý chỗ ngồi chính xác, chống tình trạng đặt trùng ghế (Overbooking).

---
