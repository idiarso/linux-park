using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ParkIRC.Services
{
    public class PrismaService
    {
        private readonly ILogger<PrismaService> _logger;
        private Process? _process;

        public PrismaService(ILogger<PrismaService> logger)
        {
            _logger = logger;
        }

        public async Task<string> ExecutePrismaQuery(string scriptPath, string args = "")
        {
            try
            {
                // Node.js process untuk menjalankan script Prisma
                _process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "node",
                        Arguments = $"{scriptPath} {args}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                _process.Start();
                
                string output = await _process.StandardOutput.ReadToEndAsync();
                string error = await _process.StandardError.ReadToEndAsync();
                
                await _process.WaitForExitAsync();
                
                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogError($"Prisma query error: {error}");
                    throw new Exception($"Prisma query failed: {error}");
                }
                
                return output;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing Prisma query: {ex.Message}");
                throw;
            }
        }

        public async Task<T> ExecutePrismaQueryTyped<T>(string scriptPath, string args = "")
        {
            string result = await ExecutePrismaQuery(scriptPath, args);
            if (string.IsNullOrEmpty(result))
            {
                throw new InvalidOperationException("Query result is empty");
            }
            
            T? data = JsonSerializer.Deserialize<T>(result);
            if (data == null)
            {
                throw new InvalidOperationException("Failed to deserialize query result");
            }
            
            return data;
        }
    }
} 