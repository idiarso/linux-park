using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Data;
using ParkIRC;
using Microsoft.Extensions.DependencyInjection;
using ParkIRC.Hubs;
using ParkIRC.Models;
using ParkIRC.Services;
using ParkIRC.Middleware;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Web;
using System.Runtime.CompilerServices;

// Configurar comportamiento legacy de timestamps para Npgsql
// Esto permite usar DateTime locales con PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Early init of NLog to allow startup and exception logging
var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add NLog with detailed configuration
    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    builder.Host.UseNLog();

    // Add database configuration
    builder.Services.AddDbContext<ApplicationDbContext>(options => {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(connectionString, npgsqlOptions => {
            npgsqlOptions.MigrationsAssembly(typeof(Program).Assembly.FullName);
        });
        
        // Enable detailed error messages in Development
        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
    });

    // Add health checks
    builder.Services.AddHealthChecks();

    // Add memory cache
    builder.Services.AddMemoryCache();

    // Configure services with proper exception handling
    builder.Services.AddScoped<IParkingService, ParkingService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
    builder.Services.AddScoped<IScannerService, ScannerService>();
    builder.Services.AddScoped<PrintService>();
    builder.Services.AddScoped<ConnectionStatusService>();
    builder.Services.AddScoped<IOfflineDataService, OfflineDataService>();
    builder.Services.AddIdentity<Operator, IdentityRole>(options => {
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
        
        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        
        // User settings
        options.User.RequireUniqueEmail = true;
    })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

    // Configure cookie settings
    builder.Services.ConfigureApplicationCookie(options => {
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.SlidingExpiration = true;
    });

    // Add SignalR support
    builder.Services.AddSignalR();

    // Add controller support with JSON options for AJAX requests
    builder.Services.AddControllersWithViews()
        .AddJsonOptions(options => {
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
        });

    // Add response caching
    builder.Services.AddResponseCaching();

    // Add printer service
    builder.Services.AddSingleton<IPrinterService, PrinterService>();

    // Add background service untuk cek koneksi
    builder.Services.AddHostedService<ConnectionMonitorService>();

    // Add Connection Status Service
    builder.Services.AddScoped<ConnectionStatusService>();
    builder.Services.AddHostedService<ConnectionStatusService>();

    // Add Print Service
    builder.Services.AddScoped<PrintService>();

    // Add Scheduled Backup Service
    builder.Services.AddHostedService<ScheduledBackupService>();

    var app = builder.Build();

    // Apply pending migrations automatically
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();
            logger.Info("Database created successfully");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "An error occurred while creating the database.");
        }
    }

    // Global error handler
    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unhandled exception occurred");
            throw;
        }
    });

    // Configure the HTTP request pipeline with better error handling
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    // Ensure database exists and can be connected
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Create database directory if it doesn't exist (no necesario para PostgreSQL, pero lo dejamos por si vuelven a SQLite)
            var dbPath = Path.GetDirectoryName(builder.Configuration.GetConnectionString("DefaultConnection").Replace("Data Source=", ""));
            if (!string.IsNullOrEmpty(dbPath) && !Directory.Exists(dbPath))
            {
                Directory.CreateDirectory(dbPath);
            }
            
            // Test database connection
            if (!context.Database.CanConnect())
            {
                logger.Error("Cannot connect to database");
                throw new Exception("Database connection failed");
            }

            // Initialize database with default data if empty (agregamos verificación específica para usuarios)
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Operator>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Create roles if they don't exist
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
                logger.Info("Admin role created");
            }
            if (!await roleManager.RoleExistsAsync("Staff"))
            {
                await roleManager.CreateAsync(new IdentityRole("Staff"));
                logger.Info("Staff role created");
            }

            // Create admin user if it doesn't exist
            if (await userManager.FindByEmailAsync("admin@parkingsystem.com") == null)
            {
                var adminUser = new Operator
                {
                    UserName = "admin@parkingsystem.com",
                    Email = "admin@parkingsystem.com",
                    FullName = "System Administrator",
                    Name = "System Administrator",
                    EmailConfirmed = true,
                    IsActive = true,
                    JoinDate = DateTime.Today,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    logger.Info("Admin user created successfully");
                }
                else
                {
                    logger.Error($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }

            // Initialize database with default data if empty
            if (!context.ParkingSpaces.Any())
            {
                // Add default parking spaces
                var parkingSpaces = new List<ParkingSpace>
                {
                    new ParkingSpace { SpaceNumber = "A1", SpaceType = "Standard", IsOccupied = false, HourlyRate = 5.00m },
                    new ParkingSpace { SpaceNumber = "A2", SpaceType = "Standard", IsOccupied = false, HourlyRate = 5.00m },
                    new ParkingSpace { SpaceNumber = "A3", SpaceType = "Standard", IsOccupied = false, HourlyRate = 5.00m },
                    new ParkingSpace { SpaceNumber = "B1", SpaceType = "Compact", IsOccupied = false, HourlyRate = 3.50m },
                };
                context.ParkingSpaces.AddRange(parkingSpaces);
                
                await context.SaveChangesAsync();
                logger.Info("Default parking spaces created");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Database initialization failed: {Message}", ex.Message);
            throw; // Re-throw to stop application startup
        }
    }

    // Configure HTTPS redirection
    app.UseHttpsRedirection();
    app.UseStaticFiles();

    // Add rate limiting middleware
    app.UseMiddleware<RateLimitingMiddleware>();

    // Add response caching middleware
    app.UseResponseCaching();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    // Configure endpoints
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Auth}/{action=Login}/{id?}");

    // Map SignalR hub
    app.MapHub<ParkingHub>("/parkingHub");

    // Add health checks
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Application stopped due to exception");
    throw;
}

// Helper method to generate transaction numbers
static string GenerateTransactionNumber()
{
    return "TRX-" + DateTime.Now.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
}
