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

// Add authentication and authorization services
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Cookies";
})
.AddCookie("Cookies", options =>
{
    options.LoginPath = "/Admin/Account/Login";
    options.LogoutPath = "/Admin/Account/Logout";
    options.AccessDeniedPath = "/Admin/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "UserType" && c.Value == UserType.Admin.ToString())));
});

var app = builder.Build();

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

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
    
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Check if database exists before trying migrations
        bool dbExists = context.Database.CanConnect();
        logger.LogInformation($"Database exists: {dbExists}");
        
        if (dbExists)
        {
            try
            {
                // If database exists, try to update schema without full migrations
                // This is safer than running full migrations when tables already exist
                logger.LogInformation("Database exists, ensuring schema is up to date...");
                context.Database.EnsureCreated();
                logger.LogInformation("Database schema updated successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while updating database schema. This is not critical if database is already set up.");
            }
        }
        else
        {
            // If database doesn't exist, create it with all tables
            logger.LogInformation("Database does not exist, creating new database...");
            context.Database.EnsureCreated();
            logger.LogInformation("Database created successfully.");
        }
        
        // Check if we can connect to the database
        if (context.Database.CanConnect())
        {
            logger.LogInformation("Connected to database successfully, proceeding with data seeding if needed.");
            
            // Helper method to hash passwords - same as in AccountController
            string HashPassword(string password)
            {
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                    var hashedPassword = Convert.ToBase64String(hashedBytes);
                    Console.WriteLine($"DEBUG - Original password: {password}");
                    Console.WriteLine($"DEBUG - Hashed password: {hashedPassword}");
                    return hashedPassword;
                }
            }
            
            // Check if admin user exists, if not create one
            if (!context.Users.Any(u => u.UserType == UserType.Admin))
            {
                logger.LogInformation("Creating admin user...");
                
                // Create a test user with PLAIN TEXT password for testing
                var testUser = new User
                {
                    FullName = "Test Admin",
                    Email = "test@test.com",
                    Username = "test",
                    Password = "test123", // Plain text for testing
                    PhoneNumber = "123-456-7890",
                    UserType = UserType.Admin,
                    CreatedAt = DateTime.Now
                };
                
                context.Users.Add(testUser);
                context.SaveChanges();
                
                logger.LogInformation($"Test admin created with Username: 'test' and Password: 'test123'");
                
                // Create the regular admin user with hashed password
                var adminPassword = "Admin123!";
                var hashedPassword = HashPassword(adminPassword);
                
                context.Users.Add(new User
                {
                    FullName = "Admin User",
                    Email = "admin@royalstay.com",
                    Username = "admin",
                    Password = hashedPassword,
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
            
            // Seed contact form submissions
            try
            {
                SeedData.Initialize(services);
                logger.LogInformation("Contact form submissions seeded successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding contact form submissions.");
            }
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

app.Run();
