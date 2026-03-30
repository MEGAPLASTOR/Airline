# Quy trình Đặt vé Máy bay (Customer Booking Flow)

Tài liệu này mô tả chi tiết luồng nghiệp vụ và quy trình kỹ thuật khi khách hàng thực hiện đặt vé máy bay trên hệ thống SkyWave Airlines.

## 1. Tổng quan Luồng Nghiệp vụ (Flowchart)

```mermaid
graph TD
    A[Trang tìm kiếm chuyến bay] -->|Chọn chuyến bay| B[Sơ đồ chọn chỗ ngồi]
    B -->|Chọn ghế & Tiếp tục| C[Thông tin hành khách]
    C -->|Xác nhận đặt vé| D{Giao dịch SQL (Transaction)}
    D -->|Lỗi| C
    D -->|Thành công| E[Trang thành công / Mã đặt chỗ]
    E -->|Thanh toán ngay| F[Cổng thanh toán]
    F -->|Thành công| G[Cập nhật trạng thái PAID]
    F -->|Thất bại| H[Thông báo lỗi thanh toán]
    G -->|Xem vé| I[Vé của tôi / Quản lý đặt chỗ]
```

---

## 2. Chi tiết các Bước thực hiện

### Bước 1: Tìm kiếm & Chọn Chuyến bay
- **URL**: `/Booking/BookFlight`
- **Controller**: `BookingController.cs`
- **Mô tả**: Hiển thị tất cả các bản ghi `FlightSchedule` có trạng thái `SCHEDULED` và thời gian khởi hành trong tương lai.
- **Giá vé 4 khoang**: Mỗi chuyến bay hiển thị mức giá "Chỉ từ", giá thực tế sẽ phụ thuộc vào khoang ghế khách hàng chọn ở bước sau.

### Bước 2: Sơ đồ chọn Chỗ ngồi tương tác
- **URL**: `/Booking/SelectSeat/{id}`
- **Logic xử lý**: 
    1. Hệ thống gọi `SeatService.GenerateSeatsAsync(id)` để đảm bảo sơ đồ 4 khoang ghế đã được tạo cho lịch trình này.
    2. Truy vấn tất cả thực thể `Seat` liên kết với `ScheduleId`.
- **Cấu trúc 4 Khoang ghế (4 Cabins)**:
    - **Hạng Nhất (First Class)**: Hàng 1 (Cấu hình 1-2-1).
    - **Hạng Thương gia (Business Class)**: Hàng 2-3 (Cấu hình 2-2).
    - **Hạng Phổ thông Đặc biệt (Premium Economy)**: Hàng 4-5 (Cấu hình 2-3-2).
    - **Hạng Phổ thông (Economy)**: Hàng 6-10 (Cấu hình 3-3).
- **Trạng thái Ghế**:
    - **AVAILABLE (Trống)**: Màu xanh (Cho phép chọn).
    - **BOOKED (Đã đặt)**: Màu san hô (Không thể chọn).
    - **BLOCKED (Khóa)**: Màu xám (Admin khóa thủ công).

### Bước 3: Thông tin Hành khách
- **URL**: `/Booking/PassengerInfo` (POST)
- **ViewModel**: `BookingViewModel.cs` lưu trữ `SeatId`, `ScheduleId` và giá vé đã tính toán.
- **Dữ liệu đầu vào**:
    - **Họ và tên**: Phải khớp với CMND/CCCD hoặc Hộ chiếu.
    - **Loại hành khách**: Người lớn / Trẻ em / Em bé.
- **Tính toán Giá vé**: Giá được lấy từ bảng `TicketPrice` dựa trên cặp `(ScheduleId, ClassId)`. Thuế 10% được cộng thêm vào tổng tiền.

### Bước 4: Xác nhận và Giao dịch ACID
- **Action**: `ConfirmBooking` (POST) trong `BookingController.cs`
- **Quy trình kỹ thuật**:
    1. **Kiểm tra Xác thực**: Xác minh người dùng đã đăng nhập.
    2. **Database Transaction**: Bắt đầu một `DbTransaction` để đảm bảo tính toàn vẹn dữ liệu.
    3. **Tính nhất quán quan hệ**:
        - **Booking**: Được tạo với trạng thái `PENDING_PAYMENT`.
        - **Passenger**: Được tạo và liên kết với `BookingId`.
        - **Ticket**: Được tạo và liên kết với `BookingId`, `PassengerId`, và `SeatId` cụ thể.
        - **Trạng thái Ghế**: Cập nhật thành `BOOKED` cho `SeatId` tương ứng.
        - **Sức chứa**: `FlightSchedule.AvailableSeats` được trừ đi 1.
    4. **Commit**: Nếu tất cả 5 bước trên thành công, giao dịch được xác nhận (Commit); nếu không, toàn bộ sẽ được Rollback.

### Bước 5: Hoàn tất (Booking Success)
- **View**: `BookingSuccess.cshtml`
- **Mô tả**: Hiển thị Mã đặt chỗ (Booking ID) duy nhất và cung cấp các nút thao tác nhanh để Thanh toán hoặc Quản lý vé.

---

## 3. Các Thành phần Kỹ thuật Chính

### Controllers
- **`BookingController.cs`**: Điều phối chính toàn bộ luồng 5 bước của khách hàng.
- **`AdminScheduleController.cs`**: Cho phép Admin quản lý trạng thái ghế (Khóa/Mở) hoặc xem chi tiết hành khách.

### Services
- **`SeatService.cs`**: Logic tập trung để tạo sơ đồ ghế ngồi 4 khoang (First, Business, Premium, Economy) cho mọi chuyến bay mới.

### Models & Database
- **`Seat`**: Thực thể mới đại diện cho một ghế vật lý trên một lịch trình bay cụ thể.
- **`BookingViewModel`**: Đối tượng DTO dùng để truyền dữ liệu qua các bước của wizard.
- **`TicketPrice`**: Bảng chuẩn hóa lưu trữ giá theo từng Hạng vé của mỗi Lịch trình.

---

## 4. Lưu ý Quan trọng
- **Đặt vé theo Quan hệ**: Hệ thống không còn lưu `SeatNumber` dưới dạng chuỗi đơn thuần trong bảng Ticket. Mọi thứ được liên kết qua `SeatId` đến bảng `Seats`.
- **Chống Overbooking**: Giao dịch SQL và kiểm tra `SeatStatus` đảm bảo không có hai người nào có thể đặt cùng một ghế tại cùng một thời điểm.
- **Quản lý linh hoạt**: Admin có thể "Khóa" ghế trực tiếp từ Dashboard, hệ thống sẽ cập nhật `SeatStatus` ngay lập tức để ngăn khách hàng đặt chỗ.
