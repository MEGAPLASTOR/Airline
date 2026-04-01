using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Airline.Migrations
{
    /// <inheritdoc />
    public partial class AddSkyMilesShop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "Promotions",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_skymiles_exclusive",
                table: "Promotions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "only_for_skymiles_payment",
                table: "Promotions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "sky_miles_cost",
                table: "Promotions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "title",
                table: "Promotions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserPromotions",
                columns: table => new
                {
                    user_promotion_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    promo_id = table.Column<int>(type: "int", nullable: false),
                    purchased_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    sky_miles_spent = table.Column<int>(type: "int", nullable: false),
                    is_redeemed = table.Column<bool>(type: "bit", nullable: false),
                    redeemed_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    redeemed_booking_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPromotions", x => x.user_promotion_id);
                    table.ForeignKey(
                        name: "FK_UserPromotions_Bookings_redeemed_booking_id",
                        column: x => x.redeemed_booking_id,
                        principalTable: "Bookings",
                        principalColumn: "booking_id");
                    table.ForeignKey(
                        name: "FK_UserPromotions_Promotions_promo_id",
                        column: x => x.promo_id,
                        principalTable: "Promotions",
                        principalColumn: "promo_id");
                    table.ForeignKey(
                        name: "FK_UserPromotions_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                });

            migrationBuilder.UpdateData(
                table: "FlightSchedules",
                keyColumn: "schedule_id",
                keyValue: 1,
                columns: new[] { "available_seats", "total_seats" },
                values: new object[] { 180, 180 });

            migrationBuilder.UpdateData(
                table: "FlightSchedules",
                keyColumn: "schedule_id",
                keyValue: 2,
                columns: new[] { "available_seats", "total_seats" },
                values: new object[] { 180, 180 });

            migrationBuilder.UpdateData(
                table: "FlightSchedules",
                keyColumn: "schedule_id",
                keyValue: 3,
                columns: new[] { "available_seats", "total_seats" },
                values: new object[] { 180, 180 });

            migrationBuilder.UpdateData(
                table: "FlightSchedules",
                keyColumn: "schedule_id",
                keyValue: 4,
                columns: new[] { "available_seats", "total_seats" },
                values: new object[] { 180, 180 });

            migrationBuilder.UpdateData(
                table: "FlightSchedules",
                keyColumn: "schedule_id",
                keyValue: 5,
                columns: new[] { "available_seats", "total_seats" },
                values: new object[] { 180, 180 });

            migrationBuilder.UpdateData(
                table: "Promotions",
                keyColumn: "promo_id",
                keyValue: 1,
                columns: new[] { "description", "is_skymiles_exclusive", "only_for_skymiles_payment", "sky_miles_cost", "title" },
                values: new object[] { "Starter discount for new customers paying with regular online methods.", false, false, 0, "Welcome Aboard" });

            migrationBuilder.UpdateData(
                table: "Promotions",
                keyColumn: "promo_id",
                keyValue: 2,
                columns: new[] { "description", "is_skymiles_exclusive", "only_for_skymiles_payment", "sky_miles_cost", "title" },
                values: new object[] { "Seasonal online promotion for summer departures.", false, false, 0, "Summer Escape" });

            migrationBuilder.UpdateData(
                table: "Promotions",
                keyColumn: "promo_id",
                keyValue: 3,
                columns: new[] { "description", "is_skymiles_exclusive", "only_for_skymiles_payment", "sky_miles_cost", "title" },
                values: new object[] { "A premium campaign code for high-value online bookings.", false, false, 0, "VIP Traveller" });

            migrationBuilder.UpdateData(
                table: "Promotions",
                keyColumn: "promo_id",
                keyValue: 4,
                columns: new[] { "description", "is_skymiles_exclusive", "only_for_skymiles_payment", "sky_miles_cost", "title" },
                values: new object[] { "Holiday booking discount for standard payments.", false, false, 0, "Holiday Special" });

            migrationBuilder.UpdateData(
                table: "Promotions",
                keyColumn: "promo_id",
                keyValue: 5,
                columns: new[] { "description", "is_skymiles_exclusive", "only_for_skymiles_payment", "sky_miles_cost", "title" },
                values: new object[] { "Short online flash sale for standard fare purchases.", false, false, 0, "Flash Sale" });

            migrationBuilder.InsertData(
                table: "Promotions",
                columns: new[] { "promo_id", "description", "discount_percent", "end_date", "is_skymiles_exclusive", "only_for_skymiles_payment", "promo_code", "sky_miles_cost", "start_date", "title" },
                values: new object[,]
                {
                    { 1001, "Redeem this shop code to reduce the SkyMiles cost of your next reward booking by 12 percent.", 12, new DateOnly(2026, 7, 31), true, true, "MILES12", 250, new DateOnly(2026, 4, 1), "Sky Saver 12" },
                    { 1002, "Higher-value reward code for customers redeeming SkyMiles on flights.", 18, new DateOnly(2026, 8, 15), true, true, "MILES18", 450, new DateOnly(2026, 4, 1), "Sky Saver 18" },
                    { 1003, "Premium reward-booking code that cuts the miles needed on your next SkyMiles purchase.", 25, new DateOnly(2026, 9, 1), true, true, "MILES25", 700, new DateOnly(2026, 4, 5), "Sky Saver 25" }
                });

            migrationBuilder.InsertData(
                table: "UserPromotions",
                columns: new[] { "user_promotion_id", "is_redeemed", "promo_id", "purchased_at", "redeemed_at", "redeemed_booking_id", "sky_miles_spent", "user_id" },
                values: new object[,]
                {
                    { 1, false, 1001, new DateTime(2026, 4, 1, 9, 0, 0, 0, DateTimeKind.Unspecified), null, null, 250, 1 },
                    { 2, false, 1002, new DateTime(2026, 4, 1, 9, 30, 0, 0, DateTimeKind.Unspecified), null, null, 450, 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPromotions_promo_id",
                table: "UserPromotions",
                column: "promo_id");

            migrationBuilder.CreateIndex(
                name: "IX_UserPromotions_redeemed_booking_id",
                table: "UserPromotions",
                column: "redeemed_booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_UserPromotions_user_promo",
                table: "UserPromotions",
                columns: new[] { "user_id", "promo_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPromotions");

            migrationBuilder.DeleteData(
                table: "Promotions",
                keyColumn: "promo_id",
                keyValue: 1001);

            migrationBuilder.DeleteData(
                table: "Promotions",
                keyColumn: "promo_id",
                keyValue: 1002);

            migrationBuilder.DeleteData(
                table: "Promotions",
                keyColumn: "promo_id",
                keyValue: 1003);

            migrationBuilder.DropColumn(
                name: "description",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "is_skymiles_exclusive",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "only_for_skymiles_payment",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "sky_miles_cost",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "title",
                table: "Promotions");

            migrationBuilder.UpdateData(
                table: "FlightSchedules",
                keyColumn: "schedule_id",
                keyValue: 1,
                columns: new[] { "available_seats", "total_seats" },
                values: new object[] { 39, 40 });

            migrationBuilder.UpdateData(
                table: "FlightSchedules",
                keyColumn: "schedule_id",
                keyValue: 2,
                columns: new[] { "available_seats", "total_seats" },
                values: new object[] { 39, 40 });

            migrationBuilder.UpdateData(
                table: "FlightSchedules",
                keyColumn: "schedule_id",
                keyValue: 3,
                columns: new[] { "available_seats", "total_seats" },
                values: new object[] { 39, 40 });

            migrationBuilder.UpdateData(
                table: "FlightSchedules",
                keyColumn: "schedule_id",
                keyValue: 4,
                columns: new[] { "available_seats", "total_seats" },
                values: new object[] { 39, 40 });

            migrationBuilder.UpdateData(
                table: "FlightSchedules",
                keyColumn: "schedule_id",
                keyValue: 5,
                columns: new[] { "available_seats", "total_seats" },
                values: new object[] { 39, 40 });
        }
    }
}
