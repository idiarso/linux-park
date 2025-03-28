using System;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Logging;
using ParkIRC.Models;
using ParkIRC.Data;
using Microsoft.EntityFrameworkCore;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Extensions.Configuration;

namespace ParkIRC.Services
{
    public interface ICameraService : IAsyncDisposable
    {
        Task<bool> InitializeAsync();
        Task<string> CaptureImageAsync();
        Task<CameraSettings> GetSettingsAsync();
        Task<bool> UpdateSettingsAsync(CameraSettings settings);
        Task<bool> TestConnectionAsync();
        bool IsConnected();
        Task<byte[]> CaptureImage();
    }

    public class CameraService : ICameraService, IAsyncDisposable
    {
        private readonly ILogger<CameraService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private bool _isInitialized;
        private VideoCapture? _videoCapture;
        private readonly string _cameraType;
        private CameraSettings? _currentSettings;
        private readonly string _imageSavePath;
        private bool _disposed;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private readonly SemaphoreSlim _captureLock = new(1, 1);

        public CameraService(
            ILogger<CameraService> logger,
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            
            _isInitialized = false;
            _cameraType = configuration.GetValue<string>("Camera:Type", "Webcam");
            _imageSavePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "camera");
            Directory.CreateDirectory(_imageSavePath);
        }

