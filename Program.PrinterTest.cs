using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Services;
using ParkIRC.Data;
using System.IO;

namespace ParkIRC.Tests
{
    public class PrinterTester
    {
        public static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Printer Test Utility");
                Console.WriteLine("====================");
                
                // Build configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();
                
                // Setup services manually
                var services = new ServiceCollection();
                services.AddLogging(configure => configure.AddConsole());
                services.AddSingleton<IConfiguration>(configuration);
                services.AddScoped<IPrinterService, PrinterService>();
                services.AddDbContext<ParkIRC.Data.ApplicationDbContext>(options => {
                    var connStr = configuration.GetConnectionString("DefaultConnection");
                    options.UseNpgsql(connStr);
                });
                
                var serviceProvider = services.BuildServiceProvider();
                
                // Create scope to properly dispose services
                using (var scope = serviceProvider.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var printerService = scopedServices.GetRequiredService<IPrinterService>();
                    var logger = scopedServices.GetRequiredService<ILogger<PrinterTester>>();
                    
                    Console.WriteLine("Initializing printer...");
                    try {
                        await printerService.InitializeAsync();
                        Console.WriteLine("Initialization successful");
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"Failed to initialize: {ex.Message}");
                    }
                    
                    Console.WriteLine("Checking printer status...");
                    bool status = await printerService.CheckPrinterStatusAsync();
                    Console.WriteLine($"Printer status: {(status ? "Connected" : "Disconnected")}");
                    
                    if (status)
                    {
                        Console.WriteLine("Printing test ticket...");
                        bool printResult = await printerService.PrintTicket("TEST-123456", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                        Console.WriteLine($"Print result: {(printResult ? "Success" : "Failed")}");
                        
                        // Also test alternative printing method
                        Console.WriteLine("Testing direct print...");
                        bool testResult = await printerService.TestPrinter();
                        Console.WriteLine($"Test print result: {(testResult ? "Success" : "Failed")}");
                    }
                }
                
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner error: {ex.InnerException.Message}");
                }
            }
        }
    }
} 