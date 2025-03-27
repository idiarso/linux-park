using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using QRCoder;
using ParkIRC.Data;
using ParkIRC.Services.Interfaces;
using ParkIRC.Models;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ParkIRC.Hardware;

public class VehicleIntegrationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VehicleIntegrationService> _logger;
    private readonly ICameraService _cameraService;
    private readonly IPrinterService _printerService;
    private readonly IHardwareManager _hardwareManager;
    private int _currentTicketSequence = 0;
    private readonly object _lockObject = new object();

    public VehicleIntegrationService(
        ApplicationDbContext context,
        ILogger<VehicleIntegrationService> logger,
        ICameraService cameraService,
        IPrinterService printerService,
        IHardwareManager hardwareManager)
    {
        _context = context;
        _logger = logger;
        _cameraService = cameraService;
        _printerService = printerService;
        _hardwareManager = hardwareManager;
        
        // Subscribe to pushbutton event
        _hardwareManager.OnPushButtonPressed += HandlePushButtonPressed;
    }

    private async void HandlePushButtonPressed(object sender, PushButtonEventArgs e)
    {
        try
        {
            // 1. Generate ticket number dengan sequence
            var ticketNumber = GenerateSequentialTicketNumber();
            
            // 2. Capture image right after button press
            var imagePath = await CaptureAndSaveImageWithTicket(ticketNumber);
            
            // 3. Generate barcode untuk ticket
            var barcodeImage = GenerateBarcode(ticketNumber);
            var barcodeImagePath = await SaveBarcodeImage(barcodeImage, ticketNumber);
            
            // 4. Create vehicle record
            var vehicle = new Vehicle
            {
                TicketNumber = ticketNumber,
                EntryTime = DateTime.Now,
                EntryImagePath = imagePath,
                BarcodeImagePath = barcodeImagePath,
                IsParked = true,
                Status = "Active",
                CreatedBy = "GATE_IN_SYSTEM",
                EntryGateId = e.GateId ?? "GATE_1"
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            // 5. Print ticket dengan barcode
            await _printerService.PrintTicketWithBarcode(
                ticketNumber, 
                vehicle.EntryTime,
                barcodeImagePath
            );

            // 6. Open gate
            await _hardwareManager.OpenGate(e.GateId);

            _logger.LogInformation($"Vehicle entry processed - Ticket: {ticketNumber}, Gate: {e.GateId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing entry: {ex.Message}");
            // Jika terjadi error, tetap buka gate untuk menghindari kemacetan
            await _hardwareManager.OpenGate(e.GateId);
        }
    }

    private string GenerateSequentialTicketNumber()
    {
        lock (_lockObject)
        {
            _currentTicketSequence++;
            var date = DateTime.Now.ToString("yyyyMMdd");
            // Format: TKT-20240314-0001 (sequence 4 digit dengan leading zeros)
            return $"TKT-{date}-{_currentTicketSequence:D4}";
        }
    }

    private string GenerateBarcode(string ticketNumber)
    {
        var barcodeWriter = new BarcodeWriter<Bitmap>
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions
            {
                Width = 300,
                Height = 300,
                Margin = 1
            }
        };

        var barcodeImage = barcodeWriter.Write(ticketNumber);
        var imagePath = Path.Combine("barcodes", $"{ticketNumber}.png");
        Directory.CreateDirectory("barcodes");
        barcodeImage.Save(imagePath);
        return imagePath;
    }

    private async Task<string> SaveBarcodeImage(string barcodeImage, string ticketNumber)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = $"BARCODE_{ticketNumber}_{timestamp}.png";
            var imagePath = Path.Combine("wwwroot", "images", "barcodes");
            Directory.CreateDirectory(imagePath);
            
            var fullPath = Path.Combine(imagePath, filename);
            File.Copy(barcodeImage, fullPath);
            
            return Path.Combine("images", "barcodes", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error saving barcode image: {ex.Message}");
            throw;
        }
    }

    private async Task<string> CaptureAndSaveImageWithTicket(string ticketNumber)
    {
        try
        {
            // Tunggu sebentar untuk memastikan kendaraan sudah posisi ideal untuk foto
            await Task.Delay(1500); // Delay 1.5 detik

            // Capture image from camera
            var imageBytes = await _cameraService.CaptureImage();
            
            // Create filename with ticket number and timestamp
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = $"GATE_IN_{ticketNumber}_{timestamp}.jpg";
            
            // Ensure directory exists
            var imagePath = Path.Combine("wwwroot", "images", "vehicles", "entry");
            Directory.CreateDirectory(imagePath);
            
            // Save image with ticket number in filename
            var fullPath = Path.Combine(imagePath, filename);
            await File.WriteAllBytesAsync(fullPath, imageBytes);
            
            // Return relative path for database
            return Path.Combine("images", "vehicles", "entry", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error capturing/saving image: {ex.Message}");
            throw;
        }
    }

    private byte[] ImageToByte(System.Drawing.Image image)
    {
        using (var stream = new MemoryStream())
        {
            image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            return stream.ToArray();
        }
    }

    public void Dispose()
    {
        _hardwareManager.OnPushButtonPressed -= HandlePushButtonPressed;
    }

    public async Task<VehicleEntryResponse> ProcessVehicleEntry(VehicleEntryRequest request)
    {
        try
        {
            var vehicle = new Vehicle
            {
                PlateNumber = request.VehicleNumber,
                VehicleType = request.VehicleType,
                EntryTime = DateTime.Now,
                Status = "Active",
                EntryGateId = request.GateId
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            var ticketNumber = GenerateSequentialTicketNumber();
            var barcodeImage = GenerateBarcode(ticketNumber);
            var barcodeImagePath = await SaveBarcodeImage(barcodeImage, ticketNumber);

            var printSuccess = await _printerService.PrintTicketWithBarcode(
                ticketNumber, 
                vehicle.EntryTime,
                barcodeImagePath
            );

            await _hardwareManager.OpenGate(request.GateId);

            return new VehicleEntryResponse
            {
                Success = true,
                Message = "Vehicle entry processed successfully",
                TicketNumber = ticketNumber,
                ImagePath = barcodeImagePath,
                PrintStatus = printSuccess
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing vehicle entry: {ex.Message}");
            return new VehicleEntryResponse
            {
                Success = false,
                Message = $"Error processing vehicle entry: {ex.Message}"
            };
        }
    }
}

public class PushButtonEventArgs : EventArgs
{
    public string GateId { get; set; }
    public DateTime Timestamp { get; set; }
}

public class VehicleIntegrationResult
{
    public bool Success { get; set; }
    public int? VehicleId { get; set; }
    public string TicketNumber { get; set; }
    public string ImagePath { get; set; }
    public string Message { get; set; }
} 