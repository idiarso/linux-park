using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ParkIRC.Data;
using ParkIRC.Models;
using ParkIRC.Services;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using ZXing;
using ZXing.Common;
using System.Text;

namespace ParkIRC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArduinoGateController : ControllerBase
    {
        private readonly ILogger<ArduinoGateController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IPrinterService _printerService;
        private readonly SerialPortService _serialPortService;
        private readonly TimeService _timeService;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public ArduinoGateController(
            ILogger<ArduinoGateController> logger,
            ApplicationDbContext context,
            IPrinterService printerService,
            SerialPortService serialPortService,
            TimeService timeService)
        {
            _logger = logger;
            _context = context;
            _printerService = printerService;
            _serialPortService = serialPortService;
            _timeService = timeService;
        }

        [HttpPost("HandleGateEvent")]
        public async Task<IActionResult> HandleGateEvent([FromBody] GateEventModel model)
        {
            try
            {
                _logger.LogInformation($"Received gate event: {model.EventType} at {model.EventTime}");

                // Handle different types of events
                switch (model.EventType)
                {
                    case "BUTTON_PRESS":
                    case "VEHICLE_DETECTED":
                        // Generate ticket for entry
                        var ticketData = await GenerateTicketAsync();
                        return Ok(new { success = true, ticketData = ticketData });

                    case "VEHICLE_PASSED":
                        // Log vehicle passing through gate
                        await LogGateEventAsync("VEHICLE_PASSED", model.Data);
                        return Ok(new { success = true });

                    case "GATE_TIMEOUT":
                        // Log gate timeout
                        await LogGateEventAsync("GATE_TIMEOUT", model.Data);
                        return Ok(new { success = true });

                    case "TICKET_REQUEST":
                        if (model.Data != null && model.Data.TryGetValue("ticketId", out var ticketId))
                        {
                            var generatedTicket = await GenerateTicketAsync(ticketId.ToString());
                            return Ok(new { success = true, ticketData = generatedTicket });
                        }
                        return BadRequest(new { success = false, message = "Missing ticketId" });

                    default:
                        return BadRequest(new { success = false, message = "Unknown event type" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling gate event");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("GenerateBarcode/{ticketNumber}")]
        public async Task<IActionResult> GenerateBarcode(string ticketNumber)
        {
            try
            {
                var barcodeImage = await GenerateBarcodeImageAsync(ticketNumber);
                return File(barcodeImage, "image/png");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating barcode");
                return StatusCode(500, "Error generating barcode");
            }
        }

        [HttpPost("OpenGate")]
        public async Task<IActionResult> OpenGate()
        {
            try
            {
                if (await _semaphore.WaitAsync(TimeSpan.FromSeconds(5)))
                {
                    try
                    {
                        bool success = await _serialPortService.SendCommandAsync("OPEN");
                        return Ok(new { success });
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
                else
                {
                    return StatusCode(429, new { success = false, message = "Gate controller is busy" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening gate");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost("CloseGate")]
        public async Task<IActionResult> CloseGate()
        {
            try
            {
                if (await _semaphore.WaitAsync(TimeSpan.FromSeconds(5)))
                {
                    try
                    {
                        bool success = await _serialPortService.SendCommandAsync("CLOSE");
                        return Ok(new { success });
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
                else
                {
                    return StatusCode(429, new { success = false, message = "Gate controller is busy" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing gate");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("Status")]
        public async Task<IActionResult> GetGateStatus()
        {
            try
            {
                if (await _semaphore.WaitAsync(TimeSpan.FromSeconds(5)))
                {
                    try
                    {
                        var statusResponse = await _serialPortService.SendCommandAndWaitForResponseAsync("STATUS");
                        if (statusResponse.StartsWith("STATUS:"))
                        {
                            var parts = statusResponse.Substring(7).Split(':');
                            return Ok(new { 
                                success = true, 
                                gateStatus = parts[0],
                                vehicleStatus = parts.Length > 1 ? parts[1] : "UNKNOWN"
                            });
                        }
                        return BadRequest(new { success = false, message = "Invalid response" });
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
                else
                {
                    return StatusCode(429, new { success = false, message = "Gate controller is busy" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gate status");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost("Print")]
        public async Task<IActionResult> PrintTicket([FromBody] PrintTicketRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.TicketNumber))
                {
                    return BadRequest(new { success = false, message = "Ticket number is required" });
                }

                // Generate and print ticket
                var ticketData = await GenerateTicketAsync(request.TicketNumber);
                
                // Send to printer via the SerialPortService
                bool printSuccess = await _serialPortService.SendCommandAsync($"PRINT:{ticketData}");
                
                return Ok(new { success = printSuccess });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing ticket");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        #region Private Helper Methods

        private async Task<string> GenerateTicketAsync(string ticketNumber = null)
        {
            // If ticket number not provided, generate a new one
            if (string.IsNullOrEmpty(ticketNumber))
            {
                ticketNumber = GenerateTicketNumber();
            }

            // Save the pending ticket in the database
            await CreatePendingVehicleEntryAsync(ticketNumber);

            // Current time for the ticket
            var currentTime = _timeService.GetCurrentTime();
            
            // Format the ticket for printing
            string ticketData = FormatTicketForPrinting(ticketNumber, currentTime);
            
            return ticketData;
        }

        private string FormatTicketForPrinting(string ticketNumber, DateTime entryTime)
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("TITLE:PARKING TICKET");
            sb.AppendLine("LINE");
            sb.AppendLine($"Date: {entryTime:dd-MM-yyyy}");
            sb.AppendLine($"Time: {entryTime:HH:mm:ss}");
            sb.AppendLine("LARGE:WELCOME");
            sb.AppendLine("LINE");
            sb.AppendLine($"BARCODE:{ticketNumber}");
            sb.AppendLine("Ticket Number:");
            sb.AppendLine(ticketNumber);
            sb.AppendLine("LINE");
            sb.AppendLine("Thank you for parking with us!");
            
            return sb.ToString();
        }

        private string GenerateTicketNumber()
        {
            // Format: TKT-YYYYMMDD-XXXXXX (where X is random alphanumeric)
            return $"TKT-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
        }

        private async Task<byte[]> GenerateBarcodeImageAsync(string barcodeData)
        {
            // Generate the barcode using ZXing.Net
            BarcodeWriter<Bitmap> writer = new BarcodeWriter<Bitmap>
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = 300,
                    Height = 100,
                    Margin = 10,
                    PureBarcode = false
                }
            };

            using (var bitmap = writer.Write(barcodeData))
            using (var stream = new MemoryStream())
            {
                // Save the bitmap to a stream as PNG
                bitmap.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }

        private async Task LogGateEventAsync(string eventType, object data)
        {
            try
            {
                var gateEvent = new GateEvent
                {
                    EventType = eventType,
                    EventTime = DateTime.Now,
                    EventData = JsonSerializer.Serialize(data ?? new { }),
                    IsProcessed = false
                };

                _context.GateEvents.Add(gateEvent);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging gate event");
                // Don't throw, just log the error
            }
        }

        private async Task CreatePendingVehicleEntryAsync(string ticketNumber)
        {
            try
            {
                // Create a pending vehicle entry
                var pendingEntry = new PendingVehicleEntry
                {
                    TicketNumber = ticketNumber,
                    CreatedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddHours(24), // Expires in 24 hours
                    Status = "Pending"
                };

                _context.PendingVehicleEntries.Add(pendingEntry);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating pending vehicle entry");
                // Don't throw, just log the error
            }
        }

        #endregion
    }

    public class GateEventModel
    {
        public string EventType { get; set; }
        public DateTime EventTime { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }

    public class PrintTicketRequest
    {
        public string TicketNumber { get; set; }
    }
} 