using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace ParkIRC.Infrastructure.Logging
{
    public static class LoggingConfiguration
    {
        public static void ConfigureLogging(ILoggingBuilder logging, string environment)
        {
            // Clear existing providers
            logging.ClearProviders();

            // Add console logging with sensitive data filter
            logging.AddProvider(new SensitiveDataFilter());

            // Configure log levels
            logging.SetMinimumLevel(
                environment.Equals("Development", StringComparison.OrdinalIgnoreCase) 
                    ? LogLevel.Debug 
                    : LogLevel.Information);

            // Add file logging in production
            if (!environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logPath);

                logging.AddFile(Path.Combine(logPath, "parkirc-{Date}.log"), options =>
                {
                    options.FileSizeLimitBytes = 10 * 1024 * 1024; // 10MB
                    options.RetainedFileCountLimit = 10;
                    options.Append = true;
                });
            }

            // Add filters for sensitive categories
            logging.AddFilter((category, level) =>
            {
                // Blacklist categories that might contain sensitive data
                string[] sensitiveCategories = {
                    "Microsoft.EntityFrameworkCore.Database.Command",
                    "Microsoft.AspNetCore.DataProtection",
                    "System.Net.Http.HttpClient"
                };

                foreach (var sensitiveCategory in sensitiveCategories)
                {
                    if (category?.StartsWith(sensitiveCategory, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return level >= LogLevel.Error;
                    }
                }

                return true;
            });
        }
    }

    public static class FileLoggerExtensions
    {
        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string filePath, Action<FileLoggerOptions> configure = null)
        {
            var options = new FileLoggerOptions
            {
                FilePath = filePath,
                FileSizeLimitBytes = 10 * 1024 * 1024, // 10MB default
                RetainedFileCountLimit = 10 // Keep 10 files by default
            };

            configure?.Invoke(options);

            builder.AddProvider(new FileLoggerProvider(options));
            return builder;
        }
    }

    public class FileLoggerOptions
    {
        public string FilePath { get; set; }
        public long FileSizeLimitBytes { get; set; }
        public int RetainedFileCountLimit { get; set; }
        public bool Append { get; set; }
    }

    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly FileLoggerOptions _options;

        public FileLoggerProvider(FileLoggerOptions options)
        {
            _options = options;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(categoryName, _options);
        }

        public void Dispose() { }
    }

    public class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly FileLoggerOptions _options;
        private static readonly object _lock = new object();

        public FileLogger(string categoryName, FileLoggerOptions options)
        {
            _categoryName = categoryName;
            _options = options;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{logLevel}] {_categoryName}: {message}";
            if (exception != null)
            {
                logEntry += $"\n{exception}";
            }

            lock (_lock)
            {
                var filePath = _options.FilePath.Replace("{Date}", DateTime.UtcNow.ToString("yyyyMMdd"));
                
                try
                {
                    // Check file size and rotate if needed
                    if (File.Exists(filePath))
                    {
                        var fileInfo = new FileInfo(filePath);
                        if (fileInfo.Length >= _options.FileSizeLimitBytes)
                        {
                            RotateLogFiles(filePath);
                        }
                    }

                    // Write the log entry
                    File.AppendAllText(filePath, logEntry + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    // Fallback to console if file logging fails
                    Console.WriteLine($"Failed to write to log file: {ex.Message}");
                    Console.WriteLine(logEntry);
                }
            }
        }

        private void RotateLogFiles(string currentLogPath)
        {
            var directory = Path.GetDirectoryName(currentLogPath);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(currentLogPath);
            var extension = Path.GetExtension(currentLogPath);

            // Shift existing log files
            for (int i = _options.RetainedFileCountLimit - 1; i >= 1; i--)
            {
                var sourceFile = Path.Combine(directory, $"{fileNameWithoutExt}.{i}{extension}");
                var targetFile = Path.Combine(directory, $"{fileNameWithoutExt}.{i + 1}{extension}");

                if (File.Exists(sourceFile))
                {
                    if (File.Exists(targetFile))
                    {
                        File.Delete(targetFile);
                    }
                    File.Move(sourceFile, targetFile);
                }
            }

            // Move current log file
            var newFile = Path.Combine(directory, $"{fileNameWithoutExt}.1{extension}");
            if (File.Exists(newFile))
            {
                File.Delete(newFile);
            }
            File.Move(currentLogPath, newFile);
        }
    }
} 