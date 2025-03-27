using System;

namespace ParkIRC.Models
{
    public class Camera
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string Resolution { get; set; } = string.Empty;
        public string ImageFormat { get; set; } = string.Empty;
        public int FrameRate { get; set; }
        public string Configuration { get; set; } = string.Empty;
        public DateTime LastChecked { get; set; }
        public string LastError { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string LastModifiedBy { get; set; } = string.Empty;
        public DateTime LastModifiedAt { get; set; }
        public virtual Device Device { get; set; } = null!;
        public int DeviceId { get; set; }
        public virtual CameraSettings Settings { get; set; } = null!;
        public int SettingsId { get; set; }
    }
} 