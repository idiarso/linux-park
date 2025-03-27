using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using ParkIRC.Models;

namespace ParkIRC.Services
{
    public interface ICachingService
    {
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task ClearAsync();
    }

    public class CachingService : ICachingService
    {
        private readonly ILogger<CachingService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks;
        private readonly TimeSpan _defaultExpiration;
        private readonly int _maxCacheSize;

        public CachingService(
            ILogger<CachingService> logger,
            IMemoryCache cache,
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

            _locks = new ConcurrentDictionary<string, SemaphoreSlim>();
            _defaultExpiration = TimeSpan.FromMinutes(
                _configuration.GetValue<int>("Cache:DefaultExpirationMinutes", 10));
            _maxCacheSize = _configuration.GetValue<int>("Cache:MaxItems", 1000);
        }

        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            try
            {
                if (await ExistsAsync(key))
                {
                    return await GetAsync<T>(key);
                }

                var lockObj = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
                await lockObj.WaitAsync();
                try
                {
                    // Double check after acquiring lock
                    if (await ExistsAsync(key))
                    {
                        return await GetAsync<T>(key);
                    }

                    var value = await factory();
                    await SetAsync(key, value, expiration);
                    return value;
                }
                finally
                {
                    lockObj.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOrSetAsync for key {Key}", key);
                throw;
            }
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                if (_cache.TryGetValue(key, out T? value))
                {
                    _logger.LogDebug("Cache hit for key {Key}", key);
                    return value;
                }

                _logger.LogDebug("Cache miss for key {Key}", key);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting value for key {Key}", key);
                throw;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSize(1) // Each entry counts as 1 unit
                    .SetPriority(CacheItemPriority.Normal)
                    .SetSlidingExpiration(expiration ?? _defaultExpiration);

                _cache.Set(key, value, cacheEntryOptions);
                _logger.LogDebug("Set cache value for key {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting value for key {Key}", key);
                throw;
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                _cache.Remove(key);
                _logger.LogDebug("Removed cache entry for key {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing key {Key}", key);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return _cache.TryGetValue(key, out _);
        }

        public async Task ClearAsync()
        {
            try
            {
                if (_cache is MemoryCache memoryCache)
                {
                    memoryCache.Compact(1.0);
                    _logger.LogInformation("Cache cleared");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
                throw;
            }
        }
    }

    public static class CacheKeys
    {
        public static string VehicleInfo(string ticketNumber) => $"vehicle:{ticketNumber}";
        public static string UserPermissions(string userId) => $"permissions:{userId}";
        public static string DeviceStatus(string deviceId) => $"device:{deviceId}";
        public static string CameraFrame(string cameraId) => $"frame:{cameraId}";
        public static string LoopDetectorState(string detectorId) => $"loop:{detectorId}";
        public static string Configuration(string key) => $"config:{key}";
    }
} 