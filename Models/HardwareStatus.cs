using System;

namespace ParkIRC.Models
{
    public class HardwareStatus
    {
        public int Id { get; set; }
        public string DeviceType { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime LastChecked { get; set; }
        public string? LastError { get; set; }
        public int ErrorCount { get; set; }
        public bool IsOnline { get; set; }
        public bool IsBackupActive { get; set; }
        public string? BackupDeviceName { get; set; }
        public DateTime? LastBackupSwitch { get; set; }
        public bool IsCameraOnline { get; set; }
        public bool IsGateOnline { get; set; }
        public bool IsPrinterOnline { get; set; }
        public bool IsLoopDetectorOnline { get; set; }
        public string DeviceId { get; set; } = string.Empty;
    }
} 