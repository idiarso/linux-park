using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ParkIRC.Models;
using ParkIRC.Services;
using System;

namespace ParkIRC.Controllers
{
    /// <summary>
    /// Controller API untuk komunikasi dengan Arduino
    /// Menangani perintah untuk membuka/menutup gerbang dan mengirim perintah print
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ArduinoController : ControllerBase
    {
        private readonly ILogger<ArduinoController> _logger;
        private readonly IArduinoService _arduinoService;
        private readonly IParkingService _parkingService;
        private readonly IPrinterService _printerService;

        public ArduinoController(
            ILogger<ArduinoController> logger,
            IArduinoService arduinoService,
            IParkingService parkingService,
            IPrinterService printerService)
        {
            _logger = logger;
            _arduinoService = arduinoService;
            _parkingService = parkingService;
            _printerService = printerService;
            
            // Mendaftarkan handler event dari Arduino
            _arduinoService.ArduinoEventReceived += ArduinoService_EventReceived;
        }

        /// <summary>
        /// Menangani event yang diterima dari Arduino
        /// </summary>
        private async void ArduinoService_EventReceived(object? sender, string eventData)
        {
            _logger.LogInformation("Event Arduino diterima: {EventData}", eventData);
            
            // Menangani berbagai jenis event
            if (eventData.StartsWith("EVENT:VEHICLE_DETECTED"))
            {
                await HandleVehicleDetected();
            }
            else if (eventData.StartsWith("EVENT:BUTTON_PRESS"))
            {
                await HandleButtonPress();
            }
            else if (eventData.StartsWith("EVENT:VEHICLE_PASSED"))
            {
                // Kendaraan telah melewati gerbang
                _logger.LogInformation("Kendaraan telah melewati gerbang");
            }
            else if (eventData.StartsWith("EVENT:GATE_TIMEOUT"))
            {
                // Timeout gerbang, gerbang ditutup secara otomatis
                _logger.LogInformation("Gerbang ditutup otomatis karena timeout");
            }
        }

        /// <summary>
        /// Menangani event ketika kendaraan terdeteksi
        /// </summary>
        private async Task HandleVehicleDetected()
        {
            try
            {
                // Membuat entri parkir baru
                var vehicleNumber = "TEMP-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                _logger.LogInformation("Mencoba membuat entri parkir untuk kendaraan: {VehicleNumber}", vehicleNumber);
                
                // Sementara kita hanya log saja, karena CreateEntryAsync tidak ada di IParkingService
                _logger.LogInformation("Simulasi membuat entri parkir (CreateEntryAsync tidak tersedia)");
                
                // Buka gerbang saja
                var gateResult = await _arduinoService.OpenGateAsync();
                
                if (!gateResult)
                {
                    _logger.LogError("Gagal membuka gerbang untuk kendaraan: {VehicleNumber}", vehicleNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saat menangani deteksi kendaraan");
            }
        }

        /// <summary>
        /// Menangani event ketika tombol ditekan
        /// </summary>
        private async Task HandleButtonPress()
        {
            try
            {
                _logger.LogInformation("Tombol ditekan, membuka gerbang");
                var result = await _arduinoService.OpenGateAsync();
                
                if (!result)
                {
                    _logger.LogError("Gagal membuka gerbang saat tombol ditekan");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saat menangani penekanan tombol");
            }
        }

        /// <summary>
        /// Mendapatkan status Arduino dan gerbang
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                // Menginisialisasi koneksi Arduino jika belum
                if (!await _arduinoService.InitializeAsync())
                {
                    _logger.LogError("Gagal menginisialisasi koneksi Arduino");
                    return StatusCode(503, new { Error = "Koneksi Arduino gagal", Message = "Tidak dapat terhubung ke perangkat Arduino" });
                }
                
                var status = await _arduinoService.GetStatusAsync();
                
                if (string.IsNullOrEmpty(status))
                {
                    return StatusCode(500, new { Error = "Status tidak tersedia", Message = "Tidak dapat mendapatkan status dari Arduino" });
                }
                
                return Ok(new { Status = status, Timestamp = DateTime.Now });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saat mendapatkan status Arduino");
                return StatusCode(500, new { Error = "Kesalahan komunikasi", Message = "Error saat berkomunikasi dengan Arduino" });
            }
        }

        /// <summary>
        /// Membuka gerbang parkir
        /// </summary>
        [HttpPost("gate/open")]
        public async Task<IActionResult> OpenGate()
        {
            try
            {
                if (!await _arduinoService.InitializeAsync())
                {
                    _logger.LogError("Gagal menginisialisasi koneksi Arduino");
                    return StatusCode(503, new { Success = false, Message = "Koneksi Arduino gagal" });
                }
                
                var result = await _arduinoService.OpenGateAsync();
                
                if (!result)
                {
                    return StatusCode(500, new { Success = false, Message = "Gagal membuka gerbang" });
                }
                
                return Ok(new { Success = true, Message = "Gerbang berhasil dibuka", Timestamp = DateTime.Now });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saat membuka gerbang");
                return StatusCode(500, new { Success = false, Message = "Error saat berkomunikasi dengan Arduino" });
            }
        }

        /// <summary>
        /// Menutup gerbang parkir
        /// </summary>
        [HttpPost("gate/close")]
        public async Task<IActionResult> CloseGate()
        {
            try
            {
                if (!await _arduinoService.InitializeAsync())
                {
                    _logger.LogError("Gagal menginisialisasi koneksi Arduino");
                    return StatusCode(503, new { Success = false, Message = "Koneksi Arduino gagal" });
                }
                
                var result = await _arduinoService.CloseGateAsync();
                
                if (!result)
                {
                    return StatusCode(500, new { Success = false, Message = "Gagal menutup gerbang" });
                }
                
                return Ok(new { Success = true, Message = "Gerbang berhasil ditutup", Timestamp = DateTime.Now });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saat menutup gerbang");
                return StatusCode(500, new { Success = false, Message = "Error saat berkomunikasi dengan Arduino" });
            }
        }

        /// <summary>
        /// Mengirim perintah cetak ke printer melalui Arduino
        /// </summary>
        [HttpPost("print")]
        public async Task<IActionResult> SendPrintCommand([FromBody] PrintCommandModel model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.PrintData))
                {
                    return BadRequest(new { Success = false, Message = "Data cetak tidak boleh kosong" });
                }
                
                if (!await _arduinoService.InitializeAsync())
                {
                    _logger.LogError("Gagal menginisialisasi koneksi Arduino");
                    return StatusCode(503, new { Success = false, Message = "Koneksi Arduino gagal" });
                }
                
                var result = await _arduinoService.SendPrintCommandAsync(model.PrintData);
                
                if (!result)
                {
                    return StatusCode(500, new { Success = false, Message = "Gagal mengirim perintah cetak" });
                }
                
                return Ok(new { Success = true, Message = "Perintah cetak berhasil dikirim", Timestamp = DateTime.Now });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saat mengirim perintah cetak");
                return StatusCode(500, new { Success = false, Message = "Error saat berkomunikasi dengan Arduino" });
            }
        }
    }

    /// <summary>
    /// Model untuk mengirim perintah cetak
    /// </summary>
    public class PrintCommandModel
    {
        public string PrintData { get; set; } = string.Empty;
    }
} 