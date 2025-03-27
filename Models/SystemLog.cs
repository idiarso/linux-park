using System;

namespace ParkIRC.Models
{
    public class SystemLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
        public string? StackTrace { get; set; }
        public string? Source { get; set; }
        public string? UserId { get; set; }
        public string? RequestPath { get; set; }
        public string? RequestMethod { get; set; }
        public string? UserAgent { get; set; }
        public string? IpAddress { get; set; }
    }
} 