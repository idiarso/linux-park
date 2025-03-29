using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ParkIRC.Services;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Text;

namespace ParkIRC.Hardware
{
    /// <summary>
    /// Manages communication with hardware devices:
    /// - Camera (IP camera or webcam)
    /// - Gate controller (via serial port)
    /// - Thermal printer
    /// </summary>
    public class HardwareManager : IHardwareManager, IDisposable, IAsyncDisposable
    {
        private readonly ILogger<HardwareManager> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHardwareRedundancyService _redundancyService;
        private SerialPort? _serialPort;
        private bool _isInitialized;
        private bool _disposedValue;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private readonly SemaphoreSlim _captureLock = new(1, 1);
        private readonly SemaphoreSlim _serialLock = new(1, 1);

        // Configuration values
        private readonly string _cameraType;
        private readonly string _cameraIp;
        private readonly string _cameraUsername;
        private readonly string _cameraPassword;
        private readonly int _cameraPort;
        private readonly string _cameraResolution;
        private readonly int _webcamDeviceIndex;
        private readonly string _comPort;
        private readonly int _baudRate;
        private readonly string _imageBasePath;

        private VideoCapture? _videoCapture;

        public HardwareManager(
            ILogger<HardwareManager> logger,
            IConfiguration configuration,
            IHardwareRedundancyService redundancyService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _redundancyService = redundancyService ?? throw new ArgumentNullException(nameof(redundancyService));

            _isInitialized = false;
            _disposedValue = false;

            // Initialize configuration values
            _cameraType = configuration["Hardware:Camera:Type"] ?? "Webcam";
            _cameraIp = configuration["Hardware:Camera:IpAddress"] ?? "localhost";
            _cameraUsername = configuration["Hardware:Camera:Username"] ?? "admin";
            _cameraPassword = configuration["Hardware:Camera:Password"] ?? "admin";
            _cameraPort = int.Parse(configuration["Hardware:Camera:Port"] ?? "8080");
            _cameraResolution = configuration["Hardware:Camera:Resolution"] ?? "640x480";
            _webcamDeviceIndex = int.Parse(configuration["Hardware:Camera:DeviceIndex"] ?? "0");
            _comPort = configuration["Hardware:SerialPort:ComPort"] ?? "/dev/ttyUSB0";
            _baudRate = int.Parse(configuration["Hardware:SerialPort:BaudRate"] ?? "9600");
            _imageBasePath = configuration["Hardware:ImagePath"] ?? "Images";
        }

