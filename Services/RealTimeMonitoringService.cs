using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Hardware;
using ParkIRC.Models;
using ParkIRC.Hubs;
using ParkIRC.Data;

namespace ParkIRC.Services
{
    public interface IRealTimeMonitoringService
    {
        Task StartMonitoringAsync(CancellationToken cancellationToken);
        Task<bool> IsLoopDetectorOccupiedAsync(string detectorId);
        Task<byte[]?> GetLatestCameraFrameAsync(string cameraId);
        Task<Dictionary<string, bool>> GetAllLoopStatesAsync();
        Task<Dictionary<string, CameraStatus>> GetAllCameraStatesAsync();
    }

    public class RealTimeMonitoringService : IRealTimeMonitoringService, IAsyncDisposable
    {
        private readonly ILogger<RealTimeMonitoringService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHardwareManager _hardwareManager;
        private readonly IHubContext<MonitoringHub> _hubContext;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHardwareRedundancyService _redundancyService;

        private readonly ConcurrentDictionary<string, bool> _loopStates;
        private readonly ConcurrentDictionary<string, CameraStatus> _cameraStates;
        private readonly ConcurrentDictionary<string, byte[]> _latestFrames;
        
        private readonly SemaphoreSlim _monitoringLock = new(1, 1);
        private readonly int _frameBufferSize;
        private readonly TimeSpan _loopCheckInterval;
        private readonly TimeSpan _cameraCheckInterval;
        private bool _isMonitoring;
        private bool _disposed;

        public RealTimeMonitoringService(
            ILogger<RealTimeMonitoringService> logger,
            IConfiguration configuration,
            IHardwareManager hardwareManager,
            IHubContext<MonitoringHub> hubContext,
            IServiceScopeFactory scopeFactory,
            IHardwareRedundancyService redundancyService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _hardwareManager = hardwareManager ?? throw new ArgumentNullException(nameof(hardwareManager));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _redundancyService = redundancyService ?? throw new ArgumentNullException(nameof(redundancyService));

            _loopStates = new ConcurrentDictionary<string, bool>();
            _cameraStates = new ConcurrentDictionary<string, CameraStatus>();
            _latestFrames = new ConcurrentDictionary<string, byte[]>();

            _frameBufferSize = _configuration.GetValue<int>("Monitoring:FrameBufferSize", 1024 * 1024); // 1MB default
            _loopCheckInterval = TimeSpan.FromMilliseconds(_configuration.GetValue<int>("Monitoring:LoopCheckIntervalMs", 100));
            _cameraCheckInterval = TimeSpan.FromMilliseconds(_configuration.GetValue<int>("Monitoring:CameraCheckIntervalMs", 33)); // ~30fps

            _isMonitoring = false;
            _disposed = false;
        }

        public async Task StartMonitoringAsync(CancellationToken cancellationToken)
        {
            await _monitoringLock.WaitAsync(cancellationToken);
            try
            {
                if (_isMonitoring)
                    return;

                _isMonitoring = true;

                // Start monitoring tasks
                _ = Task.Run(() => MonitorLoopDetectorsAsync(cancellationToken), cancellationToken);
                _ = Task.Run(() => MonitorCamerasAsync(cancellationToken), cancellationToken);

                _logger.LogInformation("Real-time monitoring started");
            }
            finally
            {
                _monitoringLock.Release();
            }
        }

