using System;

namespace ParkIRC.Models
{
    public class ParkingActivityViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string ParkingSpace { get; set; } = string.Empty;
        public DateTime EntryTime { get; set; }
        public DateTime? ExitTime { get; set; }
        public string Duration { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;

        // Helper property to determine parking status
        public string ParkingStatus 
        { 
            get 
            {
                if (ExitTime.HasValue)
                    return "keluar";
                else
                    return "terparkir";
            } 
        }

        // Helper properties for UI display
        public bool IsParked => !ExitTime.HasValue;
        public string DisplayStatus 
        { 
            get 
            {
                if (Status.ToLower() == "cancelled")
                    return "Dibatalkan";
                else if (ExitTime.HasValue)
                    return "Keluar";
                else
                    return "Terparkir"; 
            }
        }
        
        public string StatusBadgeClass
        {
            get
            {
                if (Status.ToLower() == "cancelled")
                    return "bg-danger";
                else if (ExitTime.HasValue)
                    return "bg-secondary";
                else
                    return "bg-success";
            }
        }
    }
} 