using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Data;
using ParkIRC.Models;
using ParkIRC.Hardware;

namespace ParkIRC.Services
{
    public interface IHardwareRedundancyService
    {
        Task<bool> ExecuteWithRetryAsync<T>(
            Func<Task<bool>> operation,
            string operationName,
            T context,
            CancellationToken cancellationToken = default) where T : class;
        Task<bool> SwitchToBackupDeviceAsync(string deviceType);
        Task<bool> RestoreMainDeviceAsync(string deviceType);
        bool IsUsingBackupDevice(string deviceType);
    }

    public sealed class HardwareRedundancyService : IHardwareRedundancyService
    {
        private readonly ILogger<HardwareRedundancyService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConcurrentDictionary<string, bool> _usingBackupDevice;
        private readonly ConcurrentDictionary<string, int> _failureCount;
        private readonly SemaphoreSlim _switchLock = new(1, 1);

        private readonly int _maxRetries;
        private readonly TimeSpan _retryDelay;
        private readonly TimeSpan _failureThresholdTime;
        private readonly int _failureThresholdCount;

        public HardwareRedundancyService(
            ILogger<HardwareRedundancyService> logger,
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

            _usingBackupDevice = new ConcurrentDictionary<string, bool>();
            _failureCount = new ConcurrentDictionary<string, int>();

            // Load configuration
            _maxRetries = _configuration.GetValue<int>("HardwareRedundancy:MaxRetries", 3);
            _retryDelay = TimeSpan.FromSeconds(_configuration.GetValue<int>("HardwareRedundancy:RetryDelaySeconds", 5));
            _failureThresholdTime = TimeSpan.FromMinutes(_configuration.GetValue<int>("HardwareRedundancy:FailureThresholdMinutes", 5));
            _failureThresholdCount = _configuration.GetValue<int>("HardwareRedundancy:FailureThresholdCount", 5);
        }

        public async Task<bool> ExecuteWithRetryAsync<T>(
            Func<Task<bool>> operation,
            string operationName,
            T context,
            CancellationToken cancellationToken = default) where T : class
        {
            int retryCount = 0;
            bool success = false;
            Exception? lastException = null;

            while (retryCount < _maxRetries && !success && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (retryCount > 0)
                    {
                        await Task.Delay(_retryDelay * (retryCount + 1), cancellationToken);
                        _logger.LogWarning(
                            "Retrying {OperationName} (Attempt {RetryCount}/{MaxRetries})",
                            operationName,
                            retryCount + 1,
                            _maxRetries);
                    }

                    success = await operation();

                    if (success)
                    {
                        if (retryCount > 0)
                        {
                            _logger.LogInformation(
                                "{OperationName} succeeded after {RetryCount} retries",
                                operationName,
                                retryCount);
                        }
                        ResetFailureCount(operationName);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogError(
                        ex,
                        "Error during {OperationName} (Attempt {RetryCount}/{MaxRetries})",
                        operationName,
                        retryCount + 1,
                        _maxRetries);
                }

                retryCount++;
            }

            // If we get here, all retries failed
            IncrementFailureCount(operationName);

            // Check if we need to switch to backup device
            if (await ShouldSwitchToBackupAsync(operationName))
            {
                _logger.LogWarning(
                    "Switching to backup device for {OperationName} due to repeated failures",
                    operationName);
                await SwitchToBackupDeviceAsync(operationName);
            }

            if (lastException != null)
            {
                _logger.LogError(
                    lastException,
                    "{OperationName} failed after {RetryCount} retries",
                    operationName,
                    retryCount);
            }

            return false;
        }

        public async Task<bool> SwitchToBackupDeviceAsync(string deviceType)
        {
            await _switchLock.WaitAsync();
            try
            {
                if (_usingBackupDevice.GetOrAdd(deviceType, false))
                {
                    _logger.LogWarning("Already using backup device for {DeviceType}", deviceType);
                    return true;
                }

                using var scope = _scopeFactory.CreateScope();
                var hardwareManager = scope.ServiceProvider.GetRequiredService<HardwareManager>();

                bool switched = false;
                switch (deviceType.ToLowerInvariant())
                {
                    case "camera":
                        switched = await SwitchCameraDevice(hardwareManager);
                        break;
                    case "printer":
                        switched = await SwitchPrinterDevice(hardwareManager);
                        break;
                    case "gate":
                        switched = await SwitchGateController(hardwareManager);
                        break;
                    default:
                        _logger.LogError("Unknown device type: {DeviceType}", deviceType);
                        return false;
                }

                if (switched)
                {
                    _usingBackupDevice[deviceType] = true;
                    _logger.LogInformation("Successfully switched to backup {DeviceType}", deviceType);
                    return true;
                }

                return false;
            }
            finally
            {
                _switchLock.Release();
            }
        }