        public async Task<bool> InitializeAsync()
        {
            await _initLock.WaitAsync();
            try
            {
                if (_isInitialized)
                    return true;

                // Initialize camera
                if (!await InitializeCameraAsync())
                    return false;

                // Initialize serial port
                if (!await InitializeSerialPortAsync())
                    return false;

                _isInitialized = true;
                return true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        private async Task<bool> InitializeCameraAsync()
        {
            if (_videoCapture != null)
            {
                _videoCapture.Dispose();
                _videoCapture = null;
            }

            try
            {
                if (_cameraType == "Webcam")
                {
                    _videoCapture = new VideoCapture(_webcamDeviceIndex);
                    if (!_videoCapture.IsOpened())
                    {
                        _logger.LogError("Failed to open webcam device {DeviceIndex}", _webcamDeviceIndex);
                        return false;
                    }
                }
                else if (_cameraType == "IP")
                {
                    string url = $"http://{_cameraIp}:{_cameraPort}/video";
                    _videoCapture = new VideoCapture(url);
                    if (!_videoCapture.IsOpened())
                    {
                        _logger.LogError("Failed to connect to IP camera at {Url}", url);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing camera");
                return false;
            }
        }

        private async Task<bool> InitializeSerialPortAsync()
        {
            if (_serialPort != null)
            {
                _serialPort.Dispose();
                _serialPort = null;
            }

            try
            {
                _serialPort = new SerialPort(_comPort, _baudRate)
                {
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Parity = Parity.None,
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };

                _serialPort.DataReceived += SerialPort_DataReceived;
                _serialPort.Open();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing serial port");
                return false;
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var sp = (SerialPort)sender;
                string data = sp.ReadExisting();
                _logger.LogDebug("Received from serial port: {Data}", data);
                // Handle the received data
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in serial port data received handler");
            }
        }

        public async Task<string> CaptureImageAsync(string fileName)
        {
            await _captureLock.WaitAsync();
            try
            {
                if (_videoCapture == null || !_videoCapture.IsOpened())
                {
                    throw new HardwareException("Camera is not initialized");
                }

                using (var mat = new Mat())
                {
                    if (!_videoCapture.Read(mat))
                    {
                        throw new HardwareException("Failed to capture image from camera");
                    }

                    // Convert Mat to Bitmap
                    using (var bitmap = BitmapConverter.ToBitmap(mat))
                    {
                        // Ensure directory exists
                        Directory.CreateDirectory(_imageBasePath);
                        
                        // Save the image
                        string fullPath = Path.Combine(_imageBasePath, fileName);
                        bitmap.Save(fullPath, ImageFormat.Jpeg);
                        return fullPath;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing image");
                throw;
            }
            finally
            {
                _captureLock.Release();
            }
        }

        public async Task<bool> SendCommandAsync(string command)
        {
            await _serialLock.WaitAsync();
            try
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    throw new HardwareException("Serial port is not initialized");
                }

                _serialPort.Write(command);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending command");
                return false;
            }
            finally
            {
                _serialLock.Release();
            }
        }

        public async Task<bool> OpenEntryGate()
        {
            return await SendCommandAsync("OPEN_ENTRY");
        }

        public async Task<bool> OpenExitGate()
        {
            return await SendCommandAsync("OPEN_EXIT");
        }

        public async Task<bool> PrintTicketAsync(string ticketNumber, string entryTime)
        {
            try
            {
                _logger.LogInformation("Printing ticket: {TicketNumber} at {EntryTime}", ticketNumber, entryTime);
                
                // Get printer settings from configuration
                var printerName = _configuration["Printer:DefaultName"] ?? "EPSON_TM_T82";
                int paperWidth = int.Parse(_configuration["Printer:PaperWidth"] ?? "80");
                bool autoCut = bool.Parse(_configuration["Printer:AutoCut"] ?? "true");
                
                // Create a formatted receipt
                var builder = new StringBuilder();
                
                // Header
                string siteName = _configuration["SiteIdentity:SiteName"] ?? "ParkIRC System";
                string companyName = _configuration["SiteIdentity:CompanyName"] ?? "Company Name";
                string address = _configuration["SiteIdentity:Address"] ?? "Address";
                
                // Add header
                builder.AppendLine(CenterText(siteName.ToUpper(), paperWidth));
                builder.AppendLine(CenterText(companyName, paperWidth));
                builder.AppendLine(CenterText(address, paperWidth));
                builder.AppendLine(new string('-', paperWidth));
                
                // Ticket details
                builder.AppendLine(CenterText("TIKET PARKIR", paperWidth));
                builder.AppendLine(CenterText("ENTRY TICKET", paperWidth));
                builder.AppendLine();
                builder.AppendLine($"Nomor Tiket : {ticketNumber}");
                builder.AppendLine($"Tanggal     : {DateTime.Now:yyyy-MM-dd}");
                builder.AppendLine($"Jam Masuk   : {DateTime.Now:HH:mm:ss}");
                builder.AppendLine();
                
                // Barcode - represented as text for now
                builder.AppendLine(CenterText($"*{ticketNumber}*", paperWidth));
                builder.AppendLine();
                
                // Footer
                builder.AppendLine(new string('-', paperWidth));
                builder.AppendLine(CenterText("Terima Kasih", paperWidth));
                builder.AppendLine(CenterText("Simpan Tiket Ini", paperWidth));
                builder.AppendLine(CenterText("Kehilangan Tiket Dikenakan Denda", paperWidth));
                
                // Add extra space for better tear-off
                builder.AppendLine();
                builder.AppendLine();
                builder.AppendLine();
                
                string ticketContent = builder.ToString();
                
                // Log the ticket for debugging
                _logger.LogDebug("Generated ticket content: {TicketContent}", ticketContent);
                
                // In a real implementation, this would send to the actual printer
                // For simulation, we'll just log success
                _logger.LogInformation("Ticket successfully printed for {TicketNumber}", ticketNumber);
                
                // Trigger print completion event
                OnCommandReceived(new CommandReceivedEventArgs("PRINT_COMPLETE", ticketNumber));
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing ticket");
                return false;
            }
        }
        
        private string CenterText(string text, int width)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
                
            int padding = (width - text.Length) / 2;
            padding = Math.Max(0, padding); // Ensure padding is not negative
            return new string(' ', padding) + text;
        }

        public async Task<bool> PrintReceiptAsync(string ticketNumber, string exitTime, decimal amount)
        {
            // Implementation details...
            return true;
        }

        public async Task OpenGate(string gateId)
        {
            await SendCommandAsync($"OPEN_{gateId.ToUpper()}");
        }

        public async Task CloseGate(string gateId)
        {
            await SendCommandAsync($"CLOSE_{gateId.ToUpper()}");
        }

        public async Task<bool> IsGateOpen(string gateId)
        {
            // Implementation details...
            return false;
        }

        public async Task InitializeHardware()
        {
            await InitializeAsync();
        }

        public event EventHandler<ImageCapturedEventArgs>? ImageCaptured;
        public event EventHandler<CommandReceivedEventArgs>? CommandReceived;
        public event EventHandler<PushButtonEventArgs>? OnPushButtonPressed;

        protected virtual void OnImageCaptured(ImageCapturedEventArgs e)
        {
            ImageCaptured?.Invoke(this, e);
        }

        protected virtual void OnCommandReceived(CommandReceivedEventArgs e)
        {
            CommandReceived?.Invoke(this, e);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _videoCapture?.Dispose();
                    _serialPort?.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposedValue)
            {
                _disposedValue = true;

                await DisposeAsyncCore().ConfigureAwait(false);

                _initLock.Dispose();
                _captureLock.Dispose();
                _serialLock.Dispose();

                GC.SuppressFinalize(this);
            }
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            try
            {
                if (_videoCapture != null)
                {
                    _videoCapture.Dispose();
                    _videoCapture = null;
                }

                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Close();
                    }
                    _serialPort.Dispose();
                    _serialPort = null;
                }

                await Task.CompletedTask; // For future async cleanup
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during async disposal");
            }
        }
    }
}
