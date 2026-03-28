# SkyWave Airlines Reservation System

## Giới thiệu

Dự án **Airline** là hệ thống quản lý đặt vé máy bay hiện đại, được xây dựng trên nền tảng .NET 8/9, cung cấp đầy đủ các tính năng cho cả hành khách và quản trị viên. Hệ thống được thiết kế với giao diện cao cấp, chuyên nghiệp và tối ưu trải nghiệm người dùng.

## Kiến trúc và Cấu trúc tệp tin

Dưới đây là cấu trúc chính của dự án:

- `Airline/`: Mã nguồn chính của ứng dụng ASP.NET Core MVC.
    - `Controllers/`: Chứa logic điều hướng và xử lý yêu cầu.
        - `AdminBaseController.cs`: Controller cơ sở cho các chức năng quản trị.
        - `AdminDashboardController.cs`: Bảng điều khiển trung tâm dành cho Admin.
        - `AdminAccountController.cs`: Quản lý danh sách tài khoản người dùng và quản trị viên.
        - `AdminCityController.cs`: Quản lý thông tin điểm đi/đến (Cities).
        - `AdminRouteController.cs`: Quản lý các tuyến đường bay (Routes).
        - `AdminFlightController.cs`: Quản lý thông tin các chuyến bay (Flights).
        - `AdminScheduleController.cs`: Quản lý lịch trình bay và chỗ ngồi. **[sang-fix + Thêm FlightSeats, GetSeatDetails, ToggleSeatStatus]**
        - `ManageTicketClassController.cs`: Quản lý các hạng vé (Business, Economy...).
        - `Accountcontroller.cs`: Xử lý đăng nhập, đăng ký và xác thực Cookie. **[sang-fix + Sửa tên file bỏ khoảng trắng]**
        - `ProfileController.cs`: Cập nhật thông tin cá nhân và mật khẩu cho người dùng.
        - `BookingController.cs`: Xử lý quy trình đặt chuyến bay. **[sang]**
        - `TicketController.cs`: Quản lý thông tin và xác nhận vé.
  - `Models/`: Chứa các thực thể cơ sở dữ liệu và ViewModels.
    - `BookingViewModel.cs`: Lớp trung gian xử lý quy trình đặt vé. **[sang]**
    - `User.cs`: Thông tin người dùng (Admin/User).
    - `Flight.cs`, `FlightSchedule.cs`: Thông tin chuyến bay và lịch trình.
    - `Route.cs`, `Cities.cs`: Quản lý hành trình và các thành phố điểm đi/đến.
    - `Booking.cs`, `Ticket.cs`, `Passenger.cs`: Thông tin đơn đặt chỗ, vé và hành khách.
    - `Baggage.cs`, `Payment.cs`: Dịch vụ hành lý và thanh toán.
  - `Views/`: Chứa các giao diện Razor (.cshtml).
    - `Home/Index.cshtml`: Trang chủ với thiết kế hiện đại, các lộ trình phổ biến.
    - `Admin/AdminDashboard.cshtml`: Giao diện quản trị hệ thống.
    - `Admin/FlightSeats.cshtml`: Giao diện quản lý sơ đồ ghế cho Admin. **[sang]**
    - `Booking/`: Chứa các trang đặt vé (BookFlight, SelectSeat, PassengerInfo, Success). **[sang]**
    - `Account/`: Các trang đăng nhập, đăng ký, chỉnh sửa tài khoản.
  - `DataContext.cs`: Cấu hình Entity Framework Core và ánh xạ cơ sở dữ liệu.
  - `Program.cs`: Nơi cấu hình Services, Pipeline và Dependency Injection.
  - `wwwroot/`: Tài nguyên tĩnh như CSS, JavaScript, hình ảnh và biểu tượng.
- `Airline.slnx`: Tệp giải pháp thực thi dự án.
- `.git/` & `.gitignore`: Quản lý phiên bản mã nguồn.
- `README.md`: Tài liệu hướng dẫn và cấu trúc dự án. **[sang]**

## Các chức năng chính

### 1. Dành cho Hành khách (Customer)

- **Đăng ký & Đăng nhập**: Hệ thống xác thực bằng Cookie an toàn.
- **Quản lý Tài khoản**: Chỉnh sửa thông tin cá nhân (Họ tên, Email, SĐT, CCCD, Địa chỉ, Giới tính, Tuổi) và đổi mật khẩu.
- **SkyMiles**: Hệ thống điểm thưởng tích lũy cho khách hàng thân thiết.
- **Tìm kiếm & Đặt vé**: Quy trình đặt vé nhanh chóng, hỗ trợ chọn hành trình linh hoạt.
- **Dịch vụ Hành lý**: Đăng ký và tính phí hành lý theo trọng lượng.
- **Thanh toán trực tuyến**: Ghi nhận trạng thái thanh toán và phương thức giao dịch.
- **Khuyến mãi**: Áp dụng mã giảm giá trực tiếp vào đơn đặt vé.
- **Quản lý vé**: Xem lại các vé đã đặt và thông tin xác nhận.

### 2. Dành cho Quản trị viên (Admin)

- **Bảng điều khiển (Dashboard)**: Tổng quan trạng thái hệ thống.
- **Quản lý Tài khoản**: Xem, tạo, cập nhật và xóa tài khoản người dùng hoặc quản trị viên khác.
- **Quản lý Địa lý**: Thêm/Sửa/Xóa các thành phố (Cities) và lộ trình (Routes).
- **Quản lý Chuyến bay**: Thiết lập các chuyến bay cụ thể gắn liền với lộ trình.
- **Lập lịch trình (Scheduling)**: Quản lý thời gian bay (Departure/Arrival) và số lượng ghế trống.
- **Thay đổi lịch trình (Reschedule)**: Cập nhật lại thời gian bay cho các chuyến bay hiện có.
- **Quản lý Hạng vé**: Thiết lập các loại hạng vé (Business, Economy...) và giá vé tương ứng cho từng lịch trình.

## Công nghệ sử dụng

- **Backend**: .NET 8/9, ASP.NET Core MVC.
- **Database**: SQL Server, Entity Framework Core (Code First/Database First).
- **Frontend**: Razor Pages, Vanilla CSS (Modern design), JavaScript, FontAwesome.
- **Authentication**: Cookie-based Authentication.

## Hướng dẫn chạy dự án

1. **Cấu hình SQL Server**: Cập nhật chuỗi kết nối (Connection String) trong `Airline/DataContext.cs` hoặc `appsettings.json`.
2. **Khởi tạo Database**: Chạy Migrations (nếu có) hoặc thực thi SQL script để tạo bảng.
3. **Chạy ứng dụng**: Mở `Airline.slnx` bằng Visual Studio hoặc sử dụng lệnh:
   ```bash
   dotnet run --project Airline
   ```
4. **Đăng nhập Admin**: Sử dụng tài khoản có thuộc tính `Role = 'ADMIN'`.

---

_Dự án được phát triển với mục tiêu cung cấp giải pháp đặt vé máy bay hiện đại và toàn diện nhất._
