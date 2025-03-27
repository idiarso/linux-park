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
using DirectShowLib;
using static DirectShowLib.DsGuid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ParkIRC.Services;

namespace ParkIRC.Hardware
{
    /// <summary>
    /// Manages communication with hardware devices:
    /// - Camera (IP camera or webcam)
    /// - Gate controller (via serial port)
    /// - Thermal printer
    /// </summary>
    public class HardwareManager : IHardwareManager, IDisposable
    {
        private static volatile HardwareManager? _instance;
        private static readonly object _syncRoot = new object();

        private static ILogger<HardwareManager>? _loggerInstance;
        private static IConfiguration? _configurationInstance;
        private static IHardwareRedundancyService? _redundancyServiceInstance;

        public static HardwareManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncRoot)
                    {
                        if (_instance == null)
                        {
                            if (_loggerInstance == null || _configurationInstance == null || _redundancyServiceInstance == null)
                            {
                                throw new InvalidOperationException("HardwareManager not initialized. Call Initialize first.");
                            }
                            _instance = new HardwareManager(_loggerInstance, _configurationInstance, _redundancyServiceInstance);
                        }
                    }
                }
                return _instance;
            }
        }

        public static void Initialize(
            ILogger<HardwareManager> logger,
            IConfiguration configuration,
            IHardwareRedundancyService redundancyService)
        {
            _loggerInstance = logger;
            _configurationInstance = configuration;
            _redundancyServiceInstance = redundancyService;
        }

        private readonly ILogger<HardwareManager> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHardwareRedundancyService _redundancyService;
        private SerialPort? _serialPort;
        private IFilterGraph2? _graphBuilder;
        private IMediaControl? _mediaControl;
        private IBaseFilter? _videoDevice;
        private ISampleGrabber? _sampleGrabber;
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
        private readonly string _entrySubfolder;
        private readonly string _exitSubfolder;
        private readonly string _filenameFormat;

        // Events
        public event EventHandler<ImageCapturedEventArgs>? ImageCaptured;
        public event EventHandler<CommandReceivedEventArgs>? CommandReceived;
        public event EventHandler<PushButtonEventArgs>? OnPushButtonPressed;

        public HardwareManager(
            ILogger<HardwareManager> logger,
            IConfiguration configuration,
            IHardwareRedundancyService redundancyService)
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(redundancyService);

            _logger = logger;
            _configuration = configuration;
            _redundancyService = redundancyService;
            _isInitialized = false;
            _disposedValue = false;

            // Load configuration from appsettings.json
            _cameraType = _configuration["Hardware:Camera:Type"] ?? "Webcam";
            _cameraIp = _configuration["Hardware:Camera:IP"] ?? "127.0.0.1";
            _cameraUsername = _configuration["Hardware:Camera:Username"] ?? "admin";
            _cameraPassword = _configuration["Hardware:Camera:Password"] ?? "admin";
            _cameraPort = int.Parse(_configuration["Hardware:Camera:Port"] ?? "8080");
            _cameraResolution = _configuration["Hardware:Camera:Resolution"] ?? "640x480";
            _webcamDeviceIndex = int.Parse(_configuration["Hardware:Camera:WebcamIndex"] ?? "0");
            _comPort = _configuration["Hardware:Gate:ComPort"] ?? "COM1";
            _baudRate = int.Parse(_configuration["Hardware:Gate:BaudRate"] ?? "9600");
            _imageBasePath = _configuration["Storage:ImagePath"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
            _entrySubfolder = _configuration["Storage:EntrySubfolder"] ?? "Entry";
            _exitSubfolder = _configuration["Storage:ExitSubfolder"] ?? "Exit";
            _filenameFormat = "{0}_{1:yyyyMMdd_HHmmss}";
        }

        private void ThrowIfDisposed()
        {
            if (_disposedValue)
            {
                throw new ObjectDisposedException(nameof(HardwareManager));
            }
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
                if (_mediaControl != null)
                {
                    _mediaControl.Stop();
                    Marshal.ReleaseComObject(_mediaControl);
                    _mediaControl = null;
                }

                if (_videoDevice != null)
                {
                    Marshal.ReleaseComObject(_videoDevice);
                    _videoDevice = null;
                }

                if (_sampleGrabber != null)
                {
                    Marshal.ReleaseComObject(_sampleGrabber);
                    _sampleGrabber = null;
                }

                if (_graphBuilder != null)
                {
                    Marshal.ReleaseComObject(_graphBuilder);
                    _graphBuilder = null;
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

        public async Task<bool> InitializeAsync()
        {
            ThrowIfDisposed();

            await _initLock.WaitAsync();
            try
            {
                if (_isInitialized)
                    return true;

                bool success = await _redundancyService.ExecuteWithRetryAsync<HardwareManager>(
                    async () =>
                    {
                        await InitializeSerialPortAsync();
                        await InitializeCameraAsync();
                        await EnsureDirectoriesExistAsync();
                        return true;
                    },
                    "Initialize",
                    this);

                if (success)
                {
                    _isInitialized = true;
                    _logger.LogInformation("Hardware manager initialized successfully");
                }
                else
                {
                    _logger.LogError("Failed to initialize hardware after retries");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize hardware");
                return false;
            }
            finally
            {
                _initLock.Release();
            }
        }

        private async Task InitializeSerialPortAsync()
        {
            await _serialLock.WaitAsync();
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
                
                await Task.Run(() => _serialPort.Open());
                _logger.LogInformation("Connected to serial port {Port}", _comPort);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize serial port {Port}", _comPort);
                throw;
            }
            finally
            {
                _serialLock.Release();
            }
        }

        private async Task InitializeCameraAsync()
        {
            try
            {
                if (_redundancyService.IsUsingBackupDevice("camera"))
                {
                    await InitializeBackupCameraAsync();
                    return;
                }

                _graphBuilder = (IFilterGraph2)new FilterGraph();
                _mediaControl = (IMediaControl)_graphBuilder;

                // Create and add sample grabber
                _sampleGrabber = (ISampleGrabber)new SampleGrabber();
                var sampleGrabberFilter = (IBaseFilter)_sampleGrabber;
                var hr = _graphBuilder.AddFilter(sampleGrabberFilter, "Sample Grabber");
                DsError.ThrowExceptionForHR(hr);

                // Get first video device
                _videoDevice = await GetFirstVideoCaptureDeviceAsync();
                if (_videoDevice == null)
                {
                    throw new InvalidOperationException("No video capture device found");
                }

                hr = _graphBuilder.AddFilter(_videoDevice, "Video Capture Device");
                DsError.ThrowExceptionForHR(hr);

                // Set media type
                var mediaType = new AMMediaType
                {
                    majorType = MediaType.Video,
                    subType = MediaSubType.RGB24,
                    formatType = FormatType.VideoInfo
                };
                hr = _sampleGrabber.SetMediaType(mediaType);
                DsError.ThrowExceptionForHR(hr);

                // Connect filters
                var pinOut = DsFindPin.ByDirection(_videoDevice, PinDirection.Output, 0);
                var pinIn = DsFindPin.ByDirection(sampleGrabberFilter, PinDirection.Input, 0);
                hr = _graphBuilder.Connect(pinOut, pinIn);
                DsError.ThrowExceptionForHR(hr);

                // Start capturing
                hr = _mediaControl.Run();
                DsError.ThrowExceptionForHR(hr);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing camera");
                throw;
            }
        }

        private async Task InitializeBackupCameraAsync()
        {
            // Implementation for backup camera initialization
            // This could be an IP camera or a different webcam
            _logger.LogInformation("Initializing backup camera");
            // Add backup camera initialization logic here
        }

        private async Task<IBaseFilter?> GetFirstVideoCaptureDeviceAsync()
        {
            return await Task.Run(() =>
            {
                var devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
                if (devices.Length == 0)
                {
                    return null;
                }

                var device = devices[0];
                object? source = null;
                var iid = typeof(IBaseFilter).GUID;
                device.Mon.BindToObject(null, null, ref iid, out source);
                return source as IBaseFilter;
            });
        }

        private async Task EnsureDirectoriesExistAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    Directory.CreateDirectory(Path.Combine(_imageBasePath, _entrySubfolder));
                    Directory.CreateDirectory(Path.Combine(_imageBasePath, _exitSubfolder));
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create directories");
                throw;
            }
        }

        public async Task<string> CaptureEntryImageAsync(string ticketId)
        {
            return await CaptureImageAsync(ticketId, true);
        }

        public async Task<string> CaptureExitImageAsync(string ticketId)
        {
            return await CaptureImageAsync(ticketId, false);
        }

        private async Task<string> CaptureImageAsync(string ticketId, bool isEntry)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(ticketId))
                throw new ArgumentException("Value cannot be null or empty.", nameof(ticketId));

            if (!_isInitialized)
            {
                throw new InvalidOperationException("Hardware manager is not initialized");
            }

            await _captureLock.WaitAsync();
            try
            {
                string? imagePath = null;
                bool success = await _redundancyService.ExecuteWithRetryAsync<HardwareManager>(
                    async () =>
                    {
                        try
                        {
                            if (_cameraType.Equals("Webcam", StringComparison.OrdinalIgnoreCase))
                            {
                                if (_sampleGrabber == null)
                                {
                                    throw new InvalidOperationException("Camera is not initialized");
                                }

                                // Get buffer size
                                int bufferSize = 0;
                                var hr = _sampleGrabber.GetCurrentBuffer(ref bufferSize, IntPtr.Zero);
                                DsError.ThrowExceptionForHR(hr);

                                // Create filename
                                string filename = string.Format(_filenameFormat, ticketId, DateTime.Now) + ".jpg";
                                string targetSubfolder = isEntry ? _entrySubfolder : _exitSubfolder;
                                imagePath = Path.Combine(_imageBasePath, targetSubfolder, filename);

                                // Allocate buffer and get image data
                                IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
                                try
                                {
                                    hr = _sampleGrabber.GetCurrentBuffer(ref bufferSize, buffer);
                                    DsError.ThrowExceptionForHR(hr);

                                    byte[] imageData = new byte[bufferSize];
                                    Marshal.Copy(buffer, imageData, 0, bufferSize);

                                    // Save image
                                    await Task.Run(() =>
                                    {
                                        using var bitmap = new Bitmap(640, 480, PixelFormat.Format24bppRgb);
                                        var bitmapData = bitmap.LockBits(
                                            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                            ImageLockMode.WriteOnly,
                                            PixelFormat.Format24bppRgb
                                        );

                                        try
                                        {
                                            Marshal.Copy(imageData, 0, bitmapData.Scan0, bufferSize);
                                        }
                                        finally
                                        {
                                            bitmap.UnlockBits(bitmapData);
                                        }

                                        bitmap.Save(imagePath, ImageFormat.Jpeg);
                                    });

                                    OnImageCaptured(new ImageCapturedEventArgs(ticketId, imagePath, isEntry));
                                    return true;
                                }
                                finally
                                {
                                    Marshal.FreeHGlobal(buffer);
                                }
                            }
                            else
                            {
                                // Handle IP camera capture
                                throw new NotImplementedException("IP camera capture not implemented");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error capturing image");
                            return false;
                        }
                    },
                    "CaptureImage",
                    this);

                if (!success || string.IsNullOrEmpty(imagePath))
                {
                    throw new InvalidOperationException("Failed to capture image after retries");
                }

                return imagePath;
            }
            finally
            {
                _captureLock.Release();
            }
        }

        public async Task<bool> OpenEntryGate()
        {
            ThrowIfDisposed();
            return await SendCommandAsync("OPEN_ENTRY");
        }

        public async Task<bool> OpenExitGate()
        {
            ThrowIfDisposed();
            return await SendCommandAsync("OPEN_EXIT");
        }

        public async Task<string> CaptureImageAsync(string location)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(location))
                throw new ArgumentException("Value cannot be null or empty.", nameof(location));

            bool isEntry = location.Equals("entry", StringComparison.OrdinalIgnoreCase);
            return await CaptureImageAsync(Guid.NewGuid().ToString(), isEntry);
        }

        public async Task<bool> PrintTicketAsync(string ticketNumber, string entryTime)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(ticketNumber))
                throw new ArgumentException("Value cannot be null or empty.", nameof(ticketNumber));
            if (string.IsNullOrEmpty(entryTime))
                throw new ArgumentException("Value cannot be null or empty.", nameof(entryTime));

            try
            {
                var command = $"PRINT_TICKET|{ticketNumber}|{entryTime}";
                return await SendCommandAsync(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing ticket {TicketNumber}", ticketNumber);
                return false;
            }
        }

        public async Task<bool> PrintReceiptAsync(string ticketNumber, string exitTime, decimal amount)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(ticketNumber))
                throw new ArgumentException("Value cannot be null or empty.", nameof(ticketNumber));
            if (string.IsNullOrEmpty(exitTime))
                throw new ArgumentException("Value cannot be null or empty.", nameof(exitTime));

            try
            {
                var command = $"PRINT_RECEIPT|{ticketNumber}|{exitTime}|{amount:F2}";
                return await SendCommandAsync(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing receipt {TicketNumber}", ticketNumber);
                return false;
            }
        }

        public async Task<bool> SendCommandAsync(string command)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(command))
                throw new ArgumentException("Value cannot be null or empty.", nameof(command));

            await _serialLock.WaitAsync();
            try
            {
                return await _redundancyService.ExecuteWithRetryAsync<HardwareManager>(
                    async () =>
                    {
                        if (_serialPort == null || !_serialPort.IsOpen)
                        {
                            _logger.LogError("Serial port is not initialized or not open");
                            return false;
                        }

                        await Task.Run(() => _serialPort.WriteLine(command));
                        return true;
                    },
                    $"SendCommand_{command}",
                    this);
            }
            finally
            {
                _serialLock.Release();
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_disposedValue || _serialPort == null)
                return;

            try
            {
                string data = _serialPort.ReadLine().Trim();
                if (string.IsNullOrEmpty(data))
                    return;

                string[] parts = data.Split(':');
                if (parts.Length != 2)
                    return;

                OnCommandReceived(new CommandReceivedEventArgs(parts[0], parts[1]));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing serial port data");
            }
        }

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
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task OpenGate(string gateId)
        {
            if (gateId.ToUpper() == "EXIT")
            {
                await SendCommandAsync("OPEN_EXIT_GATE");
            }
            else if (gateId.ToUpper() == "ENTRY")
            {
                await SendCommandAsync("OPEN_ENTRY_GATE");
            }
        }

        public async Task CloseGate(string gateId)
        {
            ThrowIfDisposed();
            await SendCommandAsync($"CLOSE_GATE|{gateId}");
        }

        public async Task<bool> IsGateOpen(string gateId)
        {
            ThrowIfDisposed();
            // Implementation depends on hardware capability to check gate status
            // For now, we'll assume gate is closed
            return false;
        }

        public async Task InitializeHardware()
        {
            await InitializeAsync();
        }
    }

    public class ImageCapturedEventArgs : EventArgs
    {
        public string TicketId { get; }
        public string ImagePath { get; }
        public bool IsEntryImage { get; }

        public ImageCapturedEventArgs(string ticketId, string imagePath, bool isEntryImage)
        {
            TicketId = ticketId;
            ImagePath = imagePath;
            IsEntryImage = isEntryImage;
        }
    }

    public class CommandReceivedEventArgs : EventArgs
    {
        public string Command { get; }
        public string Data { get; }

        public CommandReceivedEventArgs(string command, string data)
        {
            Command = command;
            Data = data;
        }
    }
}
