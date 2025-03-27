using System;

namespace ParkIRC.Models
{
    public class LoopDetector
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsOccupied { get; set; }
        public bool IsActive { get; set; }
        public int SensitivityLevel { get; set; }
        public int DetectionThreshold { get; set; }
        public string Configuration { get; set; } = string.Empty;
        public DateTime LastChecked { get; set; }
        public string LastError { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string LastModifiedBy { get; set; } = string.Empty;
        public DateTime LastModifiedAt { get; set; }
        public virtual Device Device { get; set; } = null!;
        public int DeviceId { get; set; }
    }
} 