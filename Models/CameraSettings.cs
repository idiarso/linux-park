using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkIRC.Models
{
    public class CameraSettings
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string GateId { get; set; } = "GATE1";
        
        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "Webcam"; // "Webcam" or "IPCamera"
        
        [StringLength(100)]
        public string? IpAddress { get; set; }
        
        [StringLength(10)]
        public string? Port { get; set; }
        
        [StringLength(100)]
        public string? Path { get; set; }
        
        [StringLength(50)]
        public string? Username { get; set; }
        
        [StringLength(100)]
        public string? Password { get; set; }
        
        // Camera profile and settings properties
        [StringLength(50)]
        public string ProfileName { get; set; } = "Default";
        
        public int ResolutionWidth { get; set; } = 1280;
        
        public int ResolutionHeight { get; set; } = 720;
        
        public int ROI_X { get; set; } = 0;
        
        public int ROI_Y { get; set; } = 0;
        
        public int ROI_Width { get; set; } = 640;
        
        public int ROI_Height { get; set; } = 480;
        
        public int Exposure { get; set; } = 100;
        
        public double Gain { get; set; } = 1.0;
        
        public int Brightness { get; set; } = 50;
        
        public int Contrast { get; set; } = 50;
        
        [StringLength(50)]
        public string LightingCondition { get; set; } = "Normal";
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
        
        [StringLength(100)]
        public string? UpdatedBy { get; set; }
    }
} 