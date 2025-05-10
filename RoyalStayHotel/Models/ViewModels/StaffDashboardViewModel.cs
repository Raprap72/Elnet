using RoyalStayHotel.Models;

namespace RoyalStayHotel.Models.ViewModels
{
    public class StaffDashboardViewModel
    {
        public List<HousekeepingTask> HousekeepingTasks { get; set; } = new();
        public List<MaintenanceRequest> MaintenanceRequests { get; set; } = new();
        public List<Booking> TodayCheckIns { get; set; } = new();
        public List<Booking> TodayCheckOuts { get; set; } = new();
        public List<Notification> UrgentNotifications { get; set; } = new();
        public DashboardStats Stats { get; set; } = new();
    }

    public class DashboardStats
    {
        public int PendingHousekeepingTasks { get; set; }
        public int PendingMaintenanceRequests { get; set; }
        public int TotalCheckInsToday { get; set; }
        public int TotalCheckOutsToday { get; set; }
        public int AvailableRooms { get; set; }
    }
} 