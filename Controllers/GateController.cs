using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Data;
using ParkIRC.Models;
using ParkIRC.Services;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using QRCoder;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace ParkIRC.Controllers
{
    [Authorize]
    public class GateController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<GateController> _logger;
        private readonly IPrinterService _printerService;

        public GateController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            ILogger<GateController> logger,
            IPrinterService printerService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _printerService = printerService;
        }

        // GET: Gate/Entry
        public IActionResult Entry()
        {
            // Load any saved camera settings from database or cookies
            // and pass to the view if needed
            
            return View();
        }

        // POST: Gate/SaveCameraSettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCameraSettings([FromBody] CameraSettings settings)
        {
            try
            {
                _logger.LogInformation($"Saving camera settings. Type: {settings.Type}");
                
                // Temukan pengaturan kamera yang ada
                var existingSettings = await _context.CameraSettings
                    .FirstOrDefaultAsync(c => c.GateId == settings.GateId);
                    
                if (existingSettings != null)
                {
                    // Update existing settings
                    existingSettings.Type = settings.Type;
                    existingSettings.IpAddress = settings.IpAddress;
                    existingSettings.Port = settings.Port;
                    existingSettings.Path = settings.Path;
                    existingSettings.Username = settings.Username;
                    existingSettings.Password = settings.Password;
                    existingSettings.LastUpdated = DateTime.Now;
                    existingSettings.UpdatedBy = User.FindFirstValue(ClaimTypes.Name) ?? "System";
                    
                    _context.CameraSettings.Update(existingSettings);
                }
                else
                {
                    // Create new settings
                    var newSettings = new CameraSettings
                    {
                        GateId = settings.GateId,
                        Type = settings.Type,
                        IpAddress = settings.IpAddress,
                        Port = settings.Port,
                        Path = settings.Path,
                        Username = settings.Username,
                        Password = settings.Password,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        LastUpdated = DateTime.Now,
                        UpdatedBy = User.FindFirstValue(ClaimTypes.Name) ?? "System"
                    };
                    
                    await _context.CameraSettings.AddAsync(newSettings);
                }
                
                await _context.SaveChangesAsync();
                
                return Json(new { success = true, message = "Pengaturan kamera berhasil disimpan" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving camera settings");
                return Json(new { success = false, message = "Terjadi kesalahan: " + ex.Message });
            }
        }
        
        // GET: Gate/GetCameraSettings
        [HttpGet]
        public async Task<IActionResult> GetCameraSettings(string gateId = "GATE1")
        {
            try
            {
                // Berdasarkan database, CameraSettings tidak memiliki kolom GateId
                // Jadi, ambil pengaturan kamera terbaru saja
                var settings = await _context.CameraSettings
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync();
                    
                if (settings == null)
                {
                    // Return default settings
                    return Json(new 
                    { 
                        success = true, 
                        settings = new 
                        {
                            type = "Webcam",
                            ipAddress = "192.168.1.100",
                            port = "8080",
                            path = "/video",
                            username = "admin",
                            password = "",
                            isActive = true
                        }
                    });
                }
                
                return Json(new 
                { 
                    success = true, 
                    settings = new 
                    {
                        type = "Webcam", // Default karena tidak ada di model
                        ipAddress = "192.168.1.100", // Default
                        port = "8080", // Default
                        path = "/video", // Default
                        username = "admin", // Default
                        password = "●●●●●●●●", // Don't return the actual password
                        isActive = settings.IsActive,
                        profileName = settings.ProfileName,
                        brightness = settings.Brightness,
                        contrast = settings.Contrast
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting camera settings");
                return Json(new { success = false, message = "Terjadi kesalahan: " + ex.Message });
            }
        }

        // POST: Gate/ProcessEntry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessEntry([FromForm] VehicleEntryModel model, [FromForm] string base64Image)
        {
            try
            {
                _logger.LogInformation($"Processing vehicle entry for {model.VehicleNumber}, Type: {model.VehicleType}");

                // Check if vehicle is already parked
                var existingVehicle = await _context.Vehicles
                    .FirstOrDefaultAsync(v => v.VehicleNumber == model.VehicleNumber && v.IsParked);

                if (existingVehicle != null)
                {
                    return Json(new { success = false, message = "Kendaraan sudah terparkir" });
                }

                // Find available parking space
                var parkingSpace = await FindOptimalParkingSpace(model.VehicleType);
                if (parkingSpace == null)
                {
                    return Json(new { success = false, message = "Tidak ada slot parkir yang tersedia untuk jenis kendaraan ini" });
                }

                // Save image if provided
                string? photoPath = null;
                bool cameraError = false;
                if (!string.IsNullOrEmpty(base64Image))
                {
                    try 
                    {
                        photoPath = await SaveBase64Image(base64Image, "entry-photos");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving entry photo");
                        cameraError = true;
                        // Continue without photo
                    }
                }

                // Get current shift
                var currentShift = await GetCurrentShiftAsync();
                if (currentShift == null)
                {
                    return Json(new { success = false, message = "Tidak ada shift aktif saat ini" });
                }

                // Create new vehicle record
                var vehicle = new Vehicle
                {
                    VehicleNumber = model.VehicleNumber,
                    VehicleType = model.VehicleType,
                    DriverName = model.DriverName ?? string.Empty,
                    PhoneNumber = model.PhoneNumber ?? string.Empty,
                    EntryTime = DateTime.Now,
                    IsParked = true,
                    EntryImagePath = photoPath ?? string.Empty,
                    ParkingSpaceId = parkingSpace.Id,
                    ShiftId = currentShift.Id,
                    TicketNumber = GenerateTicketNumber(),
                    PlateNumber = model.VehicleNumber,
                    EntryGateId = "GATE1",
                    ExitGateId = "",
                    ExternalSystemId = $"VEH-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6)}",
                    CreatedBy = User.FindFirstValue(ClaimTypes.Name) ?? "System",
                    CreatedAt = DateTime.Now,
                    Status = "Active",
                    VehicleTypeId = GetVehicleTypeId(model.VehicleType)
                };

                // Generate barcode
                string barcodeData = GenerateBarcodeData(vehicle);
                string barcodeImagePath;
                bool barcodeError = false;
                try
                {
                    barcodeImagePath = await GenerateAndSaveQRCode(barcodeData);
                    vehicle.BarcodeImagePath = barcodeImagePath;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating barcode");
                    barcodeError = true;
                    barcodeImagePath = string.Empty;
                }

                // Update parking space
                parkingSpace.IsOccupied = true;
                parkingSpace.LastOccupiedTime = DateTime.Now;

                // Generate ticket number yang akan digunakan bersama
                string ticketNumber = GenerateTicketNumber();
                vehicle.TicketNumber = ticketNumber;

                // Create parking ticket
                var ticket = new ParkingTicket
                {
                    Id = $"TKT-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6)}",
                    TicketNumber = ticketNumber,
                    BarcodeData = barcodeData,
                    BarcodeImagePath = barcodeImagePath,
                    IssueTime = DateTime.Now,
                    EntryTime = DateTime.Now,
                    Vehicle = vehicle,
                    OperatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    ShiftId = currentShift.Id,
                    CreatedAt = DateTime.Now,
                    CreatedBy = User.FindFirstValue(ClaimTypes.Name) ?? "System",
                    Status = "Active",
                    IsValid = true,
                    LicensePlate = vehicle.VehicleNumber
                };

                // Pisahkan operasi penyimpanan untuk mengisolasi error
                try
                {
                    // Simpan vehicle terlebih dahulu
                    await _context.Vehicles.AddAsync(vehicle);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Berhasil menyimpan vehicle dengan ID {vehicle.Id}");
                    
                    // Kemudian simpan parking ticket
                    await _context.ParkingTickets.AddAsync(ticket);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Berhasil menyimpan parking ticket dengan ID {ticket.Id}");
                    
                    // Buat transaction setelah vehicle dan ticket tersimpan
                    var transaction = new ParkingTransaction
                    {
                        Id = $"TRX-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6)}",
                        VehicleId = vehicle.Id,
                        ParkingSpaceId = parkingSpace.Id,
                        EntryTime = DateTime.Now,
                        TransactionNumber = GenerateTransactionNumber(),
                        TicketNumber = ticket.TicketNumber,
                        Status = "Active",
                        PaymentStatus = "Pending",
                        PaymentMethod = "Cash",
                        VehicleNumber = vehicle.VehicleNumber,
                        VehicleType = vehicle.VehicleType,
                        EntryPoint = "GATE",
                        IsManualEntry = false,
                        HourlyRate = parkingSpace.HourlyRate,
                        Amount = 0m,
                        TotalAmount = 0m,
                        PaymentAmount = 0m,
                        OperatorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System",
                        ImagePath = vehicle.EntryImagePath ?? "",
                        VehicleImagePath = vehicle.EntryImagePath ?? ""
                    };

                    // Terakhir, simpan parking transaction
                    await _context.ParkingTransactions.AddAsync(transaction);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Berhasil menyimpan semua data");
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Error detail saat menyimpan data: {Message}", dbEx.Message);
                    if (dbEx.InnerException != null)
                    {
                        _logger.LogError(dbEx.InnerException, "Inner exception: {Message}", dbEx.InnerException.Message);
                    }
                    throw; // Re-throw untuk dihandle di catch block utama
                }

                // Print entry ticket
                var entryTime = DateTime.Now;

                bool printSuccess = false;
                try
                {
                    printSuccess = await _printerService.PrintTicket(ticketNumber, entryTime.ToString("dd/MM/yyyy HH:mm:ss"));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error printing ticket");
                }

                // Open gate
                await OpenGateAsync();

                string message = "Kendaraan berhasil diproses";
                if (cameraError) message += " (Foto tidak tersimpan)";
                if (barcodeError) message += " (Barcode tidak tersimpan)";
                if (!printSuccess) message += " (Gagal mencetak tiket)";

                return Json(new { 
                    success = true, 
                    message = message,
                    ticketNumber = ticket.TicketNumber,
                    entryTime = vehicle.EntryTime,
                    barcodeImageUrl = !string.IsNullOrEmpty(barcodeImagePath) ? $"/images/barcodes/{Path.GetFileName(barcodeImagePath)}" : null,
                    printStatus = printSuccess,
                    cameraError = cameraError,
                    barcodeError = barcodeError
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing vehicle entry");
                return Json(new { success = false, message = "Terjadi kesalahan: " + ex.Message });
            }
        }

        // GET: Gate/Exit
        public IActionResult Exit()
        {
            return View();
        }

        // POST: Gate/ProcessExit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessExit([FromForm] string ticketNumber, [FromForm] string base64Image)
        {
            try
            {
                var ticket = await _context.ParkingTickets
                    .Include(t => t.Vehicle)
                    .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber);

                if (ticket == null)
                {
                    return NotFound(new { error = "Tiket tidak ditemukan" });
                }

                if (ticket.IsUsed)
                {
                    return BadRequest(new { error = "Tiket sudah digunakan" });
                }

                // Save exit photo
                string? exitPhotoPath = null;
                if (!string.IsNullOrEmpty(base64Image))
                {
                    exitPhotoPath = await SaveBase64Image(base64Image, "exit_photos");
                }

                // Update vehicle status
                var vehicle = ticket.Vehicle;
                if (vehicle == null)
                {
                    return BadRequest(new { error = "Vehicle data not found" });
                }

                vehicle.IsParked = false;
                vehicle.ExitTime = DateTime.Now;
                vehicle.ExitImagePath = exitPhotoPath ?? string.Empty;

                // Mark ticket as used
                ticket.IsUsed = true;
                ticket.ScanTime = DateTime.Now;

                // Get current shift
                var currentShift = await GetCurrentShiftAsync();
                if (currentShift == null)
                {
                    return BadRequest(new { error = "Tidak ada shift aktif saat ini" });
                }

                // Create journal entry
                var journal = new Journal
                {
                    Action = "Check Out",
                    Description = $"Vehicle {vehicle.VehicleNumber} checked out",
                    Timestamp = DateTime.UtcNow,
                    OperatorId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                };

                _context.Journals.Add(journal);
                await _context.SaveChangesAsync();

                // Find active transaction
                var transaction = await _context.ParkingTransactions
                    .FirstOrDefaultAsync(t => t.VehicleId == vehicle.Id && t.Status == "Active");
                if (transaction == null)
                {
                    return BadRequest(new { error = "Parking transaction not found" });
                }

                // Print receipt
                bool printSuccess = await _printerService.PrintReceipt(ticket.TicketNumber, vehicle.ExitTime?.ToString("dd/MM/yyyy HH:mm:ss") ?? "", ticket.Amount);
                if (!printSuccess)
                {
                    _logger.LogWarning("Failed to print receipt for transaction {TransactionNumber}", ticket.TicketNumber);
                }

                // Open the gate
                await OpenGateAsync();

                return Ok(new
                {
                    message = "Kendaraan berhasil keluar" + (!printSuccess ? " (Gagal mencetak struk)" : ""),
                    vehicleNumber = vehicle.VehicleNumber,
                    entryTime = vehicle.EntryTime,
                    exitTime = vehicle.ExitTime,
                    duration = (vehicle.ExitTime - vehicle.EntryTime)?.ToString(@"hh\:mm"),
                    printStatus = printSuccess
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Terjadi kesalahan: {ex.Message}" });
            }
        }

        private async Task<string> SaveBase64Image(string base64Image, string folder)
        {
            try
            {
                var base64Data = base64Image.Split(',')[1];
                var imageBytes = Convert.FromBase64String(base64Data);

                using var image = Image.Load<Rgba32>(imageBytes);
                
                var uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "images", folder);
                Directory.CreateDirectory(uploadDir);

                var fileName = $"{DateTime.Now:yyyyMMddHHmmss}.png";
                var filePath = Path.Combine(uploadDir, fileName);

                await image.SaveAsync(filePath);

                return $"/images/{folder}/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving base64 image");
                throw;
            }
        }

        private string GenerateBarcodeData(Vehicle vehicle)
        {
            return $"{vehicle.VehicleNumber}|{vehicle.EntryTime:yyyyMMddHHmmss}";
        }

        private string GenerateTicketNumber()
        {
            return $"TKT{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }

        private string GenerateTransactionNumber()
        {
            return $"TRN-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";
        }

        private async Task<string> GenerateAndSaveQRCode(string data)
        {
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new BitmapByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);

            // Convert byte array to ImageSharp Image
            using var image = Image.Load<Rgba32>(qrCodeBytes);
            
            // Ensure directory exists
            var barcodeDir = Path.Combine(_webHostEnvironment.WebRootPath, "images", "barcodes");
            Directory.CreateDirectory(barcodeDir);

            // Save the image
            var fileName = $"barcode_{DateTime.Now:yyyyMMddHHmmss}.png";
            var filePath = Path.Combine(barcodeDir, fileName);
            
            await image.SaveAsync(filePath);

            return $"/images/barcodes/{fileName}";
        }

        private string GenerateJournalNumber()
        {
            return $"JRN-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";
        }

        private async Task<Shift?> GetCurrentShiftAsync()
        {
            try
            {
                var now = DateTime.Now;
                // First get all active shifts
                var activeShifts = await _context.Shifts
                    .Where(s => s.IsActive)
                    .ToListAsync();
                
                // Then check time in memory
                return activeShifts.FirstOrDefault(s => s.IsTimeInShift(now));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current shift");
                return null;
            }
        }

        private async Task<ParkingSpace?> FindOptimalParkingSpace(string vehicleType)
        {
            try
            {
                // Use a custom SQL query to handle missing columns
                var availableSpaces = await _context.ParkingSpaces
                    .FromSqlRaw(@"SELECT ""Id"", ""SpaceNumber"", ""SpaceType"", ""IsOccupied"", ""HourlyRate"", 
                        ""CurrentVehicleId"", '' AS ""Location"", '' AS ""ReservedFor"", ""LastOccupiedTime"", 
                        false AS ""IsReserved"" 
                        FROM ""ParkingSpaces"" 
                        WHERE NOT ""IsOccupied"" AND ""SpaceType"" = {0}
                        ORDER BY ""LastOccupiedTime"" 
                        LIMIT 1", vehicleType)
                    .FirstOrDefaultAsync();

                if (availableSpaces == null)
                {
                    _logger.LogWarning($"No available parking space found for vehicle type: {vehicleType}");
                    return null;
                }

                return availableSpaces;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding optimal parking space for vehicle type: {vehicleType}");
                return null;
            }
        }

        private async Task OpenGateAsync()
        {
            try
            {
                // TODO: Implement actual gate control logic
                await Task.Delay(1000); // Simulate gate operation
                _logger.LogInformation("Gate opened successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening gate");
                throw;
            }
        }

        // Method untuk menentukan VehicleTypeId berdasarkan jenis kendaraan
        private int GetVehicleTypeId(string vehicleType)
        {
            return vehicleType.ToLower() switch
            {
                "car" => 1,
                "motorcycle" => 2,
                "truck" => 3,
                "bus" => 4,
                _ => 1 // Default ke Car jika tidak dikenali
            };
        }
    }
} 