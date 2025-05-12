using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoyalStayHotel.Models;

namespace RoyalStayHotel.Data
{
    public static class SeedDiscounts
    {
        public static void InitializeDiscounts(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Check if we already have discount records
                if (context.Discounts.Any())
                {
                    return; // DB already has discounts
                }

                // Add sample discounts
                context.Discounts.AddRange(
                    new Discount
                    {
                        Name = "Summer Special",
                        Code = "SUMMER25",
                        Description = "25% off all room bookings for the summer season",
                        DiscountAmount = 25,
                        IsPercentage = true,
                        StartDate = DateTime.Now,
                        EndDate = DateTime.Now.AddMonths(3),
                        IsActive = true,
                        MinimumStay = 2,
                        Type = DiscountType.Seasonal
                    },
                    new Discount
                    {
                        Name = "Deluxe Suite Discount",
                        Code = "DELUXE50",
                        Description = "50% off Deluxe Suite bookings for first-time guests",
                        DiscountAmount = 50,
                        IsPercentage = true,
                        StartDate = DateTime.Now,
                        EndDate = DateTime.Now.AddMonths(6),
                        IsActive = true,
                        RoomTypeId = (int)RoomType.DeluxeSuite,
                        Type = DiscountType.RoomRate
                    },
                    new Discount
                    {
                        Name = "Weekend Package",
                        Code = "WEEKEND2000",
                        Description = "₱2000 off on weekend bookings (Friday-Sunday)",
                        DiscountAmount = 2000,
                        IsPercentage = false,
                        StartDate = DateTime.Now,
                        EndDate = DateTime.Now.AddMonths(12),
                        IsActive = true,
                        MinimumStay = 2,
                        Type = DiscountType.Package
                    },
                    new Discount
                    {
                        Name = "Loyalty Program",
                        Code = "LOYAL15",
                        Description = "15% discount for loyal customers",
                        DiscountAmount = 15,
                        IsPercentage = true,
                        StartDate = DateTime.Now,
                        EndDate = DateTime.Now.AddYears(1),
                        IsActive = true,
                        Type = DiscountType.Special
                    },
                    new Discount
                    {
                        Name = "Early Bird Booking",
                        Code = "EARLY20",
                        Description = "20% off for bookings made 30 days in advance",
                        DiscountAmount = 20,
                        IsPercentage = true,
                        StartDate = DateTime.Now,
                        EndDate = DateTime.Now.AddMonths(6),
                        IsActive = true,
                        Type = DiscountType.Special
                    }
                );

                context.SaveChanges();
            }
        }
    }
} 