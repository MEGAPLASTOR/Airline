# SkyWave Airlines Reservation System

## Giới thiệu

Dự án **Airline** là hệ thống quản lý đặt vé máy bay hiện đại, được xây dựng trên nền tảng .NET 8/9, cung cấp đầy đủ các tính năng cho cả hành khách và quản trị viên. Hệ thống được thiết kế với giao diện cao cấp, chuyên nghiệp và tối ưu trải nghiệm người dùng.

## Cấu trúc thư mục định danh

- Airline/: Mã nguồn chính của ứng dụng ASP.NET Core MVC.
  - Controllers/: Chứa logic điều hướng và xử lý yêu cầu.
    - AdminBaseController.cs: Controller cơ sở cho các chức năng quản trị.
    - AdminDashboardController.cs: Bảng điều khiển trung tâm dành cho Admin.
    - AdminBookingController.cs: Xử lý xác thực và xác nhận thanh toán vé thủ công. **[SANG]**
    - AdminAccountController.cs: Quản lý danh sách tài khoản người dùng và quản trị viên.
    - AdminCityController.cs: Quản lý thông tin điểm đi/đến (Cities).
    - AdminRouteController.cs: Quản lý các tuyến đường bay (Routes).
    - AdminFlightController.cs: Quản lý thông tin các chuyến bay (Flights).
    - AdminScheduleController.cs: Quản lý lịch trình bay, thay đổi lịch và sơ đồ ghế.
    - TicketPriceController.cs: Quản lý giá vé linh hoạt theo lịch trình và hạng ghế.
    - ManageTicketClassController.cs: Quản lý các hạng vé (Business, Economy...).
    - Accountcontroller.cs: Xử lý đăng nhập, đăng ký và xác thực Cookie.
    - ProfileController.cs: Cập nhật thông tin cá nhân và mật khẩu cho người dùng.
    - BookingController.cs: Xử lý quy trình đặt chuyến bay.
    - TicketController.cs: Quản lý thông tin, xác nhận vé và đổi chỗ ngồi.
    - BaggageController.cs: Đăng ký hành lý ký gửi kèm theo vé.
    - PaymentController.cs: Tích hợp cổng thanh toán VNPay để xử lý giao dịch vé.
  - Models/: Chứa các thực thể cơ sở dữ liệu và ViewModels.
    - BookingViewModel.cs: Lớp trung gian xử lý quy trình đặt vé.
    - User.cs: Thông tin người dùng (Admin/User).
    - Flight.cs, FlightSchedule.cs: Thông tin chuyến bay và lịch trình.
    - Route.cs, Cities.cs: Quản lý hành trình và các thành phố điểm đi/đến.
    - Booking.cs, Ticket.cs, Passenger.cs: Thông tin đơn đặt chỗ, vé và hành khách.
    - Baggage.cs, Payment.cs: Dịch vụ hành lý và thanh toán.
    - TicketPrice.cs: Thực thể lưu trữ cấu hình giá vé.
  - Views/: Chứa các giao diện Razor (.cshtml).
    - Home/Index.cshtml: Trang chủ với thiết kế hiện đại.
    - Admin/ConfirmTicket.cshtml: Giao diện xác nhận vé cho Admin (Premium). **[SANG]**
    - Admin/: Các giao diện quản trị Dashboard, Flights, Schedules, Seats và Prices.
    - Booking/: Chứa các trang đặt vé (BookFlight, SelectSeat, PassengerInfo, Success). **[SANG-FIX]**
    - Shared/\_Layout.cshtml: Layout khách hàng với giao diện Dark Mode cao cấp. **[SANG-FIX]**
    - Shared/\_AdminLayout.cshtml: Layout quản trị với hệ thống điều hướng đồng bộ. **[SANG-FIX]**
    - Account/: Các trang đăng nhập, đăng ký, chỉnh sửa tài khoản.
  - DataContext.cs: Cấu hình Entity Framework Core.
  - Program.cs: Cấu hình Services và Pipeline.
  - wwwroot/: Chứa tài nguyên tĩnh (CSS/JS).
    - css/admin-confirm-ticket.css, js/admin-confirm-ticket.js: Tài nguyên Admin. **[SANG]**
    - css/booking/, js/booking/: Bộ tài nguyên riêng biệt cho quy trình đặt vé. **[SANG]**
    - css/site.css, js/site.js: Tài nguyên chung của hệ thống.
- Airline.slnx: Tệp giải pháp thực thi dự án.
- .git/ & .gitignore: Quản lý phiên bản mã nguồn.
- README.md: Tài liệu hướng dẫn và cấu trúc dự án. **[SANG-FIX]**
- payment.md: Tài liệu chi tiết về tích hợp VNPay.
- booking.md: Tài liệu quy trình đặt vé khách hàng.

## Các chức năng chính

### 1. Dành cho Hành khách (Customer)

- **Đăng ký & Đăng nhập**: Hệ thống xác thực bằng Cookie an toàn.
- **Quản lý Tài khoản**: Chỉnh sửa thông tin cá nhân và đổi mật khẩu.
- **SkyMiles**: Hệ thống điểm thưởng tích lũy cho hành khách thân thiết.
- **Tìm kiếm & Đặt vé**: Quy trình 4 bước tối ưu (Tìm kiếm -> Chọn ghế -> Thông tin hành khách -> Thành công).
- **Thanh toán trực tuyến**: Tích hợp VNPay (Sandbox) hỗ trợ thanh toán vé nhanh chóng.
- **Quản lý vé**: Xem ticket confirmation và thực hiện đổi chỗ ngồi (Change Seat). **[sang-fix]**

### 2. Dành cho Quản trị viên (Admin)

- **Xác nhận vé (Confirm Ticket)**: Phê duyệt các đơn đặt chỗ thanh toán thủ công. **[SANG]**
- **Dashboard Premium**: Biểu đồ và thống kê tổng quan trạng thái hệ thống.
- **Quản lý Toàn diện**: Tài khoản, Thành phố, Tuyến bay, Chuyến bay và Lịch trình.
- **Sơ đồ Ghế Động**: Đóng/Mở chỗ ngồi trực tiếp trên sơ đồ trực quan.
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

---
