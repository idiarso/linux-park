using System;
using System.ComponentModel.DataAnnotations;

namespace ParkIRC.Models
{
    public class CaptureTicket
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string TicketNumber { get; set; }
        
        [Required]
        public string ImagePath { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public string Status { get; set; } = "masuk";
        
        public string VehicleType { get; set; }
        
        public string PlateNumber { get; set; }
        
        public DateTime? ExitTimestamp { get; set; }
    }
} 