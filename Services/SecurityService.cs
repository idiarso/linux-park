using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Models;
using ParkIRC.Data;

namespace ParkIRC.Services
{
    public interface ISecurityService
    {
        Task<byte[]> EncryptImageAsync(byte[] imageData);
        Task<byte[]> DecryptImageAsync(byte[] encryptedData);
        Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission);
        Task<bool> ValidateAccessTokenAsync(string token);
        Task<string> GenerateAccessTokenAsync(string userId, string[] permissions);
        Task<bool> ValidateDeviceTokenAsync(string deviceId, string token);
    }

    public class SecurityService : ISecurityService
    {
        private readonly ILogger<SecurityService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly byte[] _encryptionKey;
        private readonly byte[] _encryptionIV;

        public SecurityService(
            ILogger<SecurityService> logger,
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

            // Load encryption keys from configuration
            string keyBase64 = _configuration["Security:EncryptionKey"] 
                ?? throw new InvalidOperationException("Encryption key not configured");
            string ivBase64 = _configuration["Security:EncryptionIV"]
                ?? throw new InvalidOperationException("Encryption IV not configured");

            _encryptionKey = Convert.FromBase64String(keyBase64);
            _encryptionIV = Convert.FromBase64String(ivBase64);
        }

        public async Task<byte[]> EncryptImageAsync(byte[] imageData)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = _encryptionKey;
                aes.IV = _encryptionIV;

                using var msEncrypt = new MemoryStream();
                using (var cryptoStream = new CryptoStream(
                    msEncrypt,
                    aes.CreateEncryptor(),
                    CryptoStreamMode.Write))
                {
                    await cryptoStream.WriteAsync(imageData);
                    await cryptoStream.FlushFinalBlockAsync();
                }

                return msEncrypt.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting image data");
                throw;
            }
        }

        public async Task<byte[]> DecryptImageAsync(byte[] encryptedData)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = _encryptionKey;
                aes.IV = _encryptionIV;

                using var msDecrypt = new MemoryStream();
                using (var cryptoStream = new CryptoStream(
                    new MemoryStream(encryptedData),
                    aes.CreateDecryptor(),
                    CryptoStreamMode.Read))
                {
                    await cryptoStream.CopyToAsync(msDecrypt);
                }

                return msDecrypt.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting image data");
                throw;
            }
        }

        public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission)
        {
            try
            {
                if (user == null)
                    return false;

                using var scope = _scopeFactory.CreateScope();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                var appUser = await userManager.GetUserAsync(user);
                if (appUser == null)
                    return false;

                // Check if user has the specific permission
                if (await userManager.IsInRoleAsync(appUser, "Administrator"))
                    return true;

                var userClaims = await userManager.GetClaimsAsync(appUser);
                return userClaims.Any(c => c.Type == "Permission" && c.Value == permission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for user", permission);
                return false;
            }
        }

        public async Task<bool> ValidateAccessTokenAsync(string token)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var accessToken = await dbContext.AccessTokens
                    .FirstOrDefaultAsync(t => t.Token == token && t.ExpiresAt > DateTime.UtcNow);

                return accessToken != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating access token");
                return false;
            }
        }

        public async Task<string> GenerateAccessTokenAsync(string userId, string[] permissions)
        {
            try
            {
                var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                var expiresAt = DateTime.UtcNow.AddHours(24); // Token valid for 24 hours

                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var accessToken = new ParkIRC.Models.AccessToken
                {
                    Token = token,
                    UserId = userId,
                    Permissions = string.Join(",", permissions),
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt
                };

                dbContext.AccessTokens.Add(accessToken);
                await dbContext.SaveChangesAsync();

                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating access token for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ValidateDeviceTokenAsync(string deviceId, string token)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var device = await dbContext.Devices
                    .FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.Token == token);

                return device != null && device.IsActive;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating device token for device {DeviceId}", deviceId);
                return false;
            }
        }
    }

    public class AccessToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Permissions { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class Device
    {
        public int Id { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime LastSeen { get; set; }
    }
} 