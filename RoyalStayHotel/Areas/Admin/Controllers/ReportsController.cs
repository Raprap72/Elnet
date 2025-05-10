using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RoyalStayHotel.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get statistics for the dashboard
            var bookings = await _context.Bookings.ToListAsync();
            var payments = await _context.Payments.ToListAsync();
            var rooms = await _context.Rooms.ToListAsync();
            var guests = await _context.Guests.ToListAsync();
            
            // Calculate current occupancy rate
            int totalRooms = rooms.Count;
            int occupiedRooms = bookings.Count(b => 
                b.CheckInDate <= DateTime.Now && 
                b.CheckOutDate >= DateTime.Now && 
                b.Status != BookingStatus.Cancelled);
                
            decimal occupancyRate = totalRooms > 0 
                ? (decimal)occupiedRooms / totalRooms * 100 
                : 0;
            
            // Get monthly revenue for the current year
            var currentYear = DateTime.Now.Year;
            var monthlyRevenue = new Dictionary<string, decimal>();
            
            for (int i = 1; i <= 12; i++)
            {
                var monthName = new DateTime(currentYear, i, 1).ToString("MMM");
                var revenue = payments
                    .Where(p => p.PaymentDate.Year == currentYear && 
                           p.PaymentDate.Month == i &&
                           p.PaymentStatus == PaymentStatus.Completed)
                    .Sum(p => p.Amount);
                           
                monthlyRevenue.Add(monthName, revenue);
            }
            
            // Room type popularity
            var roomTypePopularity = await _context.Bookings
                .Where(b => b.Status != BookingStatus.Cancelled)
                .Include(b => b.Room)
                .GroupBy(b => b.Room.Name)
                .Select(g => new { RoomType = g.Key, Count = g.Count() })
                .ToListAsync();
                
            // Pass data to view
            ViewBag.TotalBookings = bookings.Count;
            ViewBag.TotalRevenue = payments
                .Where(p => p.PaymentStatus == PaymentStatus.Completed)
                .Sum(p => p.Amount);
            ViewBag.OccupancyRate = occupancyRate;
            ViewBag.TotalGuests = guests.Count;
            ViewBag.MonthlyRevenue = monthlyRevenue;
            ViewBag.RoomTypePopularity = roomTypePopularity;
            
            return View();
        }
        
        public async Task<IActionResult> BookingReport(DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue)
                startDate = DateTime.Now.AddMonths(-1);
                
            if (!endDate.HasValue)
                endDate = DateTime.Now;
                
            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
            
            var bookings = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.User)
                .Where(b => b.CreatedAt >= startDate && b.CreatedAt <= endDate)
                .OrderBy(b => b.CreatedAt)
                .ToListAsync();
                
            var bookingsByDate = bookings
                .GroupBy(b => b.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count(),
                    Revenue = g.Sum(b => b.TotalPrice)
                })
                .OrderBy(x => x.Date)
                .ToList();
            
            // Calculate statistics
            ViewBag.TotalBookings = bookings.Count;
            ViewBag.ConfirmedBookings = bookings.Count(b => b.Status == BookingStatus.Confirmed);
            ViewBag.CancelledBookings = bookings.Count(b => b.Status == BookingStatus.Cancelled);
            ViewBag.CompletedBookings = bookings.Count(b => b.Status == BookingStatus.CheckedOut);
            ViewBag.PendingBookings = bookings.Count(b => b.Status == BookingStatus.Pending);
            
            return View(bookings);
        }
        
        public async Task<IActionResult> RevenueReport(DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue)
                startDate = DateTime.Now.AddMonths(-1);
                
            if (!endDate.HasValue)
                endDate = DateTime.Now;
                
            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
            
            var payments = await _context.Payments
                .Include(p => p.Booking)
                .ThenInclude(b => b.User)
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
                
            // Calculate revenue by payment method
            var revenueByMethod = payments
                .Where(p => p.PaymentStatus == PaymentStatus.Completed)
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new { Method = g.Key, Total = g.Sum(p => p.Amount) })
                .ToList();
                
            // Calculate daily revenue for the date range
            var dailyRevenue = new Dictionary<string, decimal>();
            for (var date = startDate.Value; date <= endDate.Value; date = date.AddDays(1))
            {
                var dayRevenue = payments
                    .Where(p => p.PaymentDate.Date == date.Date && p.PaymentStatus == PaymentStatus.Completed)
                    .Sum(p => p.Amount);
                    
                dailyRevenue.Add(date.ToString("MM/dd"), dayRevenue);
            }
            
            ViewBag.TotalRevenue = payments
                .Where(p => p.PaymentStatus == PaymentStatus.Completed)
                .Sum(p => p.Amount);
            ViewBag.RevenueByMethod = revenueByMethod;
            ViewBag.DailyRevenue = dailyRevenue;
            
            return View(payments);
        }
        
        public async Task<IActionResult> OccupancyReport(DateTime? date)
        {
            if (!date.HasValue)
                date = DateTime.Now;
                
            ViewBag.Date = date.Value.ToString("yyyy-MM-dd");
            
            // Get all rooms
            var rooms = await _context.Rooms
                .ToListAsync();
                
            // Get bookings for the selected date
            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .Where(b => b.CheckInDate <= date && b.CheckOutDate >= date && b.Status != BookingStatus.Cancelled)
                .ToListAsync();
                
            // Calculate occupancy by room type
            var roomTypes = rooms.Select(r => r.RoomType).Distinct().ToList();
            var occupancyByType = new List<dynamic>();
            
            foreach (var type in roomTypes)
            {
                int totalRooms = rooms.Count(r => r.RoomType == type);
                int occupiedRooms = bookings.Count(b => b.Room != null && b.Room.RoomType == type);
                decimal occupancyRate = totalRooms > 0 ? (decimal)occupiedRooms / totalRooms * 100 : 0;
                
                occupancyByType.Add(new
                {
                    RoomType = type.ToString(),
                    TotalRooms = totalRooms,
                    OccupiedRooms = occupiedRooms,
                    OccupancyRate = occupancyRate
                });
            }
            
            // Overall occupancy
            int totalAllRooms = rooms.Count;
            int occupiedAllRooms = bookings.Count();
            decimal overallOccupancy = totalAllRooms > 0 ? (decimal)occupiedAllRooms / totalAllRooms * 100 : 0;
            
            ViewBag.OccupancyByType = occupancyByType;
            ViewBag.OverallOccupancy = overallOccupancy;
            ViewBag.Bookings = bookings;
            
            return View(rooms);
        }
    }
} 