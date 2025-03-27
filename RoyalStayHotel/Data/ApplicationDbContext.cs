using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Models;

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
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<BookedService> BookedServices { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure entity table names
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Room>().ToTable("Rooms");
            modelBuilder.Entity<Booking>().ToTable("Bookings");
            modelBuilder.Entity<Service>().ToTable("Services");
            modelBuilder.Entity<BookedService>().ToTable("BookedServices");
            modelBuilder.Entity<Payment>().ToTable("Payments");
            modelBuilder.Entity<Review>().ToTable("Reviews");
            
            // Configure relationships with appropriate cascade behavior
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany(u => u.Payments)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Configure decimal precision for Booking.TotalPrice
            modelBuilder.Entity<Booking>()
                .Property(b => b.TotalPrice)
                .HasPrecision(18, 2);
            
            // Configure decimal precision for BookedService.TotalPrice
            modelBuilder.Entity<BookedService>()
                .Property(bs => bs.TotalPrice)
                .HasPrecision(18, 2);
            
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
                    CreatedAt = DateTime.Now
                }
            );
            
            // Seed room types
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
            
            // Seed services
            modelBuilder.Entity<Service>().HasData(
                new Service
                {
                    ServiceId = 1,
                    ServiceName = "Airport Transfer",
                    Price = 2500,
                    Description = "Luxury transportation from the airport to the hotel"
                },
                new Service
                {
                    ServiceId = 2,
                    ServiceName = "Spa Treatment",
                    Price = 3500,
                    Description = "Relaxing full body massage and spa treatment"
                },
                new Service
                {
                    ServiceId = 3,
                    ServiceName = "Room Service",
                    Price = 500,
                    Description = "24/7 in-room dining service"
                },
                new Service
                {
                    ServiceId = 4,
                    ServiceName = "Laundry Service",
                    Price = 1000,
                    Description = "Same-day laundry and dry cleaning service"
                }
            );
        }
    }
} 