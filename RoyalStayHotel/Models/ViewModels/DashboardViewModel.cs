using RoyalStayHotel.Models;
using System.Collections.Generic;

namespace RoyalStayHotel.Models
{
    public class DashboardViewModel
    {
        public string AdminName { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public int AvailableRooms { get; set; }
        public int PendingMaintenance { get; set; }
        public int PendingCheckIns { get; set; }
        public int PendingPayments { get; set; }
        public List<Booking> RecentBookings { get; set; } = new();
        public List<Payment> RecentPayments { get; set; } = new();
        public List<MaintenanceRequest> MaintenanceRequests { get; set; } = new();
    }
} 