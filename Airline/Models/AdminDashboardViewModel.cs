using System.Collections.Generic;

namespace Airline.Models
{
    public class AdminDashboardViewModel
    {
        public string FirstName { get; set; } = "Admin";

        public int TotalUsers { get; set; }
        public int TotalFlights { get; set; }
        public int ActiveBookings { get; set; }
        public decimal RevenueThisMonth { get; set; }

        public int ScheduledFlightsToday { get; set; }
        public int AvailableSeatsToday { get; set; }

        public List<TicketClassChartItem> TicketClassDistribution { get; set; } = new();
        public List<TopRouteItem> TopRoutes { get; set; } = new();
        public List<RecentTicketActivityItem> RecentTickets { get; set; } = new();
    }

    public class TicketClassChartItem
    {
        public string ClassName { get; set; } = "";
        public int Count { get; set; }
    }

    public class TopRouteItem
    {
        public string RouteName { get; set; } = "";
        public int PassengerCount { get; set; }
    }

    public class RecentTicketActivityItem
    {
        public int TicketId { get; set; }
        public string PassengerName { get; set; } = "";
        public string RouteName { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string Status { get; set; } = "";
    }
}