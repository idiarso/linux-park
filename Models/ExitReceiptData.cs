using System;

namespace ParkIRC.Models
{
    public class ExitReceiptData
    {
        public string VehicleNumber { get; set; } = string.Empty;
        public DateTime EntryTime { get; set; }
        public DateTime ExitTime { get; set; }
        public string Duration { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TransactionId { get; set; } = string.Empty;
    }
} 