        public bool IsConnected()
        {
            ThrowIfDisposed();
            return _isInitialized;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;

                if (_videoCapture != null)
                {
                    _videoCapture.Dispose();
                    _videoCapture = null;
                }

                _initLock.Dispose();
                _captureLock.Dispose();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CameraService));
            }
        }

        public async Task<bool> InitializeAsync()
        {
            ThrowIfDisposed();

            // Use SemaphoreSlim to prevent multiple simultaneous initializations
            await _initLock.WaitAsync();
            try
            {
                _logger.LogInformation("Initializing camera service...");
                _currentSettings = await GetSettingsAsync();

                if (_cameraType.Equals("Webcam", StringComparison.OrdinalIgnoreCase))
                {
                    int deviceIndex = _configuration.GetValue<int>("Camera:DeviceIndex", 0);
                    _videoCapture = new VideoCapture(deviceIndex);
                    
                    if (_videoCapture == null || !_videoCapture.IsOpened)
                    {
                        throw new InvalidOperationException("No video devices found");
                    }

                    _videoCapture.Set(CapProp.FrameWidth, _currentSettings.ResolutionWidth);
                    _videoCapture.Set(CapProp.FrameHeight, _currentSettings.ResolutionHeight);
                }

                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize camera service");
                return false;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task<string> CaptureImageAsync()
        {
            ThrowIfDisposed();

            if (!_isInitialized)
            {
                throw new InvalidOperationException("Camera service is not initialized");
            }

            // Use SemaphoreSlim to prevent multiple simultaneous captures
            await _captureLock.WaitAsync();
            try
            {
                _logger.LogInformation("Capturing image...");
                
                if (_cameraType.Equals("Webcam", StringComparison.OrdinalIgnoreCase))
                {
                    if (_videoCapture == null)
                    {
                        throw new InvalidOperationException("Video capture device is not initialized");
                    }

                    using var frame = _videoCapture.QueryFrame();
                    if (frame == null)
                    {
                        throw new InvalidOperationException("Failed to capture frame from camera");
                    }

                    string fileName = $"capture_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                    string filePath = Path.Combine(_imageSavePath, fileName);
                    
                    await Task.Run(() => frame.Save(filePath));
                    return filePath;
                }
                else
                {
                    throw new NotImplementedException("IP camera capture not implemented");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to capture image");
                throw;
            }
            finally
            {
                _captureLock.Release();
            }
        }

        public async Task<CameraSettings> GetSettingsAsync()
        {
            ThrowIfDisposed();

            try
            {
                var settings = await _context.CameraSettings
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefaultAsync();

                return settings ?? new CameraSettings
                {
                    ProfileName = "Default",
                    ResolutionWidth = 1280,
                    ResolutionHeight = 720,
                    ROI_X = 0,
                    ROI_Y = 0,
                    ROI_Width = 640,
                    ROI_Height = 480,
                    Exposure = 100,
                    Gain = 1.0,
                    Brightness = 50,
                    Contrast = 50,
                    LightingCondition = "Normal",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get camera settings");
                throw;
            }
        }

        public async Task<bool> UpdateSettingsAsync(CameraSettings settings)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(settings);

            try
            {
                settings.ModifiedAt = DateTime.UtcNow;
                _context.CameraSettings.Update(settings);
                await _context.SaveChangesAsync();

                // Update current settings if initialized
                if (_isInitialized && _videoCapture != null)
                {
                    _currentSettings = settings;
                    _videoCapture.Set(CapProp.FrameWidth, settings.ResolutionWidth);
                    _videoCapture.Set(CapProp.FrameHeight, settings.ResolutionHeight);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update camera settings");
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            ThrowIfDisposed();

            try
            {
                _logger.LogInformation("Testing camera connection...");
                
                if (_cameraType.Equals("Webcam", StringComparison.OrdinalIgnoreCase))
                {
                    int deviceIndex = _configuration.GetValue<int>("Camera:DeviceIndex", 0);
                    
                    using var testCapture = new VideoCapture(deviceIndex);
                    if (!testCapture.IsOpened)
                    {
                        return false;
                    }

                    using var frame = await Task.Run(() => testCapture.QueryFrame());
                    return frame != null;
                }
                else
                {
                    throw new NotImplementedException("IP camera test not implemented");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test camera connection");
                return false;
            }
        }

        public async Task<byte[]> CaptureImage()
        {
            if (_videoCapture == null && _cameraType.Equals("Webcam", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Video capture device is not initialized");
            }

            // Use SemaphoreSlim to prevent multiple simultaneous captures
            await _captureLock.WaitAsync();
            try
            {
                _logger.LogInformation("Capturing image as byte array...");
                
                if (_cameraType.Equals("Webcam", StringComparison.OrdinalIgnoreCase))
                {
                    if (_videoCapture == null)
                    {
                        throw new InvalidOperationException("Video capture device is not initialized");
                    }

                    using var frame = _videoCapture.QueryFrame();
                    if (frame == null)
                    {
                        throw new InvalidOperationException("Failed to capture frame from camera");
                    }

                    using var ms = new MemoryStream();
                    using var bitmap = new Bitmap(frame.Width, frame.Height, PixelFormat.Format24bppRgb);
                    
                    // Lock bits to directly access bitmap data
                    var bitmapData = bitmap.LockBits(
                        new Rectangle(0, 0, frame.Width, frame.Height),
                        ImageLockMode.WriteOnly,
                        PixelFormat.Format24bppRgb);
                    
                    try
                    {
                        // Calculate the size of the data
                        int dataSize = frame.Width * frame.Height * frame.NumberOfChannels;
                        
                        // Create a byte array to hold the image data
                        byte[] imageData = new byte[dataSize];
                        
                        // Copy data from Mat to byte array
                        System.Runtime.InteropServices.Marshal.Copy(
                            frame.DataPointer, 
                            imageData, 
                            0, 
                            dataSize);
                        
                        // Copy byte array to bitmap
                        System.Runtime.InteropServices.Marshal.Copy(
                            imageData, 
                            0, 
                            bitmapData.Scan0, 
                            dataSize);
                    }
                    finally
                    {
                        // Unlock the bitmap bits
                        bitmap.UnlockBits(bitmapData);
                    }
                    
                    bitmap.Save(ms, ImageFormat.Jpeg);
                    return ms.ToArray();
                }
                else
                {
                    throw new NotImplementedException("IP camera capture not implemented");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to capture image as byte array");
                throw;
            }
            finally
            {
                _captureLock.Release();
            }
        }
    }
}