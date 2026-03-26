# Airline Reservation System

Hệ thống quản lý đặt vé máy bay và đặt chỗ ngồi chuyên nghiệp, được xây dựng trên nền tảng ASP.NET Core 8.0 MVC và Entity Framework Core.

## 🚀 Chức năng chính

### 1. Quản lý Chỗ ngồi (Admin) - [Sang]

- **Quản lý Sơ đồ Ghế**: Cho phép Admin thiết lập sơ đồ ghế cho từng chuyến bay (`SeatController/Index`).
- **Tùy chỉnh Trạng thái**: Bật/Tắt tính khả dụng của từng ghế (`Active/Inactive`).
- **Phân hạng Vé**: Gắn nhãn hạng vé (Thương gia/Phổ thông) cho từng vị trí ghế.
- **Thao tác nhanh**: Thêm mới hoặc xóa ghế linh hoạt.

### 2. Đặt chỗ & Chọn ghế (Khách hàng) - [Sang]

- **Tìm kiếm Chuyến bay**: Tìm lộ trình theo điểm đi/đến và ngày khởi hành (`BookingController/BookFlight`).
- **Chọn ghế trực quan**: Sơ đồ ghế 2D cho phép khách hàng chọn vị trí ngồi mong muốn (`BookingController/SelectSeat`).
- **Kiểm tra tình trạng**: Tự động nhận diện và khóa các ghế đã được đặt hoặc không khả dụng.

### 3. Quản lý Vận hành (Admin)

- **Manage Flights**: Quản lý số hiệu chuyến bay, gán tuyến bay (`AdminController/ManageFlights`).
- **Flight Schedules**: Thiết lập lịch bay cụ thể (Giờ đi/đến, giá vé).
- **Manage Routes & Cities**: Quản lý mạng lưới điểm đến và tuyến đường bay.
- **User Management**: Quản trị tài khoản và phân quyền người dùng.

---

## 📂 Cấu trúc thư mục

Sơ đồ cấu trúc các thành phần quan trọng (Đánh dấu `[Sang]` cho các file mới tạo):

```text
Airline/
├── Controllers/
│   ├── SeatController.cs        # [Sang] Logic quản lý ghế Admin
│   ├── BookingController.cs     # [Sang] Logic đặt vé & chọn chỗ khách hàng
│   ├── AdminController.cs       # Quản trị hệ thống tổng quát
│   └── ProfileController.cs     # Quản lý thông tin cá nhân
├── Models/
│   ├── Seat.cs                  # [Sang] Thực thể Ghế ngồi
│   ├── Booking.cs               # Thông tin đặt vé
│   ├── Flight.cs                # Thông tin chuyến bay
│   ├── FlightSchedule.cs        # Lịch trình bay chi tiết
│   ├── Ticket.cs                # Thông tin vé đã xuất
│   └── DataContext.cs           # Entity Framework context & mapping
├── Views/
│   ├── Seat/                    # [Sang] View quản lý sơ đồ ghế
│   ├── Booking/                 # [Sang] View tìm kiếm & chọn chỗ
│   ├── Admin/                   # View quản trị Admin
│   └── Shared/                  # Layouts (_Layout, _AdminLayout)
├── wwwroot/                     # Tài nguyên tĩnh (CSS, JS, Images)
├── appsettings.json             # Cấu hình Database (ConnectionStrings)
└── Program.cs                   # Khởi chạy & cấu hình ứng dụng
```

---

## ⚙️ Hướng dẫn cài đặt & Cấu hình

1. **Cơ sở dữ liệu**:
   - Project sử dụng SQL Server với Database tên là `AirlineReservation`.
   - Cấu trúc dữ liệu tuân theo mô hình **Chuẩn hóa (Normalized)** với đầy đủ quan hệ giữa Chuyến bay, Tuyến bay, Vé và Hành khách.

2. **Cấu hình Kết nối**:
   - Chỉnh sửa chuỗi kết nối trong `appsettings.json` cho phù hợp với Server của bạn:

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER;Database=AirlineReservation;Trusted_Connection=True;..."
   }
   ```

3. **Chạy Project**:
   - Mở Terminal tại thư mục `/Airline`.
   - Chạy lệnh: `dotnet run` hoặc nhấn F5 trong Visual Studio.