        private async Task MonitorLoopDetectorsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _isMonitoring)
            {
                try
                {
                    await _redundancyService.ExecuteWithRetryAsync<RealTimeMonitoringService>(
                        async () =>
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                            // Get all configured loop detectors
                            var detectors = await dbContext.LoopDetectors
                                .Where(d => d.IsActive)
                                .ToListAsync(cancellationToken);

                            foreach (var detector in detectors)
                            {
                                bool isOccupied = await CheckLoopDetectorStateAsync(detector.Id);
                                bool stateChanged = _loopStates.AddOrUpdate(
                                    detector.Id,
                                    isOccupied,
                                    (_, oldState) => oldState != isOccupied);

                                if (stateChanged)
                                {
                                    await NotifyLoopStateChangeAsync(detector.Id, isOccupied);
                                }
                            }

                            return true;
                        },
                        "MonitorLoopDetectors",
                        this);

                    await Task.Delay(_loopCheckInterval, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error monitoring loop detectors");
                    await Task.Delay(1000, cancellationToken); // Wait before retrying
                }
            }
        }

        private async Task MonitorCamerasAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _isMonitoring)
            {
                try
                {
                    await _redundancyService.ExecuteWithRetryAsync<RealTimeMonitoringService>(
                        async () =>
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                            // Get all configured cameras
                            var cameras = await dbContext.Cameras
                                .Where(c => c.IsActive)
                                .ToListAsync(cancellationToken);

                            foreach (var camera in cameras)
                            {
                                var status = await CheckCameraStatusAsync(camera.Id);
                                _cameraStates.AddOrUpdate(
                                    camera.Id,
                                    status,
                                    (_, _) => status);

                                if (status.IsOnline)
                                {
                                    var frame = await CaptureFrameAsync(camera.Id);
                                    if (frame != null)
                                    {
                                        _latestFrames.AddOrUpdate(
                                            camera.Id,
                                            frame,
                                            (_, _) => frame);

                                        await NotifyCameraFrameUpdateAsync(camera.Id, frame);
                                    }
                                }
                            }

                            return true;
                        },
                        "MonitorCameras",
                        this);

                    await Task.Delay(_cameraCheckInterval, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error monitoring cameras");
                    await Task.Delay(1000, cancellationToken); // Wait before retrying
                }
            }
        }

        private async Task<bool> CheckLoopDetectorStateAsync(string detectorId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var detector = await dbContext.LoopDetectors
                    .FirstOrDefaultAsync(d => d.Id == detectorId);

                if (detector == null)
                    return false;

                // Implementation depends on your hardware setup
                // This is a placeholder that would be replaced with actual hardware communication
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking loop detector {DetectorId}", detectorId);
                return false;
            }
        }

        private async Task<CameraStatus> CheckCameraStatusAsync(string cameraId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var camera = await dbContext.Cameras
                    .FirstOrDefaultAsync(c => c.Id == cameraId);

                if (camera == null)
                {
                    return new CameraStatus { IsOnline = false, LastChecked = DateTime.UtcNow };
                }

                // Implementation depends on your camera setup
                // This is a placeholder that would be replaced with actual camera status check
                return new CameraStatus { IsOnline = true, LastChecked = DateTime.UtcNow };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking camera {CameraId}", cameraId);
                return new CameraStatus { IsOnline = false, LastChecked = DateTime.UtcNow };
            }
        }

        private async Task<byte[]?> CaptureFrameAsync(string cameraId)
        {
            try
            {
                // Implementation depends on your camera setup
                // This is a placeholder that would be replaced with actual frame capture
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing frame from camera {CameraId}", cameraId);
                return null;
            }
        }

        private async Task NotifyLoopStateChangeAsync(string detectorId, bool isOccupied)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync(
                    "LoopStateChanged",
                    new { DetectorId = detectorId, IsOccupied = isOccupied });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying loop state change");
            }
        }

        private async Task NotifyCameraFrameUpdateAsync(string cameraId, byte[] frameData)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync(
                    "CameraFrameUpdated",
                    new { CameraId = cameraId, FrameData = Convert.ToBase64String(frameData) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying camera frame update");
            }
        }

        public async Task<bool> IsLoopDetectorOccupiedAsync(string detectorId)
        {
            return _loopStates.GetValueOrDefault(detectorId, false);
        }

        public async Task<byte[]?> GetLatestCameraFrameAsync(string cameraId)
        {
            return _latestFrames.GetValueOrDefault(cameraId);
        }

        public async Task<Dictionary<string, bool>> GetAllLoopStatesAsync()
        {
            return new Dictionary<string, bool>(_loopStates);
        }

        public async Task<Dictionary<string, CameraStatus>> GetAllCameraStatesAsync()
        {
            return new Dictionary<string, CameraStatus>(_cameraStates);
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                _isMonitoring = false;

                _monitoringLock.Dispose();

                await Task.CompletedTask;
            }
        }
    }

    public class CameraStatus
    {
        public bool IsOnline { get; set; }
        public DateTime LastChecked { get; set; }
        public string? LastError { get; set; }
    }
} 