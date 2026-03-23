using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Airline.Migrations
{
    /// <inheritdoc />
    public partial class FirstMigration : Migration
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
                    table.PrimaryKey("PK__Cities__031491A8108F3ABA", x => x.city_id);
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
                    table.PrimaryKey("PK__Promotio__84EB4CA580D9F7A0", x => x.promo_id);
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
                    table.PrimaryKey("PK__TicketCl__FDF47986BADBED47", x => x.class_id);
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
                    role = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USER"),
                    sky_miles = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    cccd = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Users__B9BE370F99B4C109", x => x.user_id);
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
                    table.PrimaryKey("PK__Routes__28F706FE14AF89ED", x => x.route_id);
                    table.ForeignKey(
                        name: "fk_route_arr",
                        column: x => x.arrival_city,
                        principalTable: "Cities",
                        principalColumn: "city_id");
                    table.ForeignKey(
                        name: "fk_route_dep",
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
                    table.PrimaryKey("PK__Flights__E3705765D5618590", x => x.flight_id);
                    table.ForeignKey(
                        name: "FK__Flights__route_i__6E01572D",
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
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "SCHEDULED")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__FlightSc__C46A8A6F561D2411", x => x.schedule_id);
                    table.ForeignKey(
                        name: "FK__FlightSch__fligh__74AE54BC",
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
                    booking_date = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "ACTIVE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Bookings__5DE3A5B1B40D84DC", x => x.booking_id);
                    table.ForeignKey(
                        name: "FK__Bookings__schedu__04E4BC85",
                        column: x => x.schedule_id,
                        principalTable: "FlightSchedules",
                        principalColumn: "schedule_id");
                    table.ForeignKey(
                        name: "FK__Bookings__user_i__03F0984C",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
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
                    table.PrimaryKey("PK__TicketPr__1681726D46F72854", x => x.price_id);
                    table.ForeignKey(
                        name: "FK__TicketPri__class__7D439ABD",
                        column: x => x.class_id,
                        principalTable: "TicketClasses",
                        principalColumn: "class_id");
                    table.ForeignKey(
                        name: "FK__TicketPri__sched__7C4F7684",
                        column: x => x.schedule_id,
                        principalTable: "FlightSchedules",
                        principalColumn: "schedule_id");
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
                    table.PrimaryKey("PK__BookingP__3213E83F12FA2D24", x => x.id);
                    table.ForeignKey(
                        name: "FK__BookingPr__booki__1BC821DD",
                        column: x => x.booking_id,
                        principalTable: "Bookings",
                        principalColumn: "booking_id");
                    table.ForeignKey(
                        name: "FK__BookingPr__promo__1CBC4616",
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
                    table.PrimaryKey("PK__Passenge__037645860F400AFE", x => x.passenger_id);
                    table.ForeignKey(
                        name: "FK__Passenger__booki__08B54D69",
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
                    payment_date = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Payments__ED1FC9EA224FCDD7", x => x.payment_id);
                    table.ForeignKey(
                        name: "FK__Payments__bookin__14270015",
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
                    seat_number = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "BOOKED")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Tickets__D596F96BA4DFBACA", x => x.ticket_id);
                    table.ForeignKey(
                        name: "FK__Tickets__booking__0D7A0286",
                        column: x => x.booking_id,
                        principalTable: "Bookings",
                        principalColumn: "booking_id");
                    table.ForeignKey(
                        name: "FK__Tickets__class_i__0F624AF8",
                        column: x => x.class_id,
                        principalTable: "TicketClasses",
                        principalColumn: "class_id");
                    table.ForeignKey(
                        name: "FK__Tickets__passeng__0E6E26BF",
                        column: x => x.passenger_id,
                        principalTable: "Passengers",
                        principalColumn: "passenger_id");
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
                    table.PrimaryKey("PK__Baggage__A3ADEABD204A8216", x => x.baggage_id);
                    table.ForeignKey(
                        name: "FK__Baggage__ticket___1F98B2C1",
                        column: x => x.ticket_id,
                        principalTable: "Tickets",
                        principalColumn: "ticket_id");
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
                name: "idx_flight_number",
                table: "Flights",
                column: "flight_number");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_route_id",
                table: "Flights",
                column: "route_id");

            migrationBuilder.CreateIndex(
                name: "UQ__Flights__340D78BBA1E73FB5",
                table: "Flights",
                column: "flight_number",
                unique: true);

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
                name: "UQ__Promotio__C07E231504162AE1",
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
                name: "UQ__TicketCl__7DC4C39DD30DA225",
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
                name: "UQ__Users__37D42BFAF79CEB57",
                table: "Users",
                column: "cccd",
                unique: true,
                filter: "[cccd] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ__Users__AB6E616469AA4722",
                table: "Users",
                column: "email",
                unique: true,
                filter: "[email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ__Users__F3DBC572D8273813",
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
                name: "TicketClasses");

            migrationBuilder.DropTable(
                name: "Passengers");

            migrationBuilder.DropTable(
                name: "Bookings");

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
