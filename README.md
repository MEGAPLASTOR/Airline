# SkyWave Airlines Reservation System

## Overview

The **Airline** project is a modern flight reservation and management system built on .NET 8/9. It provides a complete set of features for both customers and administrators, with a polished, professional UI designed for an optimal user experience.

## Project Structure

- `Airline/`: Main ASP.NET Core MVC application.
  - `Controllers/`: Request handling and routing logic.
    - `AdminBookingController.cs`: Manual payment confirmation for bookings.
    - `BookingController.cs`: Customer booking flow.
    - `Accountcontroller.cs`: Login/Registration and cookie-based authentication.
    - `PaymentController.cs`: VNPay integration and transaction reconciliation (TransactionNo).
    - `AdminDashboardController.cs`: Admin overview and statistics.
    - `AdminAccountController.cs`: User management and role assignment.
    - `AdminCityController.cs`: Manage departure/arrival cities.
    - `AdminFlightController.cs`: Flight technical data management.
    - `AdminRouteController.cs`: Manage flight routes and itineraries.
    - `AdminScheduleController.cs`: Schedule and seat map coordination.
    - `AdminBaseController.cs`: Base controller for admin access control.
    - `TicketPriceController.cs`: Manage dynamic ticket pricing per schedule.
    - `ManageTicketClassController.cs`: Manage ticket classes (Business/Economy).
    - `TicketController.cs`: Display ticket details and handle seat changes.
    - `BaggageController.cs`: Register checked baggage services.
    - `ProfileController.cs`: Edit passenger profile and password.
    - `HomeController.cs`, `AboutController.cs`, `ContactController.cs`: Public informational pages.
  - `Services/`:
    - `SeatService.cs`: Logic to auto-generate 180 seats (30 rows) and allocate cabin sections.
  - `Models/`: Database entities and view models.
    - `BookingViewModel.cs`: DTO used during the booking workflow.
    - `Payment.cs`: Stores successful payments including `TransactionNo`.
    - ... (18 entities and view models supporting core business logic such as User, Flight, Ticket, etc.)
  - `Views/`: Razor views (.cshtml).
    - `About/Index.cshtml`: Company/about page for SkyWave Airlines.
    - `Account/ChangePassword.cshtml`: Secure change-password UI.
    - `Account/EditAccount.cshtml`: Edit passenger profile UI.
    - `Admin/AdminDashboard.cshtml`: Admin analytics dashboard.
    - `Admin/ConfirmTicket.cshtml`: Manual booking/payment approval UI.
    - `Admin/FlightReschedule.cshtml`: Flight reschedule interface.
    - `Admin/FlightSchedules.cshtml`: Manage flight schedules list.
    - `Admin/FlightSeats.cshtml`: Dynamic seat map with open/close controls.
    - `Admin/ManageAccounts.cshtml`: Manage users and roles.
    - `Admin/ManageCity.cshtml`: Manage cities data.
    - `Admin/ManageFlights.cshtml`: Manage flight technical data.
    - `Admin/ManageRoutes.cshtml`: Manage routes and itineraries.
    - `Admin/TicketClasses.cshtml`: Configure ticket classes (Business, Economy, etc.).
    - `Admin/TicketPrice.cshtml`: Dynamic ticket pricing per schedule.
    - `Baggage/RegisterForm.cshtml`: Checked baggage registration form.
    - `Baggage/SelectTicket.cshtml`: Choose ticket to attach baggage.
    - `Booking/BookFlight.cshtml`: Flight search and selection page.
    - `Booking/BookingSuccess.cshtml`: Booking completion / confirmation page.
    - `Booking/PassengerInfo.cshtml`: Passenger details form.
    - `Booking/SelectSeat.cshtml`: Seat selection map for customers.
    - `Contact/Index.cshtml`: Contact and support request page.
    - `Home/Index.cshtml`: Modern home page with flight search filters.
    - `Home/Privacy.cshtml`: Privacy policy and terms.
    - `Payment/PaymentResult.cshtml`: Show result returned by VNPay.
    - `Shared/Error.cshtml`: Centralized error page.
    - `Shared/_AdminLayout.cshtml`: Admin area layout.
    - `Shared/_Layout.cshtml`: Customer-facing main layout (dark theme).
    - `Shared/_ValidationScriptsPartial.cshtml`: Client-side validation helper.
    - `Ticket/ChangeSeat.cshtml`: Allow customers to change seats post-booking.
    - `Ticket/ViewConfirmation.cshtml`: View booked ticket details and confirmation code.
    - `_ViewImports.cshtml`, `_ViewStart.cshtml`: Razor configuration files.
  - `DataContext.cs`: Entity Framework Core configuration.
  - `Program.cs`: Services and pipeline configuration.
- `Airline.slnx`: Solution file.
- `Payment.md`: Payment documentation (VNPay & manual flows).
- `VNpay.md`: VNPay sandbox and configuration guide.
- `VNpayProblem.md`: Notes about IPN on localhost.
- `booking.md`: Customer booking workflow.

## Key Features

### For Customers

- Registration & Login: Secure cookie-based authentication.
- Account Management: Edit profile and change password.
- SkyMiles: Loyalty points system.
- Search & Booking: A streamlined 4-step booking flow with an English UI for a professional experience.
- 180-seat map: High-density seat map (30 rows) with smooth scrolling and live pricing updates.
- Online Payments: VNPay (sandbox) integration for fast payments.
- Ticket Management: View ticket confirmations and perform seat changes.

### For Administrators

- Manual Booking Confirmation: Admins can approve manual payments.
- Premium Dashboard: Charts and system overview.
- Full Management: Users, Cities, Routes, Flights, and Schedules.
- Dynamic Seat Map: Open/close seats directly on a visual map.
- Auto-Generate Seats: Automatically create 180 seats per schedule based on pricing.
- Flexible Pricing: Configure prices per class and schedule.

## Technology Stack

- Backend: .NET 8/9, ASP.NET Core MVC.
- Database: SQL Server, EF Core.
- Frontend: Razor Views, Vanilla CSS/JS, JetBrains Mono & Playfair Display fonts.
- Authentication: Cookie-based authentication.

## Running the Project

Initialize and apply migrations: `dotnet ef database update`

1. Configure SQL Server connection in `appsettings.json`.
2. Initialize the database by running migrations or executing the provided SQL scripts.
3. Configure VNPay sandbox settings in the application configuration.
4. Run the app: `dotnet run --project Airline`.

## Changelog & Recent Updates

### 2026-03-30: Booking, Authentication & Payment fixes

- `AccountController`: Added `ClaimTypes.NameIdentifier` containing `UserId` to the cookie sign-in claims.
- `BookingController`: Fixed form validation issues and automatic ticket-class generation.
- `Payment System`: Added `TransactionNo`, updated logic to record VNPay/manual transaction identifiers, and added Ngrok support.

### 2026-03-31: 180-seat map & English UI

- `SeatService`: Implemented auto-generation of 180 seats (30 rows) with dynamic cabin allocation based on `TicketPrice`.
- English UI: Booking flow views (`BookFlight`, `SelectSeat`, `PassengerInfo`, `BookingSuccess`) and seat management UI are English.
- UI Optimization: Improved seat map scrolling area to render 30 rows smoothly.
- Data Integrity: Switched to a relational `Seat` model to prevent overbooking.

---

