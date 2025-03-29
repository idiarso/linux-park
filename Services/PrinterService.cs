using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Data;
using ParkIRC.Models;
using System.IO.Ports;
using System.Text;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;

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
        private readonly string _printerName;
        private readonly string _printerPort;

        private const int BAUD_RATE = 9600;
        private const string DEFAULT_PORT = "/dev/ttyUSB0";
        private const int PRINT_TIMEOUT_MS = 5000;

        public PrinterService(
            ILogger<PrinterService> logger,
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

            _printerPort = _configuration["PrinterSettings:ComPort"] ?? DEFAULT_PORT;
            if (string.IsNullOrEmpty(_printerPort))
            {
                throw new ArgumentException("Printer port cannot be null or empty", nameof(_printerPort));
            }

            _serialPort = new SerialPort(_printerPort, BAUD_RATE)
            {
                ReadTimeout = PRINT_TIMEOUT_MS,
                WriteTimeout = PRINT_TIMEOUT_MS
            };

            // Get printer name from config, or use default CUPS printer
            _printerName = _configuration["PrinterSettings:PrinterName"];
            if (string.IsNullOrEmpty(_printerName))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // Try to get default CUPS printer
                    try
                    {
                        var defaultPrinter = GetDefaultCupsPrinter();
                        _printerName = !string.IsNullOrEmpty(defaultPrinter) ? defaultPrinter : "EPSON-TM-T82";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get default CUPS printer. Using fallback name.");
                        _printerName = "EPSON-TM-T82";
                    }
                }
                else
                {
                    _printerName = "EPSON TM-T82";
                }
            }
            _logger.LogInformation("Using printer: {PrinterName}", _printerName);
        }

        private string GetDefaultCupsPrinter()
        {
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "lpstat",
                        Arguments = "-d",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    // Output format: "system default destination: printername"
                    var parts = output.Split(':');
                    if (parts.Length > 1)
                    {
                        return parts[1].Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting default CUPS printer");
            }
            return string.Empty;
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
                // Format ticket data
                var printData = FormatTicketForThermalPrinter(data);
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // Use lp command for Linux
                    var tempFile = Path.GetTempFileName();
                    await File.WriteAllTextAsync(tempFile, printData);
                    
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "lp",
                            Arguments = $"-d {_printerName} {tempFile}",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    await process.WaitForExitAsync();
                    
                    File.Delete(tempFile);
                    
                    return process.ExitCode == 0;
                }
                else
                {
                    // Send print command to Arduino for other platforms
                    await SendCommand("PRINT");
                    await SendCommand(printData);
                    
                    // Wait for print completion acknowledgment
                    var response = await ReadResponse();
                    return response == "OK";
                }
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

        public async Task<bool> PrintTicket(string ticketContent)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(ticketContent);

            await _printLock.WaitAsync();
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // Use lp command for Linux
                    var tempFile = Path.GetTempFileName();
                    await File.WriteAllTextAsync(tempFile, ticketContent);
                    
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "lp",
                            Arguments = $"-d {_printerName} {tempFile}",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    await process.WaitForExitAsync();
                    
                    File.Delete(tempFile);
                    
                    return process.ExitCode == 0;
                }
                else
                {
                    // Send print command to Arduino for other platforms
                    await SendCommand("PRINT");
                    await SendCommand(ticketContent);
                    
                    // Wait for print completion acknowledgment
                    var response = await ReadResponse();
                    return response == "OK";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing ticket content");
                return false;
            }
            finally
            {
                _printLock.Release();
            }
        }

        public string GetPrinterPort()
        {
            ThrowIfDisposed();
            return _printerPort;
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

        public async Task<bool> PrintTicketWithBarcode(string ticketNumber, DateTime entryTime, string barcodeImagePath)
        {
            ThrowIfDisposed();
            await _printLock.WaitAsync();
            try
            {
                // Initialize printer
                await InitializePrinter();

                // Format ticket data
                var ticketData = new StringBuilder();
                ticketData.AppendLine("\x1B@");  // Initialize printer
                ticketData.AppendLine("\x1B!1"); // Emphasized mode
                ticketData.AppendLine("TIKET PARKIR");
                ticketData.AppendLine("================");
                ticketData.AppendLine("\x1B!0"); // Normal mode
                ticketData.AppendLine($"No: {ticketNumber}");
                ticketData.AppendLine($"Tanggal: {entryTime:dd/MM/yyyy}");
                ticketData.AppendLine($"Jam: {entryTime:HH:mm:ss}");

                // Add barcode if available
                if (File.Exists(barcodeImagePath))
                {
                    using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(barcodeImagePath);
                    image.Mutate(x => x.Resize(384, 0)); // Resize to thermal printer width
                    using var ms = new MemoryStream();
                    image.Save(ms, new PngEncoder());
                    var imageBytes = ms.ToArray();
                    
                    // Convert image to printer format
                    var printerImageData = ConvertImageToPrinterFormat(imageBytes);
                    ticketData.Append(printerImageData);
                }

                ticketData.AppendLine("\n\n\n"); // Feed lines
                ticketData.AppendLine("\x1DV1"); // Cut paper

                // Send data to printer
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                }
                _serialPort.Write(ticketData.ToString());

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing ticket {TicketNumber}", ticketNumber);
                return false;
            }
            finally
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
                _printLock.Release();
            }
        }

        private async Task InitializePrinter()
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                }

                // Send initialization sequence
                _serialPort.Write(new byte[] { 0x1B, 0x40 }, 0, 2); // ESC @
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing printer");
                throw;
            }
        }

        private string ConvertImageToPrinterFormat(byte[] imageData)
        {
            // This is a simplified example - you'll need to implement proper
            // image conversion for your specific thermal printer model
            var sb = new StringBuilder();
            sb.Append("\x1B*"); // Enter bit image mode
            // Add your printer-specific image conversion logic here
            return sb.ToString();
        }

        public async Task<bool> TestPrinter()
        {
            ThrowIfDisposed();
            await _printLock.WaitAsync();
            try
            {
                await InitializePrinter();
                var testData = new StringBuilder();
                testData.AppendLine("\x1B@");  // Initialize printer
                testData.AppendLine("\x1B!1"); // Emphasized mode
                testData.AppendLine("TEST PRINT");
                testData.AppendLine("==========");
                testData.AppendLine("\x1B!0"); // Normal mode
                testData.AppendLine("Printer OK");
                testData.AppendLine("\n\n\n"); // Feed lines
                testData.AppendLine("\x1DV1"); // Cut paper

                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                }
                _serialPort.Write(testData.ToString());

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing printer");
                return false;
            }
            finally
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
                _printLock.Release();
            }
        }
    }

    public class TicketData
    {
        public string TicketNumber { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public string EntryTime { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string SpaceNumber { get; set; } = string.Empty;
    }
}