using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using ParkIRC.Data;
using Timer = System.Threading.Timer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ParkIRC.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace ParkIRC.Services
{
    public sealed class MaintenanceService : IHostedService, IAsyncDisposable
    {
        private readonly ILogger<MaintenanceService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private Timer? _timer;
        private bool _disposed;
        private readonly SemaphoreSlim _maintenanceLock = new(1, 1);
        private readonly string _backupPath;
        private readonly string _tempPath;
        private readonly int _backupRetentionDays;
        private readonly int _logRetentionDays;
        private readonly PrinterService _printerService;

        public MaintenanceService(
            ILogger<MaintenanceService> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            PrinterService printerService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _printerService = printerService ?? throw new ArgumentNullException(nameof(printerService));
            
            _backupPath = _configuration.GetValue<string>("Maintenance:BackupPath", 
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups"));
            _tempPath = _configuration.GetValue<string>("Maintenance:TempPath", 
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp"));
            _backupRetentionDays = _configuration.GetValue<int>("Maintenance:BackupRetentionDays", 30);
            _logRetentionDays = _configuration.GetValue<int>("Maintenance:LogRetentionDays", 90);
            
            Directory.CreateDirectory(_backupPath);
            Directory.CreateDirectory(_tempPath);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MaintenanceService));
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            _logger.LogInformation("Maintenance Service is starting");

            _timer = new Timer(
                async state => await DoWorkAsync(state).ConfigureAwait(false),
                null,
                TimeSpan.Zero,
                TimeSpan.FromHours(24));

            // Schedule weekly and monthly tasks
            ScheduleRecurringTasks();

            return Task.CompletedTask;
        }

        private void ScheduleRecurringTasks()
        {
            var now = DateTime.Now;
            
            // Schedule weekly tasks for Sunday at 2 AM
            var nextSunday = now.DayOfWeek == DayOfWeek.Sunday && now.Hour < 2
                ? now.Date.AddHours(2)
                : now.Date.AddDays(7 - (int)now.DayOfWeek).AddHours(2);
            
            var weeklyTimer = new Timer(
                async state => await RunWeeklyTasksAsync().ConfigureAwait(false),
                null,
                nextSunday - now,
                TimeSpan.FromDays(7));

            // Schedule monthly tasks for 1st day at 3 AM
            var nextMonth = now.Day == 1 && now.Hour < 3
                ? now.Date.AddHours(3)
                : now.Date.AddMonths(1).AddDays(1 - now.Day).AddHours(3);
            
            var monthlyTimer = new Timer(
                async state => await RunMonthlyTasksAsync().ConfigureAwait(false),
                null,
                nextMonth - now,
                TimeSpan.FromDays(30));
        }

        private async Task DoWorkAsync(object? state)
        {
            if (_disposed)
                return;

            await _maintenanceLock.WaitAsync();
            try
            {
                _logger.LogInformation("Performing daily maintenance tasks...");
                
                await BackupDatabaseAsync();
                await CheckErrorLogsAsync();
                await VerifyConnectionsAsync();
                
                _logger.LogInformation("Daily maintenance tasks completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing daily maintenance tasks");
            }
            finally
            {
                _maintenanceLock.Release();
            }
        }

        private async Task RunWeeklyTasksAsync()
        {
            if (_disposed)
                return;

            await _maintenanceLock.WaitAsync();
            try
            {
                _logger.LogInformation("Performing weekly maintenance tasks...");
                
                await CleanupTempFilesAsync();
                await TestBackupRestoreAsync();
                
                _logger.LogInformation("Weekly maintenance tasks completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing weekly maintenance tasks");
            }
            finally
            {
                _maintenanceLock.Release();
            }
        }

        private async Task RunMonthlyTasksAsync()
        {
            if (_disposed)
                return;

            await _maintenanceLock.WaitAsync();
            try
            {
                _logger.LogInformation("Performing monthly maintenance tasks...");
                
                await PerformanceAuditAsync();
                await SecurityAuditAsync();
                
                _logger.LogInformation("Monthly maintenance tasks completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing monthly maintenance tasks");
            }
            finally
            {
                _maintenanceLock.Release();
            }
        }

        private async Task BackupDatabaseAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var backupFileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                var backupFilePath = Path.Combine(_backupPath, backupFileName);
                
                // Create backup using EF Core
                await dbContext.Database.ExecuteSqlRawAsync(
                    $"BACKUP DATABASE {dbContext.Database.GetDbConnection().Database} " +
                    $"TO DISK = '{backupFilePath}' WITH INIT");
                
                // Cleanup old backups
                var oldBackups = Directory.GetFiles(_backupPath, "backup_*.bak")
                    .Select(f => new FileInfo(f))
                    .Where(f => f.CreationTime < DateTime.Now.AddDays(-_backupRetentionDays));
                
                foreach (var file in oldBackups)
                {
                    file.Delete();
                }
                
                _logger.LogInformation("Database backup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing database backup");
                throw;
            }
        }

        private async Task CheckErrorLogsAsync()
        {
            try
            {
                var logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(logsPath))
                    return;

                // Analyze error logs
                var errorLogs = Directory.GetFiles(logsPath, "*.log")
                    .Select(f => new FileInfo(f))
                    .Where(f => f.LastWriteTime >= DateTime.Now.AddDays(-1));

                foreach (var log in errorLogs)
                {
                    var content = await File.ReadAllLinesAsync(log.FullName);
                    var errorCount = content.Count(l => l.Contains("[Error]", StringComparison.OrdinalIgnoreCase));
                    
                    if (errorCount > 0)
                    {
                        _logger.LogWarning("Found {ErrorCount} errors in log file {LogFile}", 
                            errorCount, log.Name);
                    }
                }

                // Cleanup old logs
                var oldLogs = Directory.GetFiles(logsPath, "*.log")
                    .Select(f => new FileInfo(f))
                    .Where(f => f.LastWriteTime < DateTime.Now.AddDays(-_logRetentionDays));

                foreach (var file in oldLogs)
                {
                    file.Delete();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking error logs");
                throw;
            }
        }

        private async Task VerifyConnectionsAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                // Check database connection
                if (!await dbContext.Database.CanConnectAsync())
                {
                    _logger.LogError("Database connection verification failed");
                    return;
                }

                // Check printer connections
                var printerStatuses = await _printerService.GetAllPrinterStatus();
                foreach (var (id, status) in printerStatuses)
                {
                    _logger.LogInformation("Printer {Id}: {Status}", id, status);
                }

                _logger.LogInformation("Connection verification completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying connections");
                throw;
            }
        }

        private async Task CleanupTempFilesAsync()
        {
            try
            {
                if (!Directory.Exists(_tempPath))
                    return;

                var oldFiles = Directory.GetFiles(_tempPath, "*.*", SearchOption.AllDirectories)
                    .Select(f => new FileInfo(f))
                    .Where(f => f.LastAccessTime < DateTime.Now.AddDays(-7));

                foreach (var file in oldFiles)
                {
                    file.Delete();
                }

                // Cleanup empty directories
                foreach (var dir in Directory.GetDirectories(_tempPath, "*", SearchOption.AllDirectories))
                {
                    if (!Directory.EnumerateFileSystemEntries(dir).Any())
                    {
                        Directory.Delete(dir);
                    }
                }

                _logger.LogInformation("Temporary files cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up temporary files");
                throw;
            }
        }

        private async Task TestBackupRestoreAsync()
        {
            try
            {
                // Get latest backup
                var latestBackup = Directory.GetFiles(_backupPath, "backup_*.bak")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .FirstOrDefault();

                if (latestBackup == null)
                {
                    _logger.LogWarning("No backup files found for restore test");
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                // Create test database name
                var testDbName = $"TestRestore_{DateTime.Now:yyyyMMdd}";
                
                // Restore to test database
                await dbContext.Database.ExecuteSqlRawAsync(
                    $"RESTORE DATABASE {testDbName} " +
                    $"FROM DISK = '{latestBackup.FullName}' " +
                    "WITH REPLACE");
                
                // Verify test database
                var testConnection = dbContext.Database.GetDbConnection().ConnectionString
                    .Replace(dbContext.Database.GetDbConnection().Database, testDbName);
                
                using var testContext = new ApplicationDbContext(
                    new DbContextOptionsBuilder<ApplicationDbContext>()
                        .UseSqlServer(testConnection)
                        .Options);
                
                if (await testContext.Database.CanConnectAsync())
                {
                    _logger.LogInformation("Backup restore test completed successfully");
                }
                else
                {
                    _logger.LogError("Backup restore test failed - could not connect to restored database");
                }
                
                // Cleanup test database
                await dbContext.Database.ExecuteSqlRawAsync($"DROP DATABASE {testDbName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing backup restore");
                throw;
            }
        }

        private async Task PerformanceAuditAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                // Check database size
                var dbSize = await dbContext.Database.ExecuteSqlRawAsync(
                    "SELECT SUM(size * 8 / 1024) FROM sys.master_files WHERE database_id = DB_ID()");
                
                if (dbSize > 1024) // 1 GB
                {
                    _logger.LogWarning("Database size exceeds 1 GB");
                }

                // Check index fragmentation
                await dbContext.Database.ExecuteSqlRawAsync(@"
                    DECLARE @TableName sysname
                    DECLARE TableCursor CURSOR FOR
                        SELECT OBJECT_NAME([object_id])
                        FROM sys.indexes
                        WHERE index_id > 0 AND [object_id] > 100

                    OPEN TableCursor
                    FETCH NEXT FROM TableCursor INTO @TableName

                    WHILE @@FETCH_STATUS = 0
                    BEGIN
                        EXEC('ALTER INDEX ALL ON ' + @TableName + ' REBUILD')
                        FETCH NEXT FROM TableCursor INTO @TableName
                    END

                    CLOSE TableCursor
                    DEALLOCATE TableCursor");

                _logger.LogInformation("Performance audit completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing performance audit");
                throw;
            }
        }

        private async Task SecurityAuditAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                // Check for failed login attempts
                var failedLogins = await dbContext.Database.ExecuteSqlRawAsync(@"
                    SELECT COUNT(*) 
                    FROM sys.event_log 
                    WHERE event_type = 'LOGIN FAILED' 
                    AND DATEDIFF(HOUR, event_time, GETDATE()) <= 24");
                
                if (failedLogins > 10)
                {
                    _logger.LogWarning("High number of failed login attempts detected: {Count}", failedLogins);
                }

                // Check database permissions
                var permissionAudit = await dbContext.Database.ExecuteSqlRawAsync(@"
                    SELECT dp.name AS principal_name,
                           dp.type_desc AS principal_type,
                           o.name AS object_name,
                           p.permission_name,
                           p.state_desc
                    FROM sys.database_permissions p
                    JOIN sys.database_principals dp ON p.grantee_principal_id = dp.principal_id
                    JOIN sys.objects o ON p.major_id = o.object_id
                    WHERE dp.name NOT IN ('dbo', 'public', 'INFORMATION_SCHEMA', 'sys')");

                _logger.LogInformation("Security audit completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing security audit");
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            _logger.LogInformation("Maintenance Service is stopping");
            
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

                _maintenanceLock.Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }
}