        public async Task<bool> RestoreMainDeviceAsync(string deviceType)
        {
            await _switchLock.WaitAsync();
            try
            {
                if (!_usingBackupDevice.GetOrAdd(deviceType, false))
                {
                    _logger.LogInformation("Already using main device for {DeviceType}", deviceType);
                    return true;
                }

                using var scope = _scopeFactory.CreateScope();
                var hardwareManager = scope.ServiceProvider.GetRequiredService<HardwareManager>();

                bool restored = false;
                switch (deviceType.ToLowerInvariant())
                {
                    case "camera":
                        restored = await RestoreCameraDevice(hardwareManager);
                        break;
                    case "printer":
                        restored = await RestorePrinterDevice(hardwareManager);
                        break;
                    case "gate":
                        restored = await RestoreGateController(hardwareManager);
                        break;
                    default:
                        _logger.LogError("Unknown device type: {DeviceType}", deviceType);
                        return false;
                }

                if (restored)
                {
                    _usingBackupDevice[deviceType] = false;
                    ResetFailureCount(deviceType);
                    _logger.LogInformation("Successfully restored main {DeviceType}", deviceType);
                    return true;
                }

                return false;
            }
            finally
            {
                _switchLock.Release();
            }
        }

        public bool IsUsingBackupDevice(string deviceType)
        {
            return _usingBackupDevice.GetOrAdd(deviceType, false);
        }

        private void IncrementFailureCount(string operationName)
        {
            _failureCount.AddOrUpdate(
                operationName,
                1,
                (_, count) => count + 1);
        }

        private void ResetFailureCount(string operationName)
        {
            _failureCount.TryRemove(operationName, out _);
        }

        private async Task<bool> ShouldSwitchToBackupAsync(string operationName)
        {
            if (_failureCount.TryGetValue(operationName, out int count))
            {
                return count >= _failureThresholdCount;
            }
            return false;
        }

        private async Task<bool> SwitchCameraDevice(HardwareManager hardwareManager)
        {
            try
            {
                // Implementation depends on your camera setup
                // Example: Switch from main webcam to backup IP camera
                var config = await GetBackupDeviceConfig("camera");
                if (config != null)
                {
                    // Update camera configuration
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error switching camera device");
                return false;
            }
        }

        private async Task<bool> SwitchPrinterDevice(HardwareManager hardwareManager)
        {
            try
            {
                // Implementation depends on your printer setup
                // Example: Switch from main printer to backup printer
                var config = await GetBackupDeviceConfig("printer");
                if (config != null)
                {
                    // Update printer configuration
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error switching printer device");
                return false;
            }
        }

        private async Task<bool> SwitchGateController(HardwareManager hardwareManager)
        {
            try
            {
                // Implementation depends on your gate controller setup
                // Example: Switch from main controller to backup controller
                var config = await GetBackupDeviceConfig("gate");
                if (config != null)
                {
                    // Update gate controller configuration
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error switching gate controller");
                return false;
            }
        }

        private async Task<bool> RestoreCameraDevice(HardwareManager hardwareManager)
        {
            try
            {
                // Implementation to restore main camera
                var config = await GetMainDeviceConfig("camera");
                if (config != null)
                {
                    // Restore camera configuration
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring camera device");
                return false;
            }
        }

        private async Task<bool> RestorePrinterDevice(HardwareManager hardwareManager)
        {
            try
            {
                // Implementation to restore main printer
                var config = await GetMainDeviceConfig("printer");
                if (config != null)
                {
                    // Restore printer configuration
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring printer device");
                return false;
            }
        }

        private async Task<bool> RestoreGateController(HardwareManager hardwareManager)
        {
            try
            {
                // Implementation to restore main gate controller
                var config = await GetMainDeviceConfig("gate");
                if (config != null)
                {
                    // Restore gate controller configuration
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring gate controller");
                return false;
            }
        }

        private async Task<DeviceConfig?> GetBackupDeviceConfig(string deviceType)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await dbContext.DeviceConfigs
                .FirstOrDefaultAsync(d => d.DeviceType == deviceType && d.IsBackup);
        }

        private async Task<DeviceConfig?> GetMainDeviceConfig(string deviceType)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await dbContext.DeviceConfigs
                .FirstOrDefaultAsync(d => d.DeviceType == deviceType && !d.IsBackup);
        }
    }
} 