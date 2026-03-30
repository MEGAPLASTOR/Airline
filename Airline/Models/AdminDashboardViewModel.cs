namespace Airline.Models;

public sealed class AdminDashboardViewModel
{
    public string AdminFirstName { get; init; } = "Admin";

    public int CurrentYear { get; init; }

    public AdminDashboardKpiViewModel TotalUsers { get; init; } = new();

    public AdminDashboardKpiViewModel RevenueThisMonth { get; init; } = new();

    public AdminDashboardKpiViewModel ActiveBookings { get; init; } = new();

    public AdminDashboardKpiViewModel FlightsToday { get; init; } = new();

    public string[] BookingChartLabels { get; init; } = [];

    public int[] BookingChartValues { get; init; } = [];

    public string[] RevenueChartLabels { get; init; } = [];

    public decimal[] RevenueChartValues { get; init; } = [];

    public AdminDashboardDistributionViewModel[] TicketClasses { get; init; } = [];

    public AdminDashboardRouteViewModel[] TopRoutes { get; init; } = [];

    public AdminDashboardRecentTicketViewModel[] RecentTickets { get; init; } = [];

    public AdminDashboardHealthViewModel[] OperationalMetrics { get; init; } = [];
}

public sealed class AdminDashboardKpiViewModel
{
    public string Value { get; init; } = "0";

    public string Label { get; init; } = "";

    public string TrendText { get; init; } = "0%";

    public string TrendDirection { get; init; } = "flat";

    public decimal[] Sparkline { get; init; } = [];
}

public sealed class AdminDashboardDistributionViewModel
{
    public string Label { get; init; } = "";

    public int Count { get; init; }

    public decimal Percentage { get; init; }
}

public sealed class AdminDashboardRouteViewModel
{
    public string Label { get; init; } = "";

    public int PassengerCount { get; init; }
}

public sealed class AdminDashboardRecentTicketViewModel
{
    public string TicketCode { get; init; } = "";

    public string PassengerName { get; init; } = "";

    public string RouteLabel { get; init; } = "";

    public string ClassName { get; init; } = "";

    public string StatusLabel { get; init; } = "";

    public string StatusCssClass { get; init; } = "pill-gold";
}

public sealed class AdminDashboardHealthViewModel
{
    public string Name { get; init; } = "";

    public int Percentage { get; init; }

    public string ValueText { get; init; } = "";

    public string CssClass { get; init; } = "";
}
