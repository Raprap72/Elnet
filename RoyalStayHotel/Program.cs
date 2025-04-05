using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;

var builder = WebApplication.CreateBuilder(args);

// Add database context - prioritize LocalDB as SQL Express connection is failing
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Try to use the DefaultConnection (LocalDB) first as it's more likely to work
    string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        // Fall back to SQL Express if LocalDB connection string is missing
        connectionString = builder.Configuration.GetConnectionString("SQLExpressConnection");
    }
    
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("No valid connection string found in configuration.");
    }
    
    // Always include TrustServerCertificate=True to avoid SSL issues
    if (!connectionString.Contains("TrustServerCertificate=True"))
    {
        connectionString += ";TrustServerCertificate=True;";
    }
    
    options.UseSqlServer(connectionString);
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Delete and recreate database
        logger.LogInformation("Deleting and recreating database...");
        bool recreateSuccessful = false;
        
        try
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            recreateSuccessful = true;
            logger.LogInformation("Database recreated successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while recreating database. Trying alternate approach.");
            
            // If the above failed, try using migrations
            try
            {
                context.Database.Migrate();
                recreateSuccessful = true;
                logger.LogInformation("Database migrated successfully.");
            }
            catch (Exception migrationEx)
            {
                logger.LogError(migrationEx, "Error occurred during database migration. Will try to use existing database.");
            }
        }
        
        // Only seed if we have a valid database
        if (recreateSuccessful || context.Database.CanConnect())
        {
            logger.LogInformation("Seeding database...");
            
            // Check if admin user exists, if not create one
            if (!context.Users.Any(u => u.UserType == UserType.Admin))
            {
                logger.LogInformation("Creating admin user...");
                context.Users.Add(new User
                {
                    FullName = "Admin User",
                    Email = "admin@royalstay.com",
                    Username = "admin",
                    Password = "Admin123!", // In production, use a password hasher
                    PhoneNumber = "123-456-7890",
                    UserType = UserType.Admin,
                    CreatedAt = DateTime.Now
                });
                
                context.SaveChanges();
                logger.LogInformation("Admin user created successfully.");
            }
            
            // Seed rooms if none exist
            if (!context.Rooms.Any())
            {
                logger.LogInformation("Seeding rooms...");
                context.Rooms.AddRange(
                    new Room
                    {
                        RoomNumber = "101",
                        RoomType = RoomType.Deluxe,
                        Description = "Spacious room with city view",
                        PricePerNight = 150.00m,
                        MaxGuests = 2,
                        BedType = "King",
                        RoomSize = "30 sq m",
                        AvailabilityStatus = AvailabilityStatus.Available,
                        ImageUrl = "/images/deluxe_room.png",
                        HasKingBed = true,
                        HasDoubleBeds = false
                    },
                    new Room
                    {
                        RoomNumber = "102",
                        RoomType = RoomType.Standard,
                        Description = "Cozy room with garden view",
                        PricePerNight = 100.00m,
                        MaxGuests = 2,
                        BedType = "Queen",
                        RoomSize = "25 sq m",
                        AvailabilityStatus = AvailabilityStatus.Available,
                        ImageUrl = "/images/standard_room.png",
                        HasKingBed = false,
                        HasDoubleBeds = true
                    },
                    new Room
                    {
                        RoomNumber = "201",
                        RoomType = RoomType.DeluxeSuite,
                        Description = "Luxury suite with ocean view",
                        PricePerNight = 250.00m,
                        MaxGuests = 4,
                        BedType = "King + Sofa Bed",
                        RoomSize = "45 sq m",
                        AvailabilityStatus = AvailabilityStatus.Available,
                        ImageUrl = "/images/deluxe-suite_room.png",
                        HasKingBed = true,
                        HasDoubleBeds = true
                    }
                );
                
                context.SaveChanges();
                logger.LogInformation("Rooms seeded successfully.");
            }
            
            // Seed services if none exist
            if (!context.Services.Any())
            {
                logger.LogInformation("Seeding services...");
                context.Services.AddRange(
                    new Service
                    {
                        ServiceName = "Room Service",
                        Price = 20.00m,
                        Description = "24/7 room service"
                    },
                    new Service
                    {
                        ServiceName = "Spa Treatment",
                        Price = 80.00m,
                        Description = "1-hour massage"
                    },
                    new Service
                    {
                        ServiceName = "Airport Transfer",
                        Price = 50.00m,
                        Description = "One-way transfer"
                    }
                );
                
                context.SaveChanges();
                logger.LogInformation("Services seeded successfully.");
            }
            
            logger.LogInformation("Database seeding completed successfully.");
        }
        else
        {
            logger.LogWarning("Cannot connect to database. Skipping seeding.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Ensure static files are properly served
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
    
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
