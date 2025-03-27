using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Data;
using ParkIRC.Models;

namespace ParkIRC.Services
{
    public class PrinterService : IPrinterService, IAsyncDisposable
    {
        private readonly ILogger<PrinterService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SerialPort _serialPort;
        private readonly SemaphoreSlim _printLock = new(1, 1);
        private bool _disposed;

        private const int BAUD_RATE = 9600;
        private const string DEFAULT_PORT = "COM3";
        private const int PRINT_TIMEOUT_MS = 5000;

        public PrinterService(
            ILogger<PrinterService> logger,
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

            var port = _configuration["PrinterSettings:ComPort"] ?? DEFAULT_PORT;
            if (string.IsNullOrEmpty(port))
            {
                throw new ArgumentException("Printer port cannot be null or empty", nameof(port));
            }

            _serialPort = new SerialPort(port, BAUD_RATE)
            {
                ReadTimeout = PRINT_TIMEOUT_MS,
                WriteTimeout = PRINT_TIMEOUT_MS
            };
        }

        public async Task InitializeAsync()
        {
            ThrowIfDisposed();
            
            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                    _logger.LogInformation("Printer port {Port} opened successfully", _serialPort.PortName);
                    
                    // Send initialization command to Arduino
                    await SendCommand("INIT");
                    
                    // Wait for acknowledgment
                    var response = await ReadResponse();
                    if (response != "OK")
                    {
                        throw new Exception($"Printer initialization failed: {response}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing printer on port {Port}", _serialPort.PortName);
                throw;
            }
        }

        public async Task<bool> PrintTicketAsync(string ticketNumber, string entryTime)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(ticketNumber);
            ArgumentNullException.ThrowIfNull(entryTime);

            await _printLock.WaitAsync();
            try
            {
                var ticketData = new TicketData
                {
                    TicketNumber = ticketNumber,
                    EntryTime = entryTime,
                    VehicleNumber = "Unknown",
                    Barcode = ticketNumber
                };

                return await PrintTicketData(ticketData);
            }
            finally
            {
                _printLock.Release();
            }
        }

        public async Task<bool> PrintReceiptAsync(string ticketNumber, string exitTime, decimal amount)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(ticketNumber);
            ArgumentNullException.ThrowIfNull(exitTime);

