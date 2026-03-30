using Microsoft.EntityFrameworkCore;

namespace Airline.Models;

public partial class DataContext : DbContext
{
    public DataContext()
    {
    }

    public DataContext(DbContextOptions<DataContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Baggage> Baggages { get; set; }
    public virtual DbSet<Booking> Bookings { get; set; }
    public virtual DbSet<BookingPromotion> BookingPromotions { get; set; }
    public virtual DbSet<Cities> Cities { get; set; }
    public virtual DbSet<Flight> Flights { get; set; }
    public virtual DbSet<FlightSchedule> FlightSchedules { get; set; }
    public virtual DbSet<Passenger> Passengers { get; set; }
    public virtual DbSet<Payment> Payments { get; set; }
    public virtual DbSet<Promotion> Promotions { get; set; }
    public virtual DbSet<Route> Routes { get; set; }
    public virtual DbSet<Seat> Seats { get; set; }
    public virtual DbSet<Ticket> Tickets { get; set; }
    public virtual DbSet<TicketClass> TicketClasses { get; set; }
    public virtual DbSet<TicketPrice> TicketPrices { get; set; }
    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Baggage>(entity =>
        {
            entity.HasKey(e => e.BaggageId);

            entity.ToTable("Baggage");

            entity.Property(e => e.BaggageId).HasColumnName("baggage_id");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.Weight)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("weight");

            entity.HasOne(d => d.Ticket).WithMany(p => p.Baggages)
                .HasForeignKey(d => d.TicketId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId);

            entity.HasIndex(e => e.UserId, "idx_booking_user");

            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.BookingDate)
                .HasColumnType("datetime")
                .HasColumnName("booking_date");
            entity.Property(e => e.BookingType)
                .HasMaxLength(20)
                .HasColumnName("booking_type");
            entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Schedule).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.User).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<BookingPromotion>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.BookingId, e.PromoId }, "uq_booking_promo").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.PromoId).HasColumnName("promo_id");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingPromotions)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Promo).WithMany(p => p.BookingPromotions)
                .HasForeignKey(d => d.PromoId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Cities>(entity =>
        {
            entity.HasKey(e => e.CityId);

            entity.Property(e => e.CityId).HasColumnName("city_id");
            entity.Property(e => e.CityName)
                .HasMaxLength(100)
                .HasColumnName("city_name");
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .HasColumnName("country");
        });

        modelBuilder.Entity<Flight>(entity =>
        {
            entity.HasKey(e => e.FlightId);

            entity.HasIndex(e => e.FlightNumber).IsUnique();

            entity.Property(e => e.FlightId).HasColumnName("flight_id");
            entity.Property(e => e.FlightNumber)
                .HasMaxLength(20)
                .HasColumnName("flight_number");
            entity.Property(e => e.RouteId).HasColumnName("route_id");

            entity.HasOne(d => d.Route).WithMany(p => p.Flights)
                .HasForeignKey(d => d.RouteId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<FlightSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId);

            entity.HasIndex(e => e.DepartureTime, "idx_schedule_time");

            entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
            entity.Property(e => e.ArrivalTime)
                .HasColumnType("datetime")
                .HasColumnName("arrival_time");
            entity.Property(e => e.AvailableSeats).HasColumnName("available_seats");
            entity.Property(e => e.DepartureTime)
                .HasColumnType("datetime")
                .HasColumnName("departure_time");
            entity.Property(e => e.FlightId).HasColumnName("flight_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.TotalSeats).HasColumnName("total_seats");

            entity.HasOne(d => d.Flight).WithMany(p => p.FlightSchedules)
                .HasForeignKey(d => d.FlightId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Passenger>(entity =>
        {
            entity.HasKey(e => e.PassengerId);

            entity.Property(e => e.PassengerId).HasColumnName("passenger_id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.PassengerType)
                .HasMaxLength(20)
                .HasColumnName("passenger_type");

            entity.HasOne(d => d.Booking).WithMany(p => p.Passengers)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId);

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.PaymentDate)
                .HasColumnType("datetime")
                .HasColumnName("payment_date");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .HasColumnName("payment_status");
            entity.Property(e => e.TransactionNo)
                .HasMaxLength(100)
                .HasColumnName("transaction_no");

            entity.HasOne(d => d.Booking).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.PromoId);

            entity.HasIndex(e => e.PromoCode).IsUnique();

            entity.Property(e => e.PromoId).HasColumnName("promo_id");
            entity.Property(e => e.DiscountPercent).HasColumnName("discount_percent");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.PromoCode)
                .HasMaxLength(50)
                .HasColumnName("promo_code");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
        });

        modelBuilder.Entity<Route>(entity =>
        {
            entity.HasKey(e => e.RouteId);

            entity.Property(e => e.RouteId).HasColumnName("route_id");
            entity.Property(e => e.ArrivalCity).HasColumnName("arrival_city");
            entity.Property(e => e.DepartureCity).HasColumnName("departure_city");

            entity.HasOne(d => d.ArrivalCityNavigation).WithMany(p => p.RouteArrivalCityNavigations)
                .HasForeignKey(d => d.ArrivalCity)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.DepartureCityNavigation).WithMany(p => p.RouteDepartureCityNavigations)
                .HasForeignKey(d => d.DepartureCity)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.ToTable("Seats");

            entity.HasKey(e => e.SeatId);

            entity.HasIndex(e => new { e.ScheduleId, e.SeatNumber }, "UQ_Seats_Schedule_SeatNumber")
                .IsUnique();

            entity.Property(e => e.SeatId).HasColumnName("seat_id");
            entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.SeatNumber)
                .HasMaxLength(10)
                .HasColumnName("seat_number");
            entity.Property(e => e.SeatStatus)
                .HasMaxLength(20)
                .HasDefaultValue("AVAILABLE")
                .HasColumnName("seat_status");

            entity.HasOne(d => d.Schedule).WithMany(p => p.Seats)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Class).WithMany(p => p.Seats)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.TicketId);

            entity.HasIndex(e => e.SeatId, "UX_Tickets_SeatId")
                .IsUnique()
                .HasFilter("[seat_id] IS NOT NULL");

            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.PassengerId).HasColumnName("passenger_id");
            entity.Property(e => e.SeatId).HasColumnName("seat_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("BOOKED")
                .HasColumnName("status");

            entity.HasOne(d => d.Booking).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Class).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Passenger).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.PassengerId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Seat).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.SeatId);
        });

        modelBuilder.Entity<TicketClass>(entity =>
        {
            entity.HasKey(e => e.ClassId);

            entity.HasIndex(e => e.ClassName).IsUnique();

            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.ClassName)
                .HasMaxLength(50)
                .HasColumnName("class_name");
        });

        modelBuilder.Entity<TicketPrice>(entity =>
        {
            entity.HasKey(e => e.PriceId);

            entity.HasIndex(e => new { e.ScheduleId, e.ClassId }, "uq_schedule_class").IsUnique();

            entity.Property(e => e.PriceId).HasColumnName("price_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");

            entity.HasOne(d => d.Class).WithMany(p => p.TicketPrices)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Schedule).WithMany(p => p.TicketPrices)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.HasIndex(e => e.Cccd).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.Age).HasColumnName("age");
            entity.Property(e => e.Cccd)
                .HasMaxLength(20)
                .HasColumnName("cccd");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .HasColumnName("first_name");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .HasColumnName("gender");
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .HasColumnName("last_name");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .HasColumnName("phone");
            entity.Property(e => e.Role)
                .HasMaxLength(10)
                .HasColumnName("role");
            entity.Property(e => e.SkyMiles).HasColumnName("sky_miles");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");
        });

        SeedData(modelBuilder);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // -------------------------
        // CITIES
        // -------------------------
        modelBuilder.Entity<Cities>().HasData(
            new Cities { CityId = 1, CityName = "Ho Chi Minh City", Country = "Vietnam" },
            new Cities { CityId = 2, CityName = "Hanoi", Country = "Vietnam" },
            new Cities { CityId = 3, CityName = "Da Nang", Country = "Vietnam" },
            new Cities { CityId = 4, CityName = "Singapore", Country = "Singapore" },
            new Cities { CityId = 5, CityName = "Bangkok", Country = "Thailand" }
        );

        // -------------------------
        // ROUTES
        // -------------------------
        modelBuilder.Entity<Route>().HasData(
            new Route { RouteId = 1, DepartureCity = 1, ArrivalCity = 2 },
            new Route { RouteId = 2, DepartureCity = 2, ArrivalCity = 3 },
            new Route { RouteId = 3, DepartureCity = 1, ArrivalCity = 4 },
            new Route { RouteId = 4, DepartureCity = 4, ArrivalCity = 5 },
            new Route { RouteId = 5, DepartureCity = 3, ArrivalCity = 1 }
        );

        // -------------------------
        // FLIGHTS
        // flight_number unique
        // -------------------------
        modelBuilder.Entity<Flight>().HasData(
            new Flight { FlightId = 1, FlightNumber = "VN1001", RouteId = 1 },
            new Flight { FlightId = 2, FlightNumber = "VN1002", RouteId = 2 },
            new Flight { FlightId = 3, FlightNumber = "VN2001", RouteId = 3 },
            new Flight { FlightId = 4, FlightNumber = "VN3001", RouteId = 4 },
            new Flight { FlightId = 5, FlightNumber = "VN4001", RouteId = 5 }
        );

        // -------------------------
        // FLIGHT SCHEDULES
        // -------------------------
        modelBuilder.Entity<FlightSchedule>().HasData(
            new FlightSchedule
            {
                ScheduleId = 1,
                FlightId = 1,
                DepartureTime = new DateTime(2026, 04, 01, 8, 0, 0),
                ArrivalTime = new DateTime(2026, 04, 01, 10, 0, 0),
                TotalSeats = 180,
                AvailableSeats = 180,
                Status = "SCHEDULED"
            },
            new FlightSchedule
            {
                ScheduleId = 2,
                FlightId = 2,
                DepartureTime = new DateTime(2026, 04, 02, 9, 30, 0),
                ArrivalTime = new DateTime(2026, 04, 02, 11, 0, 0),
                TotalSeats = 180,
                AvailableSeats = 180,
                Status = "SCHEDULED"
            },
            new FlightSchedule
            {
                ScheduleId = 3,
                FlightId = 3,
                DepartureTime = new DateTime(2026, 04, 03, 13, 0, 0),
                ArrivalTime = new DateTime(2026, 04, 03, 16, 30, 0),
                TotalSeats = 180,
                AvailableSeats = 180,
                Status = "SCHEDULED"
            },
            new FlightSchedule
            {
                ScheduleId = 4,
                FlightId = 4,
                DepartureTime = new DateTime(2026, 04, 04, 7, 15, 0),
                ArrivalTime = new DateTime(2026, 04, 04, 8, 45, 0),
                TotalSeats = 180,
                AvailableSeats = 180,
                Status = "SCHEDULED"
            },
            new FlightSchedule
            {
                ScheduleId = 5,
                FlightId = 5,
                DepartureTime = new DateTime(2026, 04, 05, 18, 0, 0),
                ArrivalTime = new DateTime(2026, 04, 05, 19, 30, 0),
                TotalSeats = 180,
                AvailableSeats = 180,
                Status = "SCHEDULED"
            }
        );

        // -------------------------
        // USERS
        // email, username, cccd đều unique
        // -------------------------
        modelBuilder.Entity<User>().HasData(
            new User
            {
                UserId = 1,
                FirstName = "Nguyen",
                LastName = "An",
                Username = "nguyenan",
                Email = "nguyenan@example.com",
                Password = "123456",
                Phone = "0900000001",
                Gender = "Male",
                Age = 25,
                Address = "Ho Chi Minh City",
                Role = "USER",
                SkyMiles = 1200,
                Cccd = "079201000001",
                CreatedAt = new DateTime(2026, 03, 01, 8, 0, 0)
            },
            new User
            {
                UserId = 2,
                FirstName = "Tran",
                LastName = "Binh",
                Username = "tranbinh",
                Email = "tranbinh@example.com",
                Password = "123456",
                Phone = "0900000002",
                Gender = "Male",
                Age = 30,
                Address = "Hanoi",
                Role = "USER",
                SkyMiles = 850,
                Cccd = "079201000002",
                CreatedAt = new DateTime(2026, 03, 01, 8, 5, 0)
            },
            new User
            {
                UserId = 3,
                FirstName = "Le",
                LastName = "Chi",
                Username = "lechi",
                Email = "lechi@example.com",
                Password = "123456",
                Phone = "0900000003",
                Gender = "Female",
                Age = 27,
                Address = "Da Nang",
                Role = "USER",
                SkyMiles = 600,
                Cccd = "079201000003",
                CreatedAt = new DateTime(2026, 03, 01, 8, 10, 0)
            },
            new User
            {
                UserId = 4,
                FirstName = "Pham",
                LastName = "Dung",
                Username = "phamdung",
                Email = "phamdung@example.com",
                Password = "123456",
                Phone = "0900000004",
                Gender = "Female",
                Age = 29,
                Address = "Singapore",
                Role = "USER",
                SkyMiles = 1500,
                Cccd = "079201000004",
                CreatedAt = new DateTime(2026, 03, 01, 8, 15, 0)
            },
            new User
            {
                UserId = 5,
                FirstName = "Vo",
                LastName = "Em",
                Username = "voem",
                Email = "voem@example.com",
                Password = "123456",
                Phone = "0900000005",
                Gender = "Male",
                Age = 35,
                Address = "Bangkok",
                Role = "ADMIN",
                SkyMiles = 3000,
                Cccd = "079201000005",
                CreatedAt = new DateTime(2026, 03, 01, 8, 20, 0)
            }
        );

        // -------------------------
        // TICKET CLASSES
        // chỉ 4 hạng theo yêu cầu
        // class_name unique
        // -------------------------
        modelBuilder.Entity<TicketClass>().HasData(
            new TicketClass { ClassId = 1, ClassName = "Economy" },
            new TicketClass { ClassId = 2, ClassName = "Premium Economy" },
            new TicketClass { ClassId = 3, ClassName = "Business" },
            new TicketClass { ClassId = 4, ClassName = "First" }
        );

        // -------------------------
        // TICKET PRICES
        // unique(schedule_id, class_id)
        // Mỗi schedule seed 1 class để tránh dài quá và vẫn đúng ràng buộc
        // -------------------------
        modelBuilder.Entity<TicketPrice>().HasData(
            new TicketPrice { PriceId = 1, ScheduleId = 1, ClassId = 1, Price = 1200000m },
            new TicketPrice { PriceId = 2, ScheduleId = 2, ClassId = 2, Price = 1800000m },
            new TicketPrice { PriceId = 3, ScheduleId = 3, ClassId = 3, Price = 4500000m },
            new TicketPrice { PriceId = 4, ScheduleId = 4, ClassId = 4, Price = 6500000m },
            new TicketPrice { PriceId = 5, ScheduleId = 5, ClassId = 1, Price = 1500000m }
        );

        // -------------------------
        // PROMOTIONS
        // promo_code unique
        // -------------------------
        modelBuilder.Entity<Promotion>().HasData(
            new Promotion
            {
                PromoId = 1,
                PromoCode = "NEWUSER10",
                DiscountPercent = 10,
                StartDate = new DateOnly(2026, 04, 01),
                EndDate = new DateOnly(2026, 04, 30)
            },
            new Promotion
            {
                PromoId = 2,
                PromoCode = "SUMMER15",
                DiscountPercent = 15,
                StartDate = new DateOnly(2026, 05, 01),
                EndDate = new DateOnly(2026, 05, 31)
            },
            new Promotion
            {
                PromoId = 3,
                PromoCode = "VIP20",
                DiscountPercent = 20,
                StartDate = new DateOnly(2026, 04, 10),
                EndDate = new DateOnly(2026, 06, 10)
            },
            new Promotion
            {
                PromoId = 4,
                PromoCode = "HOLIDAY12",
                DiscountPercent = 12,
                StartDate = new DateOnly(2026, 06, 01),
                EndDate = new DateOnly(2026, 06, 30)
            },
            new Promotion
            {
                PromoId = 5,
                PromoCode = "FLASH8",
                DiscountPercent = 8,
                StartDate = new DateOnly(2026, 04, 15),
                EndDate = new DateOnly(2026, 04, 20)
            }
        );

        // -------------------------
        // BOOKINGS
        // -------------------------
        modelBuilder.Entity<Booking>().HasData(
            new Booking
            {
                BookingId = 1,
                UserId = 1,
                ScheduleId = 1,
                BookingDate = new DateTime(2026, 03, 20, 10, 0, 0),
                BookingType = "ONEWAY",
                Status = "ACTIVE"
            },
            new Booking
            {
                BookingId = 2,
                UserId = 2,
                ScheduleId = 2,
                BookingDate = new DateTime(2026, 03, 20, 10, 15, 0),
                BookingType = "ROUNDTRIP",
                Status = "ACTIVE"
            },
            new Booking
            {
                BookingId = 3,
                UserId = 3,
                ScheduleId = 3,
                BookingDate = new DateTime(2026, 03, 20, 10, 30, 0),
                BookingType = "ONEWAY",
                Status = "ACTIVE"
            },
            new Booking
            {
                BookingId = 4,
                UserId = 4,
                ScheduleId = 4,
                BookingDate = new DateTime(2026, 03, 20, 10, 45, 0),
                BookingType = "ONEWAY",
                Status = "ACTIVE"
            },
            new Booking
            {
                BookingId = 5,
                UserId = 5,
                ScheduleId = 5,
                BookingDate = new DateTime(2026, 03, 20, 11, 0, 0),
                BookingType = "ROUNDTRIP",
                Status = "ACTIVE"
            }
        );

        // -------------------------
        // PASSENGERS
        // -------------------------
        modelBuilder.Entity<Passenger>().HasData(
            new Passenger { PassengerId = 1, BookingId = 1, FullName = "Nguyen Van An", PassengerType = "ADULT" },
            new Passenger { PassengerId = 2, BookingId = 2, FullName = "Tran Van Binh", PassengerType = "ADULT" },
            new Passenger { PassengerId = 3, BookingId = 3, FullName = "Le Thi Chi", PassengerType = "ADULT" },
            new Passenger { PassengerId = 4, BookingId = 4, FullName = "Pham Thi Dung", PassengerType = "ADULT" },
            new Passenger { PassengerId = 5, BookingId = 5, FullName = "Vo Van Em", PassengerType = "ADULT" }
        );

        // -------------------------
        // SEATS
        // unique(schedule_id, seat_number)
        // mỗi ticket dùng 1 seat riêng
        // -------------------------
        modelBuilder.Entity<Seat>().HasData(
            new Seat { SeatId = 1, ScheduleId = 1, ClassId = 1, SeatNumber = "A01", SeatStatus = "BOOKED" },
            new Seat { SeatId = 2, ScheduleId = 2, ClassId = 2, SeatNumber = "B01", SeatStatus = "BOOKED" },
            new Seat { SeatId = 3, ScheduleId = 3, ClassId = 3, SeatNumber = "C01", SeatStatus = "BOOKED" },
            new Seat { SeatId = 4, ScheduleId = 4, ClassId = 4, SeatNumber = "D01", SeatStatus = "BOOKED" },
            new Seat { SeatId = 5, ScheduleId = 5, ClassId = 1, SeatNumber = "A02", SeatStatus = "BOOKED" }
        );

        // -------------------------
        // TICKETS
        // seat_id unique
        // class_id nên khớp với seat.class_id để dữ liệu đẹp
        // -------------------------
        modelBuilder.Entity<Ticket>().HasData(
            new Ticket { TicketId = 1, BookingId = 1, PassengerId = 1, ClassId = 1, SeatId = 1, Status = "BOOKED" },
            new Ticket { TicketId = 2, BookingId = 2, PassengerId = 2, ClassId = 2, SeatId = 2, Status = "BOOKED" },
            new Ticket { TicketId = 3, BookingId = 3, PassengerId = 3, ClassId = 3, SeatId = 3, Status = "BOOKED" },
            new Ticket { TicketId = 4, BookingId = 4, PassengerId = 4, ClassId = 4, SeatId = 4, Status = "BOOKED" },
            new Ticket { TicketId = 5, BookingId = 5, PassengerId = 5, ClassId = 1, SeatId = 5, Status = "BOOKED" }
        );

        // -------------------------
        // BAGGAGE
        // ticket_id FK hợp lệ
        // -------------------------
        modelBuilder.Entity<Baggage>().HasData(
            new Baggage { BaggageId = 1, TicketId = 1, Weight = 20.00m, Price = 200000m },
            new Baggage { BaggageId = 2, TicketId = 2, Weight = 25.00m, Price = 250000m },
            new Baggage { BaggageId = 3, TicketId = 3, Weight = 30.00m, Price = 300000m },
            new Baggage { BaggageId = 4, TicketId = 4, Weight = 35.00m, Price = 350000m },
            new Baggage { BaggageId = 5, TicketId = 5, Weight = 15.00m, Price = 150000m }
        );

        // -------------------------
        // PAYMENTS
        // booking_id FK hợp lệ, transaction_no không trùng cho đẹp dữ liệu
        // -------------------------
        modelBuilder.Entity<Payment>().HasData(
            new Payment
            {
                PaymentId = 1,
                BookingId = 1,
                Amount = 1400000m,
                PaymentDate = new DateTime(2026, 03, 20, 10, 5, 0),
                PaymentMethod = "VNPAY",
                PaymentStatus = "PAID",
                TransactionNo = "TXN000001"
            },
            new Payment
            {
                PaymentId = 2,
                BookingId = 2,
                Amount = 2050000m,
                PaymentDate = new DateTime(2026, 03, 20, 10, 20, 0),
                PaymentMethod = "MOMO",
                PaymentStatus = "PAID",
                TransactionNo = "TXN000002"
            },
            new Payment
            {
                PaymentId = 3,
                BookingId = 3,
                Amount = 4800000m,
                PaymentDate = new DateTime(2026, 03, 20, 10, 35, 0),
                PaymentMethod = "CREDIT_CARD",
                PaymentStatus = "PAID",
                TransactionNo = "TXN000003"
            },
            new Payment
            {
                PaymentId = 4,
                BookingId = 4,
                Amount = 6850000m,
                PaymentDate = new DateTime(2026, 03, 20, 10, 50, 0),
                PaymentMethod = "BANKING",
                PaymentStatus = "PAID",
                TransactionNo = "TXN000004"
            },
            new Payment
            {
                PaymentId = 5,
                BookingId = 5,
                Amount = 1650000m,
                PaymentDate = new DateTime(2026, 03, 20, 11, 5, 0),
                PaymentMethod = "CASH",
                PaymentStatus = "PAID",
                TransactionNo = "TXN000005"
            }
        );

        // -------------------------
        // BOOKING PROMOTIONS
        // unique(booking_id, promo_id)
        // -------------------------
        modelBuilder.Entity<BookingPromotion>().HasData(
            new BookingPromotion { Id = 1, BookingId = 1, PromoId = 1 },
            new BookingPromotion { Id = 2, BookingId = 2, PromoId = 2 },
            new BookingPromotion { Id = 3, BookingId = 3, PromoId = 3 },
            new BookingPromotion { Id = 4, BookingId = 4, PromoId = 4 },
            new BookingPromotion { Id = 5, BookingId = 5, PromoId = 5 }
        );
    }
}