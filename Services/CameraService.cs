using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using ParkIRC.Hardware;
using ParkIRC.Models;
using ParkIRC.Data;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

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
        private readonly IHardwareManager _hardwareManager;
        private readonly ApplicationDbContext _context;
        private bool _isInitialized;
        private VideoCapture? _videoCapture;
        private readonly string _imageSavePath;
        private bool _disposed;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private readonly SemaphoreSlim _captureLock = new(1, 1);

        public CameraService(
            ILogger<CameraService> logger,
            IHardwareManager hardwareManager,
            ApplicationDbContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hardwareManager = hardwareManager ?? throw new ArgumentNullException(nameof(hardwareManager));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            
            _isInitialized = false;
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
                
                // Initialize video capture
                _videoCapture = new VideoCapture(0); // Default webcam
                if (!_videoCapture.IsOpened())
                {
                    throw new HardwareException("Failed to initialize camera");
                }

                // Set camera properties
                _videoCapture.Set(VideoCaptureProperties.FrameWidth, 640);
                _videoCapture.Set(VideoCaptureProperties.FrameHeight, 480);
                _videoCapture.Set(VideoCaptureProperties.Fps, 30);

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
                
                if (_videoCapture == null)
                {
                    throw new InvalidOperationException("Video capture device is not initialized");
                }

                using (var mat = new Mat())
                {
                    if (!_videoCapture.Read(mat))
                    {
                        throw new InvalidOperationException("Failed to capture frame from camera");
                    }

                    // Convert Mat to byte array
                    byte[] imageData = mat.ToBytes();
                    
                    // Create ImageSharp image from byte array
                    using var image = Image.Load<Rgba32>(imageData);
                    
                    // Save the image
                    string fileName = $"capture_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                    string fullPath = Path.Combine(_imageSavePath, fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                    
                    using var stream = File.Create(fullPath);
                    image.Save(stream, new JpegEncoder());
                    return fullPath;
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
                    _videoCapture.Set(VideoCaptureProperties.FrameWidth, settings.ResolutionWidth);
                    _videoCapture.Set(VideoCaptureProperties.FrameHeight, settings.ResolutionHeight);
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
                
                if (_videoCapture == null || !_videoCapture.IsOpened())
                {
                    throw new HardwareException("Camera is not initialized");
                }

                using var frame = new Mat();
                if (!_videoCapture.Read(frame))
                {
                    throw new HardwareException("Failed to capture frame from camera");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test camera connection");
                return false;
            }
        }

        public async Task<byte[]> CaptureImage()
        {
            if (_videoCapture == null)
            {
                throw new InvalidOperationException("Video capture device is not initialized");
            }

            // Use SemaphoreSlim to prevent multiple simultaneous captures
            await _captureLock.WaitAsync();
            try
            {
                _logger.LogInformation("Capturing image as byte array...");
                
                if (_videoCapture == null)
                {
                    throw new InvalidOperationException("Video capture device is not initialized");
                }

                using (var mat = new Mat())
                {
                    if (!_videoCapture.Read(mat))
                    {
                        throw new InvalidOperationException("Failed to capture frame from camera");
                    }

                    // Convert Mat to byte array
                    byte[] imageData = mat.ToBytes();
                    
                    // Create ImageSharp image from byte array
                    using var image = Image.Load<Rgba32>(imageData);
                    
                    // Save the image to MemoryStream
                    using var ms = new MemoryStream();
                    image.Save(ms, new JpegEncoder());
                    return ms.ToArray();
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