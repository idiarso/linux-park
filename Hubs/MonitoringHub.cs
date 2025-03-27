using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ParkIRC.Services;

namespace ParkIRC.Hubs
{
    public class MonitoringHub : Hub
    {
        private readonly ILogger<MonitoringHub> _logger;
        private readonly IRealTimeMonitoringService _monitoringService;

        public MonitoringHub(
            ILogger<MonitoringHub> logger,
            IRealTimeMonitoringService monitoringService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var connectionId = Context.ConnectionId;
                _logger.LogInformation("Client connected: {ConnectionId}", connectionId);

                // Send initial states
                var loopStates = await _monitoringService.GetAllLoopStatesAsync();
                var cameraStates = await _monitoringService.GetAllCameraStatesAsync();

                await Clients.Caller.SendAsync("InitialState", new
                {
                    LoopStates = loopStates,
                    CameraStates = cameraStates
                });

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnConnectedAsync");
                throw;
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var connectionId = Context.ConnectionId;
                _logger.LogInformation(
                    exception,
                    "Client disconnected: {ConnectionId}",
                    connectionId);

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnDisconnectedAsync");
                throw;
            }
        }

        public async Task RequestCameraFrame(string cameraId)
        {
            try
            {
                var frame = await _monitoringService.GetLatestCameraFrameAsync(cameraId);
                if (frame != null)
                {
                    await Clients.Caller.SendAsync(
                        "CameraFrame",
                        new
                        {
                            CameraId = cameraId,
                            FrameData = Convert.ToBase64String(frame)
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting camera frame for {CameraId}", cameraId);
                await Clients.Caller.SendAsync(
                    "Error",
                    new { Message = "Failed to get camera frame", CameraId = cameraId });
            }
        }

        public async Task SubscribeToCamera(string cameraId)
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Camera_{cameraId}");
                _logger.LogInformation(
                    "Client {ConnectionId} subscribed to camera {CameraId}",
                    Context.ConnectionId,
                    cameraId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to camera {CameraId}", cameraId);
                await Clients.Caller.SendAsync(
                    "Error",
                    new { Message = "Failed to subscribe to camera", CameraId = cameraId });
            }
        }

        public async Task UnsubscribeFromCamera(string cameraId)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Camera_{cameraId}");
                _logger.LogInformation(
                    "Client {ConnectionId} unsubscribed from camera {CameraId}",
                    Context.ConnectionId,
                    cameraId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing from camera {CameraId}", cameraId);
                await Clients.Caller.SendAsync(
                    "Error",
                    new { Message = "Failed to unsubscribe from camera", CameraId = cameraId });
            }
        }

        public async Task SubscribeToLoopDetector(string detectorId)
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Loop_{detectorId}");
                _logger.LogInformation(
                    "Client {ConnectionId} subscribed to loop detector {DetectorId}",
                    Context.ConnectionId,
                    detectorId);

                // Send current state
                var isOccupied = await _monitoringService.IsLoopDetectorOccupiedAsync(detectorId);
                await Clients.Caller.SendAsync(
                    "LoopState",
                    new { DetectorId = detectorId, IsOccupied = isOccupied });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to loop detector {DetectorId}", detectorId);
                await Clients.Caller.SendAsync(
                    "Error",
                    new { Message = "Failed to subscribe to loop detector", DetectorId = detectorId });
            }
        }

        public async Task UnsubscribeFromLoopDetector(string detectorId)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Loop_{detectorId}");
                _logger.LogInformation(
                    "Client {ConnectionId} unsubscribed from loop detector {DetectorId}",
                    Context.ConnectionId,
                    detectorId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing from loop detector {DetectorId}", detectorId);
                await Clients.Caller.SendAsync(
                    "Error",
                    new { Message = "Failed to unsubscribe from loop detector", DetectorId = detectorId });
            }
        }
    }
} 