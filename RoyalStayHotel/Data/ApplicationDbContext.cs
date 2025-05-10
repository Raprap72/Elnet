using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Models;
using System;

namespace RoyalStayHotel.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<User> Users { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<RoomTypeInventory> RoomTypeInventories { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<BookedService> BookedServices { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ContactFormSubmission> ContactFormSubmissions { get; set; }
        public DbSet<HotelService> HotelServices { get; set; }
        public DbSet<SiteSetting> SiteSettings { get; set; }
        public DbSet<Guest> Guests { get; set; }
        public DbSet<RoomTypeInfo> RoomTypeInfos { get; set; }
        public DbSet<HousekeepingTask> HousekeepingTasks { get; set; }
        public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure entity table names and keys
            modelBuilder.Entity<User>()
                .ToTable("Users")
                .HasKey(u => u.UserId);

            modelBuilder.Entity<Room>()
                .ToTable("Rooms")
                .HasKey(r => r.RoomId);

            modelBuilder.Entity<RoomTypeInventory>()
                .ToTable("RoomTypeInventories");
            
            // Configure Booking entity
            modelBuilder.Entity<Booking>()
                .ToTable("Bookings")
                .HasKey(b => b.BookingId);

            modelBuilder.Entity<Booking>()
                .Property(b => b.BookingReference)
                .IsRequired();

            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.BookingReference)
                .IsUnique();
            
            modelBuilder.Entity<Service>()
                .ToTable("Services")
                .HasKey(s => s.ServiceId);

            modelBuilder.Entity<BookedService>()
                .ToTable("BookedServices")
                .HasKey(bs => bs.Id);

            modelBuilder.Entity<Payment>()
                .ToTable("Payments")
                .HasKey(p => p.PaymentId);

            modelBuilder.Entity<Review>()
                .ToTable("Reviews");

            modelBuilder.Entity<ContactFormSubmission>()
                .ToTable("ContactFormSubmissions");

            modelBuilder.Entity<HotelService>()
                .ToTable("HotelServices")
                .HasKey(hs => hs.Id);

            modelBuilder.Entity<SiteSetting>()
                .ToTable("SiteSettings");

            modelBuilder.Entity<Guest>()
                .ToTable("Guests");

            modelBuilder.Entity<RoomTypeInfo>()
                .ToTable("RoomTypeInfos");

            modelBuilder.Entity<HousekeepingTask>()
                .ToTable("HousekeepingTasks");

            modelBuilder.Entity<MaintenanceRequest>()
                .ToTable("MaintenanceRequests");

            modelBuilder.Entity<Notification>()
                .ToTable("Notifications");

            modelBuilder.Entity<Discount>()
                .ToTable("Discounts");

            modelBuilder.Entity<UserActivityLog>()
                .ToTable("UserActivityLogs");
            
            // Configure relationships
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Room)
                .WithMany()
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BookedService>()
                .HasOne(bs => bs.Booking)
                .WithMany(b => b.BookedServices)
                .HasForeignKey(bs => bs.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookedService>()
                .HasOne(bs => bs.Service)
                .WithMany()
                .HasForeignKey(bs => bs.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure decimal precision
            modelBuilder.Entity<Booking>()
                .Property(b => b.TotalPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<BookedService>()
                .Property(bs => bs.TotalPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Room>()
                .Property(r => r.PricePerNight)
                .HasPrecision(18, 2);

            modelBuilder.Entity<HotelService>()
                .Property(s => s.Price)
                .HasPrecision(18, 2);

            // Configure unique indexes
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Room>()
                .HasIndex(r => r.RoomNumber)
                .IsUnique();

            // Seed initial data
            SeedInitialData(modelBuilder);
        }

        private void SeedInitialData(ModelBuilder modelBuilder)
        {
            // Seed admin user
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    FullName = "Admin User",
                    Email = "admin@royalstay.com",
                    Username = "admin",
                    Password = "Admin123!", // In production, use password hashing
                    PhoneNumber = "123-456-7890",
                    UserType = UserType.Admin,
                    CreatedAt = new DateTime(2023, 1, 1, 12, 0, 0)
                }
            );

            // Seed rooms
            modelBuilder.Entity<Room>().HasData(
                new Room
                {
                    RoomId = 1,
                    RoomNumber = "101",
                    RoomType = RoomType.Deluxe,
                    Description = "Experience luxury in our spacious Deluxe Room with modern amenities and elegant design.",
                    PricePerNight = 22628,
                    MaxGuests = 3,
                    BedType = "King",
                    RoomSize = "40 sq m",
                    AvailabilityStatus = AvailabilityStatus.Available,
                    ImageUrl = "/images/deluxe_room.png",
                    HasKingBed = true,
                    HasDoubleBeds = false
                },
                new Room
                {
                    RoomId = 2,
                    RoomNumber = "201",
                    RoomType = RoomType.DeluxeSuite,
                    Description = "Upgrade your stay with our Deluxe Suite featuring a separate living area and premium furnishings.",
                    PricePerNight = 33800,
                    MaxGuests = 4,
                    BedType = "Double",
                    RoomSize = "60 sq m",
                    AvailabilityStatus = AvailabilityStatus.Available,
                    ImageUrl = "/images/deluxe-suite_room.png",
                    HasKingBed = false,
                    HasDoubleBeds = true
                },
                new Room
                {
                    RoomId = 3,
                    RoomNumber = "301",
                    RoomType = RoomType.ExecutiveDeluxe,
                    Description = "Our Executive Deluxe Room offers the ultimate luxury experience with panoramic views and exclusive amenities.",
                    PricePerNight = 55500,
                    MaxGuests = 4,
                    BedType = "King and Double",
                    RoomSize = "80 sq m",
                    AvailabilityStatus = AvailabilityStatus.Available,
                    ImageUrl = "/images/executive-deluxe_room.png",
                    HasKingBed = true,
                    HasDoubleBeds = true
                }
            );

            // Seed hotel services
            modelBuilder.Entity<HotelService>().HasData(
                new HotelService
                {
                    Id = 1,
                    Name = "Room Cleaning",
                    Description = "Daily room cleaning service",
                    Price = 0,
                    IsAvailable = true,
                    ServiceType = ServiceType.AdditionalService
                },
                new HotelService
                {
                    Id = 2,
                    Name = "Breakfast Buffet",
                    Description = "Enjoy a lavish breakfast buffet with international and local cuisine",
                    Price = 1200,
                    IsAvailable = true,
                    ServiceType = ServiceType.AdditionalService
                },
                new HotelService
                {
                    Id = 3,
                    Name = "Gym Access",
                    Description = "24/7 access to our fully equipped fitness center",
                    Price = 500,
                    IsAvailable = true,
                    ServiceType = ServiceType.AdditionalService
                },
                new HotelService
                {
                    Id = 4,
                    Name = "Swimming Pool",
                    Description = "Access to our infinity pool with stunning views",
                    Price = 0,
                    IsAvailable = true,
                    ServiceType = ServiceType.AdditionalService
                }
            );
        }
    }
} 