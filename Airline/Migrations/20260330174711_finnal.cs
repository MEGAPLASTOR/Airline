using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Airline.Migrations
{
    /// <inheritdoc />
    public partial class finnal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    city_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    city_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.city_id);
                });

            migrationBuilder.CreateTable(
                name: "Promotions",
                columns: table => new
                {
                    promo_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    promo_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    discount_percent = table.Column<int>(type: "int", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotions", x => x.promo_id);
                });

            migrationBuilder.CreateTable(
                name: "TicketClasses",
                columns: table => new
                {
                    class_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    class_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketClasses", x => x.class_id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    last_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    age = table.Column<int>(type: "int", nullable: true),
                    role = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    sky_miles = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    cccd = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    route_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    departure_city = table.Column<int>(type: "int", nullable: false),
                    arrival_city = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.route_id);
                    table.ForeignKey(
                        name: "FK_Routes_Cities_arrival_city",
                        column: x => x.arrival_city,
                        principalTable: "Cities",
                        principalColumn: "city_id");
                    table.ForeignKey(
                        name: "FK_Routes_Cities_departure_city",
                        column: x => x.departure_city,
                        principalTable: "Cities",
                        principalColumn: "city_id");
                });

            migrationBuilder.CreateTable(
                name: "Flights",
                columns: table => new
                {
                    flight_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    flight_number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    route_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flights", x => x.flight_id);
                    table.ForeignKey(
                        name: "FK_Flights_Routes_route_id",
                        column: x => x.route_id,
                        principalTable: "Routes",
                        principalColumn: "route_id");
                });

            migrationBuilder.CreateTable(
                name: "FlightSchedules",
                columns: table => new
                {
                    schedule_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    flight_id = table.Column<int>(type: "int", nullable: false),
                    departure_time = table.Column<DateTime>(type: "datetime", nullable: false),
                    arrival_time = table.Column<DateTime>(type: "datetime", nullable: false),
                    total_seats = table.Column<int>(type: "int", nullable: true),
                    available_seats = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightSchedules", x => x.schedule_id);
                    table.ForeignKey(
                        name: "FK_FlightSchedules_Flights_flight_id",
                        column: x => x.flight_id,
                        principalTable: "Flights",
                        principalColumn: "flight_id");
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    booking_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    schedule_id = table.Column<int>(type: "int", nullable: false),
                    booking_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    booking_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.booking_id);
                    table.ForeignKey(
                        name: "FK_Bookings_FlightSchedules_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "FlightSchedules",
                        principalColumn: "schedule_id");
                    table.ForeignKey(
                        name: "FK_Bookings_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Seats",
                columns: table => new
                {
                    seat_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    schedule_id = table.Column<int>(type: "int", nullable: false),
                    class_id = table.Column<int>(type: "int", nullable: false),
                    seat_number = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    seat_status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "AVAILABLE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seats", x => x.seat_id);
                    table.ForeignKey(
                        name: "FK_Seats_FlightSchedules_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "FlightSchedules",
                        principalColumn: "schedule_id");
                    table.ForeignKey(
                        name: "FK_Seats_TicketClasses_class_id",
                        column: x => x.class_id,
                        principalTable: "TicketClasses",
                        principalColumn: "class_id");
                });

            migrationBuilder.CreateTable(
                name: "TicketPrices",
                columns: table => new
                {
                    price_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    schedule_id = table.Column<int>(type: "int", nullable: false),
                    class_id = table.Column<int>(type: "int", nullable: false),
                    price = table.Column<decimal>(type: "decimal(10,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketPrices", x => x.price_id);
                    table.ForeignKey(
                        name: "FK_TicketPrices_FlightSchedules_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "FlightSchedules",
                        principalColumn: "schedule_id");
                    table.ForeignKey(
                        name: "FK_TicketPrices_TicketClasses_class_id",
                        column: x => x.class_id,
                        principalTable: "TicketClasses",
                        principalColumn: "class_id");
                });

            migrationBuilder.CreateTable(
                name: "BookingPromotions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    booking_id = table.Column<int>(type: "int", nullable: false),
                    promo_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingPromotions", x => x.id);
                    table.ForeignKey(
                        name: "FK_BookingPromotions_Bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "Bookings",
                        principalColumn: "booking_id");
                    table.ForeignKey(
                        name: "FK_BookingPromotions_Promotions_promo_id",
                        column: x => x.promo_id,
                        principalTable: "Promotions",
                        principalColumn: "promo_id");
                });

            migrationBuilder.CreateTable(
                name: "Passengers",
                columns: table => new
                {
                    passenger_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    booking_id = table.Column<int>(type: "int", nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    passenger_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Passengers", x => x.passenger_id);
                    table.ForeignKey(
                        name: "FK_Passengers_Bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "Bookings",
                        principalColumn: "booking_id");
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    payment_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    booking_id = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    payment_method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    payment_status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    payment_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    transaction_no = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.payment_id);
                    table.ForeignKey(
                        name: "FK_Payments_Bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "Bookings",
                        principalColumn: "booking_id");
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    ticket_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    booking_id = table.Column<int>(type: "int", nullable: false),
                    passenger_id = table.Column<int>(type: "int", nullable: false),
                    class_id = table.Column<int>(type: "int", nullable: false),
                    seat_id = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "BOOKED")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.ticket_id);
                    table.ForeignKey(
                        name: "FK_Tickets_Bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "Bookings",
                        principalColumn: "booking_id");
                    table.ForeignKey(
                        name: "FK_Tickets_Passengers_passenger_id",
                        column: x => x.passenger_id,
                        principalTable: "Passengers",
                        principalColumn: "passenger_id");
                    table.ForeignKey(
                        name: "FK_Tickets_Seats_seat_id",
                        column: x => x.seat_id,
                        principalTable: "Seats",
                        principalColumn: "seat_id");
                    table.ForeignKey(
                        name: "FK_Tickets_TicketClasses_class_id",
                        column: x => x.class_id,
                        principalTable: "TicketClasses",
                        principalColumn: "class_id");
                });

            migrationBuilder.CreateTable(
                name: "Baggage",
                columns: table => new
                {
                    baggage_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ticket_id = table.Column<int>(type: "int", nullable: false),
                    weight = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    price = table.Column<decimal>(type: "decimal(10,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Baggage", x => x.baggage_id);
                    table.ForeignKey(
                        name: "FK_Baggage_Tickets_ticket_id",
                        column: x => x.ticket_id,
                        principalTable: "Tickets",
                        principalColumn: "ticket_id");
                });

            migrationBuilder.InsertData(
                table: "Cities",
                columns: new[] { "city_id", "city_name", "country" },
                values: new object[,]
                {
                    { 1, "Ho Chi Minh City", "Vietnam" },
                    { 2, "Hanoi", "Vietnam" },
                    { 3, "Da Nang", "Vietnam" },
                    { 4, "Singapore", "Singapore" },
                    { 5, "Bangkok", "Thailand" }
                });

            migrationBuilder.InsertData(
                table: "Promotions",
                columns: new[] { "promo_id", "discount_percent", "end_date", "promo_code", "start_date" },
                values: new object[,]
                {
                    { 1, 10, new DateOnly(2026, 4, 30), "NEWUSER10", new DateOnly(2026, 4, 1) },
                    { 2, 15, new DateOnly(2026, 5, 31), "SUMMER15", new DateOnly(2026, 5, 1) },
                    { 3, 20, new DateOnly(2026, 6, 10), "VIP20", new DateOnly(2026, 4, 10) },
                    { 4, 12, new DateOnly(2026, 6, 30), "HOLIDAY12", new DateOnly(2026, 6, 1) },
                    { 5, 8, new DateOnly(2026, 4, 20), "FLASH8", new DateOnly(2026, 4, 15) }
                });

            migrationBuilder.InsertData(
                table: "TicketClasses",
                columns: new[] { "class_id", "class_name" },
                values: new object[,]
                {
                    { 1, "Economy" },
                    { 2, "Premium Economy" },
                    { 3, "Business" },
                    { 4, "First" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "user_id", "address", "age", "cccd", "created_at", "email", "first_name", "gender", "last_name", "password", "phone", "role", "sky_miles", "username" },
                values: new object[,]
                {
                    { 1, "Ho Chi Minh City", 25, "079201000001", new DateTime(2026, 3, 1, 8, 0, 0, 0, DateTimeKind.Unspecified), "nguyenan@example.com", "Nguyen", "Male", "An", "123456", "0900000001", "USER", 1200, "nguyenan" },
                    { 2, "Hanoi", 30, "079201000002", new DateTime(2026, 3, 1, 8, 5, 0, 0, DateTimeKind.Unspecified), "tranbinh@example.com", "Tran", "Male", "Binh", "123456", "0900000002", "USER", 850, "tranbinh" },
                    { 3, "Da Nang", 27, "079201000003", new DateTime(2026, 3, 1, 8, 10, 0, 0, DateTimeKind.Unspecified), "lechi@example.com", "Le", "Female", "Chi", "123456", "0900000003", "USER", 600, "lechi" },
                    { 4, "Singapore", 29, "079201000004", new DateTime(2026, 3, 1, 8, 15, 0, 0, DateTimeKind.Unspecified), "phamdung@example.com", "Pham", "Female", "Dung", "123456", "0900000004", "USER", 1500, "phamdung" },
                    { 5, "Bangkok", 35, "079201000005", new DateTime(2026, 3, 1, 8, 20, 0, 0, DateTimeKind.Unspecified), "voem@example.com", "Vo", "Male", "Em", "123456", "0900000005", "ADMIN", 3000, "voem" }
                });

            migrationBuilder.InsertData(
                table: "Routes",
                columns: new[] { "route_id", "arrival_city", "departure_city" },
                values: new object[,]
                {
                    { 1, 2, 1 },
                    { 2, 3, 2 },
                    { 3, 4, 1 },
                    { 4, 5, 4 },
                    { 5, 1, 3 }
                });

            migrationBuilder.InsertData(
                table: "Flights",
                columns: new[] { "flight_id", "flight_number", "route_id" },
                values: new object[,]
                {
                    { 1, "VN1001", 1 },
                    { 2, "VN1002", 2 },
                    { 3, "VN2001", 3 },
                    { 4, "VN3001", 4 },
                    { 5, "VN4001", 5 }
                });

            migrationBuilder.InsertData(
                table: "FlightSchedules",
                columns: new[] { "schedule_id", "arrival_time", "available_seats", "departure_time", "flight_id", "status", "total_seats" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 4, 1, 10, 0, 0, 0, DateTimeKind.Unspecified), 39, new DateTime(2026, 4, 1, 8, 0, 0, 0, DateTimeKind.Unspecified), 1, "SCHEDULED", 40 },
                    { 2, new DateTime(2026, 4, 2, 11, 0, 0, 0, DateTimeKind.Unspecified), 39, new DateTime(2026, 4, 2, 9, 30, 0, 0, DateTimeKind.Unspecified), 2, "SCHEDULED", 40 },
                    { 3, new DateTime(2026, 4, 3, 16, 30, 0, 0, DateTimeKind.Unspecified), 39, new DateTime(2026, 4, 3, 13, 0, 0, 0, DateTimeKind.Unspecified), 3, "SCHEDULED", 40 },
                    { 4, new DateTime(2026, 4, 4, 8, 45, 0, 0, DateTimeKind.Unspecified), 39, new DateTime(2026, 4, 4, 7, 15, 0, 0, DateTimeKind.Unspecified), 4, "SCHEDULED", 40 },
                    { 5, new DateTime(2026, 4, 5, 19, 30, 0, 0, DateTimeKind.Unspecified), 39, new DateTime(2026, 4, 5, 18, 0, 0, 0, DateTimeKind.Unspecified), 5, "SCHEDULED", 40 }
                });

            migrationBuilder.InsertData(
                table: "Bookings",
                columns: new[] { "booking_id", "booking_date", "booking_type", "schedule_id", "status", "user_id" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 3, 20, 10, 0, 0, 0, DateTimeKind.Unspecified), "ONEWAY", 1, "ACTIVE", 1 },
                    { 2, new DateTime(2026, 3, 20, 10, 15, 0, 0, DateTimeKind.Unspecified), "ROUNDTRIP", 2, "ACTIVE", 2 },
                    { 3, new DateTime(2026, 3, 20, 10, 30, 0, 0, DateTimeKind.Unspecified), "ONEWAY", 3, "ACTIVE", 3 },
                    { 4, new DateTime(2026, 3, 20, 10, 45, 0, 0, DateTimeKind.Unspecified), "ONEWAY", 4, "ACTIVE", 4 },
                    { 5, new DateTime(2026, 3, 20, 11, 0, 0, 0, DateTimeKind.Unspecified), "ROUNDTRIP", 5, "ACTIVE", 5 }
                });

            migrationBuilder.InsertData(
                table: "Seats",
                columns: new[] { "seat_id", "class_id", "schedule_id", "seat_number", "seat_status" },
                values: new object[,]
                {
                    { 1, 1, 1, "A01", "BOOKED" },
                    { 2, 2, 2, "B01", "BOOKED" },
                    { 3, 3, 3, "C01", "BOOKED" },
                    { 4, 4, 4, "D01", "BOOKED" },
                    { 5, 1, 5, "A02", "BOOKED" }
                });

            migrationBuilder.InsertData(
                table: "TicketPrices",
                columns: new[] { "price_id", "class_id", "price", "schedule_id" },
                values: new object[,]
                {
                    { 1, 1, 1200000m, 1 },
                    { 2, 2, 1800000m, 2 },
                    { 3, 3, 4500000m, 3 },
                    { 4, 4, 6500000m, 4 },
                    { 5, 1, 1500000m, 5 }
                });

            migrationBuilder.InsertData(
                table: "BookingPromotions",
                columns: new[] { "id", "booking_id", "promo_id" },
                values: new object[,]
                {
                    { 1, 1, 1 },
                    { 2, 2, 2 },
                    { 3, 3, 3 },
                    { 4, 4, 4 },
                    { 5, 5, 5 }
                });

            migrationBuilder.InsertData(
                table: "Passengers",
                columns: new[] { "passenger_id", "booking_id", "full_name", "passenger_type" },
                values: new object[,]
                {
                    { 1, 1, "Nguyen Van An", "ADULT" },
                    { 2, 2, "Tran Van Binh", "ADULT" },
                    { 3, 3, "Le Thi Chi", "ADULT" },
                    { 4, 4, "Pham Thi Dung", "ADULT" },
                    { 5, 5, "Vo Van Em", "ADULT" }
                });

            migrationBuilder.InsertData(
                table: "Payments",
                columns: new[] { "payment_id", "amount", "booking_id", "payment_date", "payment_method", "payment_status", "transaction_no" },
                values: new object[,]
                {
                    { 1, 1400000m, 1, new DateTime(2026, 3, 20, 10, 5, 0, 0, DateTimeKind.Unspecified), "VNPAY", "PAID", "TXN000001" },
                    { 2, 2050000m, 2, new DateTime(2026, 3, 20, 10, 20, 0, 0, DateTimeKind.Unspecified), "MOMO", "PAID", "TXN000002" },
                    { 3, 4800000m, 3, new DateTime(2026, 3, 20, 10, 35, 0, 0, DateTimeKind.Unspecified), "CREDIT_CARD", "PAID", "TXN000003" },
                    { 4, 6850000m, 4, new DateTime(2026, 3, 20, 10, 50, 0, 0, DateTimeKind.Unspecified), "BANKING", "PAID", "TXN000004" },
                    { 5, 1650000m, 5, new DateTime(2026, 3, 20, 11, 5, 0, 0, DateTimeKind.Unspecified), "CASH", "PAID", "TXN000005" }
                });

            migrationBuilder.InsertData(
                table: "Tickets",
                columns: new[] { "ticket_id", "booking_id", "class_id", "passenger_id", "seat_id", "status" },
                values: new object[,]
                {
                    { 1, 1, 1, 1, 1, "BOOKED" },
                    { 2, 2, 2, 2, 2, "BOOKED" },
                    { 3, 3, 3, 3, 3, "BOOKED" },
                    { 4, 4, 4, 4, 4, "BOOKED" },
                    { 5, 5, 1, 5, 5, "BOOKED" }
                });

            migrationBuilder.InsertData(
                table: "Baggage",
                columns: new[] { "baggage_id", "price", "ticket_id", "weight" },
                values: new object[,]
                {
                    { 1, 200000m, 1, 20.00m },
                    { 2, 250000m, 2, 25.00m },
                    { 3, 300000m, 3, 30.00m },
                    { 4, 350000m, 4, 35.00m },
                    { 5, 150000m, 5, 15.00m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Baggage_ticket_id",
                table: "Baggage",
                column: "ticket_id");

            migrationBuilder.CreateIndex(
                name: "IX_BookingPromotions_promo_id",
                table: "BookingPromotions",
                column: "promo_id");

            migrationBuilder.CreateIndex(
                name: "uq_booking_promo",
                table: "BookingPromotions",
                columns: new[] { "booking_id", "promo_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_booking_user",
                table: "Bookings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_schedule_id",
                table: "Bookings",
                column: "schedule_id");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_flight_number",
                table: "Flights",
                column: "flight_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Flights_route_id",
                table: "Flights",
                column: "route_id");

            migrationBuilder.CreateIndex(
                name: "idx_schedule_time",
                table: "FlightSchedules",
                column: "departure_time");

            migrationBuilder.CreateIndex(
                name: "IX_FlightSchedules_flight_id",
                table: "FlightSchedules",
                column: "flight_id");

            migrationBuilder.CreateIndex(
                name: "IX_Passengers_booking_id",
                table: "Passengers",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_booking_id",
                table: "Payments",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_promo_code",
                table: "Promotions",
                column: "promo_code",
                unique: true,
                filter: "[promo_code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_arrival_city",
                table: "Routes",
                column: "arrival_city");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_departure_city",
                table: "Routes",
                column: "departure_city");

            migrationBuilder.CreateIndex(
                name: "IX_Seats_class_id",
                table: "Seats",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "UQ_Seats_Schedule_SeatNumber",
                table: "Seats",
                columns: new[] { "schedule_id", "seat_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketClasses_class_name",
                table: "TicketClasses",
                column: "class_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketPrices_class_id",
                table: "TicketPrices",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "uq_schedule_class",
                table: "TicketPrices",
                columns: new[] { "schedule_id", "class_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_booking_id",
                table: "Tickets",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_class_id",
                table: "Tickets",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_passenger_id",
                table: "Tickets",
                column: "passenger_id");

            migrationBuilder.CreateIndex(
                name: "UX_Tickets_SeatId",
                table: "Tickets",
                column: "seat_id",
                unique: true,
                filter: "[seat_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_cccd",
                table: "Users",
                column: "cccd",
                unique: true,
                filter: "[cccd] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_email",
                table: "Users",
                column: "email",
                unique: true,
                filter: "[email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_username",
                table: "Users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Baggage");

            migrationBuilder.DropTable(
                name: "BookingPromotions");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "TicketPrices");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "Promotions");

            migrationBuilder.DropTable(
                name: "Passengers");

            migrationBuilder.DropTable(
                name: "Seats");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "TicketClasses");

            migrationBuilder.DropTable(
                name: "FlightSchedules");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Flights");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "Cities");
        }
    }
}
