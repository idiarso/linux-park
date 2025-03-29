using Microsoft.Extensions.Logging;
using System;

namespace ParkIRC.Services
{
    /// <summary>
    /// Service for handling time-related operations
    /// </summary>
    public class TimeService
    {
        private readonly ILogger<TimeService> _logger;
        private TimeZoneInfo _timeZoneInfo;

        public TimeService(ILogger<TimeService> logger)
        {
            _logger = logger;
            try
            {
                // Try to use the local system time zone
                _timeZoneInfo = TimeZoneInfo.Local;
                _logger.LogInformation($"Using local time zone: {_timeZoneInfo.DisplayName}");
            }
            catch (Exception ex)
            {
                // Fall back to UTC if local time zone can't be determined
                _logger.LogError(ex, "Failed to get local time zone, falling back to UTC");
                _timeZoneInfo = TimeZoneInfo.Utc;
            }
        }

        /// <summary>
        /// Sets the time zone to use for time operations
        /// </summary>
        /// <param name="timeZoneId">The time zone ID (e.g., "Asia/Jakarta")</param>
        public void SetTimeZone(string timeZoneId)
        {
            try
            {
                _timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                _logger.LogInformation($"Time zone set to: {_timeZoneInfo.DisplayName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to set time zone to {timeZoneId}, keeping current time zone");
            }
        }

        /// <summary>
        /// Gets the current time in the configured time zone
        /// </summary>
        /// <returns>The current time</returns>
        public DateTime GetCurrentTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZoneInfo);
        }

        /// <summary>
        /// Converts a UTC time to the configured time zone
        /// </summary>
        /// <param name="utcTime">The UTC time to convert</param>
        /// <returns>The converted time</returns>
        public DateTime ConvertFromUtc(DateTime utcTime)
        {
            if (utcTime.Kind != DateTimeKind.Utc)
            {
                utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
            }
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, _timeZoneInfo);
        }

        /// <summary>
        /// Converts a time from the configured time zone to UTC
        /// </summary>
        /// <param name="localTime">The local time to convert</param>
        /// <returns>The UTC time</returns>
        public DateTime ConvertToUtc(DateTime localTime)
        {
            if (localTime.Kind == DateTimeKind.Utc)
            {
                return localTime;
            }
            return TimeZoneInfo.ConvertTimeToUtc(localTime, _timeZoneInfo);
        }

        /// <summary>
        /// Gets the current date in the configured time zone
        /// </summary>
        /// <returns>The current date</returns>
        public DateTime GetCurrentDate()
        {
            return GetCurrentTime().Date;
        }

        /// <summary>
        /// Gets the start of the day for a given date
        /// </summary>
        /// <param name="date">The date</param>
        /// <returns>The start of the day</returns>
        public DateTime GetStartOfDay(DateTime date)
        {
            return date.Date;
        }

        /// <summary>
        /// Gets the end of the day for a given date
        /// </summary>
        /// <param name="date">The date</param>
        /// <returns>The end of the day</returns>
        public DateTime GetEndOfDay(DateTime date)
        {
            return date.Date.AddDays(1).AddTicks(-1);
        }
    }
} 