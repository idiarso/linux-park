using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Diagnostics;
using ParkIRC.Hubs;
using System.IO;
using Microsoft.Extensions.Configuration;
using ParkIRC.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Timer = System.Threading.Timer;
using System.Security.AccessControl;
using ParkIRC.Models;
using DirectShowLib;
using System.Collections.Generic;
using System.Linq;

namespace ParkIRC.Services
{
    public sealed class ConnectionStatusService : IHostedService, IAsyncDisposable
    {
        private readonly ILogger<ConnectionStatusService> _logger;
        private readonly IHubContext<ParkingHub> _hubContext;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly PrintService _printService;
        private Timer? _timer;
        private bool _isRunning;
        private bool _disposed;
        private readonly SemaphoreSlim _statusLock = new(1, 1);
        private readonly int _checkInterval;

        public ConnectionStatusService(
            ILogger<ConnectionStatusService> logger,
            IHubContext<ParkingHub> hubContext,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            PrintService printService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _printService = printService ?? throw new ArgumentNullException(nameof(printService));
            
            _checkInterval = _configuration.GetValue<int>("Monitoring:CheckInterval", 60);
            _isRunning = false;
            _disposed = false;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ConnectionStatusService));
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            _logger.LogInformation("Connection Status Service is starting");
            
            _timer = new Timer(
                async state => await CheckStatusAsync(state).ConfigureAwait(false),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(_checkInterval));
            
