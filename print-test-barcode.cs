using System;
using System.IO;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ParkIRC.Services;
using ZXing;
using ZXing.QrCode;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using ZXing.Common;

class BarcodeTest
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Printer Test with Barcode");
        Console.WriteLine("=========================");

        // Ambil service provider dari application
        var services = CreateServices();
        var printerService = services.GetRequiredService<IPrinterService>();
        var logger = services.GetRequiredService<ILogger<BarcodeTest>>();

        try
        {
            // Test printer status
            bool status = await printerService.CheckPrinterStatusAsync();
            Console.WriteLine($"Printer status: {(status ? "Connected" : "Disconnected")}");

            if (status)
            {
                string testTicketNumber = "TEST-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                DateTime entryTime = DateTime.Now;
                
                // Path ke barcode image yang sudah ada atau akan dibuat
                string barcodeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", "barcodes");
                Directory.CreateDirectory(barcodeDir);
                string barcodeImagePath = Path.Combine(barcodeDir, $"{testTicketNumber}.png");
                
                // Generate barcode
                Console.WriteLine($"Generating barcode for ticket: {testTicketNumber}");
                GenerateBarcode(testTicketNumber, barcodeImagePath);
                
                // Print ticket with barcode
                Console.WriteLine("Printing ticket with barcode...");
                bool printResult = await printerService.PrintTicketWithBarcode(testTicketNumber, entryTime, barcodeImagePath);
                Console.WriteLine($"Print result: {(printResult ? "Success" : "Failed")}");
                
                // Test direct printing
                Console.WriteLine("Testing direct print...");
                bool testResult = await printerService.TestPrinter();
                Console.WriteLine($"Test print result: {(testResult ? "Success" : "Failed")}");
            }
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
        
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
    
    private static void GenerateBarcode(string content, string outputPath)
    {
        try
        {
            // Buat writer untuk QR Code
            var barcodeWriter = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = 300,
                    Height = 100, 
                    Margin = 5,
                    PureBarcode = false
                }
            };
            
            // Generate barcode
            var pixelData = barcodeWriter.Write(content);
            
            // Convert to image
            using (var image = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(
                pixelData.Pixels, 
                pixelData.Width, 
                pixelData.Height))
            {
                // Save as PNG
                using var stream = new FileStream(outputPath, FileMode.Create);
                image.Save(stream, new PngEncoder());
            }
            
            Console.WriteLine($"Barcode successfully generated at: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating barcode: {ex.Message}");
            throw;
        }
    }

    private static IServiceProvider CreateServices()
    {
        // Setup configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        // Setup DI
        var services = new ServiceCollection()
            .AddLogging(configure => configure.AddConsole())
            .AddSingleton<IConfiguration>(configuration);

        // Register our services
        services.AddScoped<IPrinterService, PrinterService>();

        return services.BuildServiceProvider();
    }
} 