            await _printLock.WaitAsync();
            try
            {
                var receiptData = new TicketData
                {
                    TicketNumber = ticketNumber,
                    EntryTime = exitTime,
                    VehicleNumber = "Unknown",
                    Barcode = ticketNumber
                };

                return await PrintTicketData(receiptData);
            }
            finally
            {
                _printLock.Release();
            }
        }

        public async Task<bool> PrintEntryTicketAsync(string ticketNumber)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(ticketNumber);

            await _printLock.WaitAsync();
            try
            {
                var ticketData = new TicketData
                {
                    TicketNumber = ticketNumber,
                    EntryTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                    VehicleNumber = "Unknown",
                    Barcode = ticketNumber
                };

                return await PrintTicketData(ticketData);
            }
            finally
            {
                _printLock.Release();
            }
        }

        private async Task<bool> PrintTicketData(TicketData data)
        {
            try
            {
                // Format ticket data for thermal printer
                var printData = FormatTicketForThermalPrinter(data);
                
                // Send print command to Arduino
                await SendCommand("PRINT");
                await SendCommand(printData);
                
                // Wait for print completion acknowledgment
                var response = await ReadResponse();
                return response == "OK";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing ticket {TicketNumber}", data.TicketNumber);
                return false;
            }
        }

        private string FormatTicketForThermalPrinter(TicketData data)
        {
            var sb = new StringBuilder();
            
            // Center align header
            sb.AppendLine("<center>TIKET PARKIR</center>");
            sb.AppendLine("<bold>================</bold>");
            
            // Ticket details
            sb.AppendLine($"No: {data.TicketNumber}");
            sb.AppendLine($"Waktu: {data.EntryTime}");
            if (!string.IsNullOrEmpty(data.VehicleNumber))
            {
                sb.AppendLine($"Kendaraan: {data.VehicleNumber}");
            }
            
            // Add barcode
            sb.AppendLine("<barcode>");
            sb.AppendLine(data.Barcode);
            sb.AppendLine("</barcode>");
            
            // Footer
            sb.AppendLine("<center>================</center>");
            sb.AppendLine("<cut>"); // Thermal printer cut command

            return sb.ToString();
        }

        private async Task SendCommand(string command)
        {
            if (!_serialPort.IsOpen)
            {
                throw new InvalidOperationException("Serial port is not open");
            }

            var data = Encoding.ASCII.GetBytes(command + "\n");
            await _serialPort.BaseStream.WriteAsync(data);
            await _serialPort.BaseStream.FlushAsync();
        }

        private async Task<string> ReadResponse()
        {
            var buffer = new byte[1024];
            var sb = new StringBuilder();
            
            try
            {
                while (true)
                {
                    var count = await _serialPort.BaseStream.ReadAsync(buffer);
                    if (count == 0) break;
                    
                    var text = Encoding.ASCII.GetString(buffer, 0, count);
                    sb.Append(text);
                    
                    if (text.Contains('\n'))
                    {
                        break;
                    }
                }
                
                return sb.ToString().Trim();
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Timeout waiting for printer response");
                return "TIMEOUT";
            }
        }

        public async Task<bool> CheckPrinterStatusAsync()
        {
            ThrowIfDisposed();
            
            try
            {
                if (!_serialPort.IsOpen)
                {
                    return false;
                }

                // Send status check command
                await SendCommand("STATUS");
                var response = await ReadResponse();
                return response?.Trim() == "OK";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking printer status");
                return false;
            }
        }

        public async Task<bool> PrintTicket(string ticketNumber, string entryTime)
        {
            return await PrintTicketAsync(ticketNumber, entryTime);
        }

        public async Task<bool> PrintReceipt(string ticketNumber, string exitTime, decimal amount)
        {
            return await PrintReceiptAsync(ticketNumber, exitTime, amount);
        }

        public string GetPrinterPort()
        {
            ThrowIfDisposed();
            return _serialPort.PortName;
        }

        public async Task<IEnumerable<(string id, string status)>> GetAllPrinterStatus()
        {
            var statuses = new List<(string id, string status)>();
            try
            {
                // Get all configured printers
                var printers = await GetConfiguredPrintersAsync();
                foreach (var printer in printers)
                {
                    var status = await GetPrinterStatusAsync(printer.Id);
                    statuses.Add((printer.Id, status));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting printer statuses");
            }
            return statuses;
        }

        private async Task<IEnumerable<PrinterConfig>> GetConfiguredPrintersAsync()
        {
            var printers = new List<PrinterConfig>();
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var devices = await dbContext.Devices
                    .Where(d => d.Type == "Printer" && d.IsActive)
                    .ToListAsync();

                foreach (var device in devices)
                {
                    printers.Add(new PrinterConfig
                    {
                        Id = device.Id.ToString(),
                        Name = device.Name,
                        Port = device.Port,
                        IpAddress = device.IpAddress
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configured printers");
            }
            return printers;
        }

        private async Task<string> GetPrinterStatusAsync(string printerId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var device = await dbContext.Devices
                    .FirstOrDefaultAsync(d => d.Id.ToString() == printerId && d.Type == "Printer");

                if (device == null)
                    return "Not Found";

                return device.Status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting printer status for {PrinterId}", printerId);
                return "Error";
            }
        }

        private class PrinterConfig
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public int Port { get; set; }
            public string IpAddress { get; set; } = string.Empty;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PrinterService));
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;

                if (_serialPort.IsOpen)
                {
                    try
                    {
                        await SendCommand("CLOSE");
                        _serialPort.Close();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error closing printer port");
                    }
                }
                
                _serialPort.Dispose();
                _printLock.Dispose();
            }
        }
    }

    public class TicketData
    {
        public string TicketNumber { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public string EntryTime { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
    }
}