            return Task.CompletedTask;
        }

        private async Task CheckStatusAsync(object? state)
        {
            if (_disposed || _isRunning)
                return;

            await _statusLock.WaitAsync();
            try
            {
                _isRunning = true;
                _logger.LogDebug("Checking system connection status...");
                
                var systemStatus = await GetSystemStatusAsync();
                
                await _hubContext.Clients.All.SendAsync(
                    "ReceiveSystemStatus",
                    systemStatus,
                    CancellationToken.None);
                
                _logger.LogDebug(
                    "System status: DB={Database}, Printer={Printer}, Camera={Camera}",
                    systemStatus.Database,
                    systemStatus.Printer,
                    systemStatus.Camera);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking system status");
            }
            finally
            {
                _isRunning = false;
                _statusLock.Release();
            }
        }
        
        public async Task<SystemStatusDto> GetSystemStatusAsync()
        {
            ThrowIfDisposed();

            var status = new SystemStatusDto
            {
                Timestamp = DateTime.UtcNow
            };
            
            // Check database connection
            status.Database = await CheckDatabaseConnectionAsync();
            
            // Check printer
            status.Printer = await CheckPrinterConnectionAsync();
            
            // Check camera
            status.Camera = await CheckCameraConnectionAsync();
            
            // Get system resources
            await GetSystemResourcesAsync(status);
            
            return status;
        }

        private async Task<bool> CheckDatabaseConnectionAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                return await dbContext.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database connection");
                return false;
            }
        }

        private async Task<bool> CheckPrinterConnectionAsync()
        {
            try
            {
                // Check printer using CUPS on Linux/macOS or WMI on Windows
                if (OperatingSystem.IsWindows())
                {
                    return await Task.Run(() =>
                    {
                        var processInfo = new ProcessStartInfo
                        {
                            FileName = "wmic",
                            Arguments = "printer get name",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        
                        using var process = Process.Start(processInfo);
                        if (process == null)
                            return false;

                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                        return !string.IsNullOrEmpty(output) && output.Contains("\n");
                    });
                }
                else
                {
                    return await Task.Run(() =>
                    {
                        var processInfo = new ProcessStartInfo
                        {
                            FileName = "lpstat",
                            Arguments = "-p",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        
                        using var process = Process.Start(processInfo);
                        if (process == null)
                            return false;

                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                        return !string.IsNullOrEmpty(output) && !output.Contains("no destinations");
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking printer status");
                return false;
            }
        }

        private async Task<bool> CheckCameraConnectionAsync()
        {
            try
            {
                // Check camera using DirectShow
                if (OperatingSystem.IsWindows())
                {
                    return await Task.Run(() =>
                    {
                        var devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
                        return devices.Length > 0;
                    });
                }
                else
                {
                    // For Linux/macOS, check if video device exists
                    return await Task.Run(() =>
                    {
                        var processInfo = new ProcessStartInfo
                        {
                            FileName = "ls",
                            Arguments = "/dev/video*",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        
                        using var process = Process.Start(processInfo);
                        if (process == null)
                            return false;

                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                        return !string.IsNullOrEmpty(output) && output.Contains("video");
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking camera status");
                return false;
            }
        }
        
        private async Task GetSystemResourcesAsync(SystemStatusDto status)
        {
            ArgumentNullException.ThrowIfNull(status);

            try
            {
                if (OperatingSystem.IsWindows())
                {
                    await GetWindowsSystemResourcesAsync(status);
                }
                else
                {
                    await GetLinuxSystemResourcesAsync(status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system resources");
            }
        }

        private async Task GetWindowsSystemResourcesAsync(SystemStatusDto status)
        {
            await Task.Run(() =>
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(Directory.GetCurrentDirectory()) ?? "C:");
                status.DiskTotal = FormatBytes(driveInfo.TotalSize);
                status.DiskUsed = FormatBytes(driveInfo.TotalSize - driveInfo.AvailableFreeSpace);
                status.DiskFree = FormatBytes(driveInfo.AvailableFreeSpace);
                status.DiskUsagePercent = $"{((double)(driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / driveInfo.TotalSize * 100):F1}%";

                var performanceCounter = new PerformanceCounter("Memory", "Available MBytes", true);
                var totalMemory = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
                var availableMemory = performanceCounter.NextValue() * 1024 * 1024;
                var usedMemory = totalMemory - availableMemory;

                status.MemoryTotal = FormatBytes(totalMemory);
                status.MemoryUsed = FormatBytes(usedMemory);
                status.MemoryFree = FormatBytes(availableMemory);
            });
        }

        private async Task GetLinuxSystemResourcesAsync(SystemStatusDto status)
        {
            await Task.Run(() =>
            {
                // Get disk usage
                var diskProcess = new ProcessStartInfo
                {
                    FileName = "df",
                    Arguments = "-h .",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(diskProcess))
                {
                    if (process != null)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                        var lines = output.Split('\n');
                        if (lines.Length > 1)
                        {
                            var parts = lines[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 4)
                            {
                                status.DiskTotal = parts[1];
                                status.DiskUsed = parts[2];
                                status.DiskFree = parts[3];
                                status.DiskUsagePercent = parts[4];
                            }
                        }
                    }
                }
                
                // Get memory usage
                var memProcess = new ProcessStartInfo
                {
                    FileName = "free",
                    Arguments = "-h",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(memProcess))
                {
                    if (process != null)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                        var lines = output.Split('\n');
                        if (lines.Length > 1)
                        {
                            var parts = lines[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 6)
                            {
                                status.MemoryTotal = parts[1];
                                status.MemoryUsed = parts[2];
                                status.MemoryFree = parts[3];
                            }
                        }
                    }
                }
            });
        }

        private static string FormatBytes(double bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (bytes >= 1024 && order < sizes.Length - 1)
            {
                order++;
                bytes /= 1024;
            }
            return $"{bytes:0.##} {sizes[order]}";
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            _logger.LogInformation("Connection Status Service is stopping");
            
            if (_timer != null)
            {
                await _timer.DisposeAsync();
                _timer = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;

                if (_timer != null)
                {
                    await _timer.DisposeAsync();
                    _timer = null;
                }

                _statusLock.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        public async Task<SystemStatus> GetSystemStatus()
        {
            var status = new SystemStatus
            {
                IsOnline = true,
                LastChecked = DateTime.UtcNow,
                Components = new Dictionary<string, bool>()
            };

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Check database connection
                status.Components["Database"] = await dbContext.Database.CanConnectAsync();

                // Check hardware components
                var hardwareStatus = await dbContext.HardwareStatuses
                    .OrderByDescending(h => h.LastChecked)
                    .FirstOrDefaultAsync();

                if (hardwareStatus != null)
                {
                    status.Components["Camera"] = hardwareStatus.IsCameraOnline;
                    status.Components["Gate"] = hardwareStatus.IsGateOnline;
                    status.Components["Printer"] = hardwareStatus.IsPrinterOnline;
                    status.Components["LoopDetector"] = hardwareStatus.IsLoopDetectorOnline;
                }

                // Overall system status is online only if all components are online
                status.IsOnline = status.Components.All(c => c.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking system status");
                status.IsOnline = false;
            }

            return status;
        }
    }
    
    public sealed class SystemStatusDto
    {
        public DateTime Timestamp { get; set; }
        public bool Database { get; set; }
        public bool Printer { get; set; }
        public bool Camera { get; set; }
        public bool IsConnected => Database && Printer && Camera;
        
        // System resources
        public string DiskTotal { get; set; } = string.Empty;
        public string DiskUsed { get; set; } = string.Empty;
        public string DiskFree { get; set; } = string.Empty;
        public string DiskUsagePercent { get; set; } = string.Empty;
        
        public string MemoryTotal { get; set; } = string.Empty;
        public string MemoryUsed { get; set; } = string.Empty;
        public string MemoryFree { get; set; } = string.Empty;
    }
}