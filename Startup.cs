using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ParkIRC.Models;
using ParkIRC.Services;
using ParkIRC.Services.Interfaces;
using ParkIRC.Hardware;
using ParkIRC.Hubs;
using ParkIRC.Data;

namespace ParkIRC
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Add database context
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection") ?? 
                    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));

            // Add Identity
            services.AddIdentity<Operator, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Configure Identity options
            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
            });

            // Add memory cache
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = Configuration.GetValue<int>("Cache:MaxItems", 1000);
            });

            // Add SignalR
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.MaximumReceiveMessageSize = 102400; // 100 KB
                options.StreamBufferCapacity = 10;
            });

            // Register services
            services.AddSingleton<IHardwareRedundancyService, HardwareRedundancyService>();
            services.AddSingleton<ICachingService, CachingService>();
            services.AddSingleton<ISecurityService, SecurityService>();
            services.AddSingleton<IRealTimeMonitoringService, RealTimeMonitoringService>();

            // Register hardware services
            services.AddSingleton<IHardwareManager>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<HardwareManager>>();
                var config = sp.GetRequiredService<IConfiguration>();
                var redundancyService = sp.GetRequiredService<IHardwareRedundancyService>();
                
                HardwareManager.Initialize(logger, config, redundancyService);
                return (IHardwareManager)HardwareManager.Instance;
            });

            // Add MVC
            services.AddControllersWithViews();

            // Add CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // Add authentication
            services.AddAuthentication()
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = Configuration["Jwt:Issuer"],
                        ValidAudience = Configuration["Jwt:Audience"],
                        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                            System.Text.Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
                    };

                    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            // Add authorization
            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Administrator"));
                options.AddPolicy("RequireOperatorRole", policy => policy.RequireRole("Operator"));
            });

            services.AddScoped<VehicleIntegrationService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors("AllowAll");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapHub<MonitoringHub>("/hubs/monitoring");
            });

            // Initialize services
            var serviceProvider = app.ApplicationServices;
            var monitoringService = serviceProvider.GetRequiredService<IRealTimeMonitoringService>();
            var logger = serviceProvider.GetRequiredService<ILogger<Startup>>();

            // Start monitoring in the background
            Task.Run(async () =>
            {
                try
                {
                    await monitoringService.StartMonitoringAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error starting monitoring service");
                }
            });
        }
    }
} 