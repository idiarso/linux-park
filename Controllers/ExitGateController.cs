using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ParkIRC.Data;
using ParkIRC.Models;
using ParkIRC.Services;
using ParkIRC.Hubs;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ParkIRC.Controllers
{
    [Authorize(Roles = "Operator")]
    public class ExitGateController : Controller
    {
        private readonly ILogger<ExitGateController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly PrintService _printService;
        private readonly IHubContext<ParkingHub> _hubContext;

        public ExitGateController(
            ILogger<ExitGateController> logger,
            ApplicationDbContext context,
            PrintService printService,
            IHubContext<ParkingHub> hubContext)
        {
            _logger = logger;
            _context = context;
            _printService = printService;
            _hubContext = hubContext;
        }

        // View utama untuk pintu keluar - defaultnya scan barcode
        public IActionResult Index()
        {
            ViewBag.DefaultMode = "barcode";
            return View();
        }

        // Process barcode dari scanner
        [HttpPost]
        public async Task<IActionResult> ProcessBarcode(string barcode)
        {
            try
            {
                if (string.IsNullOrEmpty(barcode))
                {
                    return Json(new { success = false, message = "Barcode tidak valid" });
                }

                // Cari transaksi berdasarkan barcode
                var transaction = await _context.ParkingTransactions
                    .Include(t => t.Vehicle)
                    .FirstOrDefaultAsync(t => t.TicketNumber == barcode && t.ExitTime == default);

                if (transaction == null)
                {
                    return Json(new { success = false, message = "Tiket tidak ditemukan atau sudah digunakan" });
                }

                // Hitung durasi dan biaya
                var exitTime = DateTime.Now;
                var duration = exitTime - transaction.EntryTime;
                
                // Update transaksi
                transaction.ExitTime = exitTime;
                // Hitung biaya sesuai tarif
                transaction.TotalAmount = CalculateFee(transaction.Vehicle.VehicleType, duration);
                
                await _context.SaveChangesAsync();
                
                // Buka gate
                await _hubContext.Clients.All.SendAsync("OpenGate", "EXIT", transaction.ParkingSpace);
                
                // Catat log
                _logger.LogInformation($"Exit gate opened for vehicle {transaction.Vehicle.VehicleNumber} with barcode {barcode}");
                
                // Return data untuk tampilan receipt
                return Json(new { 
                    success = true, 
                    vehicleNumber = transaction.Vehicle.VehicleNumber,
                    vehicleType = transaction.Vehicle.VehicleType,
                    entryTime = transaction.EntryTime.ToString("dd/MM/yyyy HH:mm:ss"),
                    exitTime = exitTime.ToString("dd/MM/yyyy HH:mm:ss"),
                    duration = FormatDuration(duration),
                    amount = transaction.TotalAmount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing barcode");
                return Json(new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        // Find by vehicle number (alternative method)
        [HttpPost]
        public async Task<IActionResult> FindByVehicleNumber(string vehicleNumber)
        {
            if (string.IsNullOrEmpty(vehicleNumber))
            {
                return Json(new { success = false, message = "Nomor kendaraan tidak valid" });
            }

            // Normalisasi nomor kendaraan (hapus spasi berlebih dan ubah ke uppercase)
            vehicleNumber = vehicleNumber.Trim().ToUpper();
            
            // Cari dengan nomor kendaraan yang dinormalisasi dan gunakan Contains untuk pencarian lebih fleksibel
            var transaction = await _context.ParkingTransactions
                .Include(t => t.Vehicle)
                .Where(t => t.ExitTime == default)
                .OrderByDescending(t => t.EntryTime)
                .FirstOrDefaultAsync(t => 
                    // Periksa apakah vehicle number persis sama
                    t.Vehicle.VehicleNumber.ToUpper() == vehicleNumber || 
                    // Atau coba tanpa spasi (misal B1234CD)
                    t.Vehicle.VehicleNumber.Replace(" ", "").ToUpper() == vehicleNumber.Replace(" ", "") ||
                    // Atau coba contains agar lebih fleksibel
                    t.Vehicle.VehicleNumber.ToUpper().Contains(vehicleNumber) ||
                    vehicleNumber.Contains(t.Vehicle.VehicleNumber.ToUpper()));

            if (transaction == null)
            {
                // Jika tidak ditemukan, cari juga di tabel Vehicle langsung untuk kasus khusus
                var vehicle = await _context.Vehicles
                    .Where(v => v.IsParked && 
                          (v.VehicleNumber.ToUpper() == vehicleNumber || 
                           v.VehicleNumber.Replace(" ", "").ToUpper() == vehicleNumber.Replace(" ", "") ||
                           v.VehicleNumber.ToUpper().Contains(vehicleNumber) ||
                           vehicleNumber.Contains(v.VehicleNumber.ToUpper())))
                    .OrderByDescending(v => v.EntryTime)
                    .FirstOrDefaultAsync();

                if (vehicle != null)
                {
                    // Jika kendaraan ditemukan di tabel Vehicles tapi tidak di Transactions, coba cari transaksi terkait
                    transaction = await _context.ParkingTransactions
                        .Include(t => t.Vehicle)
                        .Where(t => t.VehicleId == vehicle.Id && t.ExitTime == default)
                        .OrderByDescending(t => t.EntryTime)
                        .FirstOrDefaultAsync();
                    
                    if (transaction == null)
                    {
                        // Jika masih tidak ditemukan transaksi, beri feedback lebih spesifik
                        return Json(new { success = false, message = $"Kendaraan {vehicle.VehicleNumber} ditemukan tetapi tidak memiliki transaksi parkir aktif" });
                    }
                }
                else
                {
                    return Json(new { success = false, message = "Kendaraan tidak ditemukan atau sudah keluar" });
                }
            }

            _logger.LogInformation($"Kendaraan ditemukan: {transaction.Vehicle?.VehicleNumber} dengan tiket {transaction.TicketNumber}");

            return Json(new { 
                success = true, 
                ticketNumber = transaction.TicketNumber,
                vehicleNumber = transaction.Vehicle?.VehicleNumber ?? "Unknown",
                vehicleType = transaction.Vehicle?.VehicleType ?? "Unknown",
                entryTime = transaction.EntryTime.ToString("dd/MM/yyyy HH:mm:ss")
            });
        }

        private decimal CalculateFee(string vehicleType, TimeSpan duration)
        {
            // Implementasi perhitungan tarif sesuai jenis kendaraan dan durasi
            // Contoh sederhana
            decimal hourlyRate = vehicleType.ToLower() == "motor" ? 2000 : 5000;
            int hours = (int)Math.Ceiling(duration.TotalHours);
            return hours * hourlyRate;
        }

        private string FormatDuration(TimeSpan duration)
        {
            return $"{(int)duration.TotalHours} jam {duration.Minutes} menit";
        }

        // Method to print receipt
        [HttpPost]
        public async Task<IActionResult> PrintReceipt(string ticketNumber, string vehicleNumber, string entryTime, 
                                                    string exitTime, string duration, decimal amount)
        {
            try
            {
                _logger.LogInformation("=== PRINT RECEIPT START ===");
                _logger.LogInformation("Printing receipt for vehicle {VehicleNumber}, ticket {TicketNumber}", vehicleNumber, ticketNumber);
                
                // Create receipt content
                var content = new StringBuilder();
                content.AppendLine("STRUK PARKIR - KELUAR");
                content.AppendLine("===================");
                content.AppendLine();
                content.AppendLine($"No. Tiket    : {ticketNumber}");
                content.AppendLine($"No. Kendaraan: {vehicleNumber}");
                content.AppendLine();
                content.AppendLine($"Waktu Masuk : {entryTime}");
                content.AppendLine($"Waktu Keluar: {exitTime}");
                content.AppendLine($"Durasi      : {duration}");
                content.AppendLine();
                content.AppendLine($"Total Biaya : Rp {amount:N0}");
                content.AppendLine();
                content.AppendLine("===================");
                content.AppendLine("Terima Kasih");
                content.AppendLine();
                content.AppendLine();
                content.AppendLine();

                // Coba simpan konten receipt ke file terlebih dahulu
                try {
                    string debugFile = $"receipt_debug_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                    System.IO.File.WriteAllText(debugFile, content.ToString());
                    _logger.LogInformation($"Saved receipt content to {debugFile} for debugging");
                }
                catch (Exception ex) {
                    _logger.LogWarning($"Could not save receipt content to file: {ex.Message}");
                }

                // Log content untuk debugging
                _logger.LogInformation("=== RECEIPT CONTENT START ===");
                _logger.LogInformation($"{content}");
                _logger.LogInformation("=== RECEIPT CONTENT END ===");
                
                // Send to printer
                _logger.LogInformation("Sending to printer service...");
                bool printSuccess = _printService.PrintTicket(content.ToString());
                _logger.LogInformation($"Print result: {(printSuccess ? "SUCCESS" : "FAILED")}");
                
                if (!printSuccess)
                {
                    _logger.LogWarning("Failed to print receipt for vehicle {VehicleNumber}", vehicleNumber);
                    
                    // Coba alternatif printing ke file jika gagal
                    try {
                        string filename = $"receipt_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                        System.IO.File.WriteAllText(filename, content.ToString());
                        _logger.LogInformation($"Receipt saved to file {filename} as fallback");
                        
                        _logger.LogInformation("=== PRINT RECEIPT END (FALLBACK) ===");
                        return Json(new { 
                            success = false, 
                            message = $"Gagal mencetak struk ke printer. Receipt disimpan ke file {filename}." 
                        });
                    }
                    catch (Exception ex) {
                        _logger.LogError(ex, "Gagal menyimpan receipt ke file");
                        _logger.LogInformation("=== PRINT RECEIPT END (ERROR) ===");
                        return Json(new { success = false, message = "Gagal mencetak struk. Cek status printer." });
                    }
                }
                
                _logger.LogInformation("=== PRINT RECEIPT END (SUCCESS) ===");
                return Json(new { success = true, message = "Struk berhasil dicetak" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing receipt for vehicle {VehicleNumber}", vehicleNumber);
                return Json(new { success = false, message = $"Terjadi kesalahan sistem: {ex.Message}" });
            }
        }
    }
} 