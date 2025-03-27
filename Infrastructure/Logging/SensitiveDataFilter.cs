using Microsoft.Extensions.Logging;
using System;

namespace ParkIRC.Infrastructure.Logging
{
    public class SensitiveDataFilter : ILoggerProvider
    {
        private readonly LogLevel _minLevel;

        public SensitiveDataFilter(LogLevel minLevel = LogLevel.Information)
        {
            _minLevel = minLevel;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new SensitiveDataLogger(categoryName, _minLevel);
        }

        public void Dispose() { }
    }

    public class SensitiveDataLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly LogLevel _minLevel;

        private static readonly string[] SensitivePatterns = new[]
        {
            "password",
            "secret",
            "token",
            "apikey",
            "connectionstring",
            "pwd",
            "auth"
        };

        public SensitiveDataLogger(string categoryName, LogLevel minLevel)
        {
            _categoryName = categoryName;
            _minLevel = minLevel;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

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

            // Filter out sensitive data
            message = FilterSensitiveData(message);

            // Remove file paths
            message = FilterFilePaths(message);

            // Remove stack traces in production
            if (!IsDevelopment() && exception != null)
            {
                message = $"Error: {exception.Message}";
            }

            // Write to console (you can modify this to write to file or other destinations)
            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{logLevel}] {_categoryName}: {message}");
        }

        private string FilterSensitiveData(string message)
        {
            if (string.IsNullOrEmpty(message)) return message;

            var filteredMessage = message;
            foreach (var pattern in SensitivePatterns)
            {
                // Match pattern followed by any non-whitespace characters
                var regex = new System.Text.RegularExpressions.Regex(
                    $@"{pattern}[^\s]*\s*[:=]\s*[^\s]+",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                filteredMessage = regex.Replace(filteredMessage, $"{pattern}=*****");
            }

            return filteredMessage;
        }

        private string FilterFilePaths(string message)
        {
            if (string.IsNullOrEmpty(message)) return message;

            // Filter Windows-style paths
            var windowsPathRegex = new System.Text.RegularExpressions.Regex(@"[A-Za-z]:\\[^\s/:\*\?""<>\|]+");
            message = windowsPathRegex.Replace(message, "[FILTERED_PATH]");

            // Filter Unix-style paths
            var unixPathRegex = new System.Text.RegularExpressions.Regex(@"/(?:[^/\s]+/)*[^/\s]+");
            message = unixPathRegex.Replace(message, "[FILTERED_PATH]");

            return message;
        }

        private bool IsDevelopment()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
        }
    }
} 