using System;

namespace ParkIRC.Models
{
    public class ParkingRate
    {
        public int Id { get; set; }
        public string VehicleType { get; set; } = string.Empty;
        public decimal BaseRate { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal DailyRate { get; set; }
        public decimal WeeklyRate { get; set; }
        public decimal MonthlyRate { get; set; }
        public decimal PenaltyRate { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string LastModifiedBy { get; set; } = string.Empty;
        public DateTime LastModifiedAt { get; set; }
        public bool IsActive { get; set; }
        public decimal MotorcycleRate { get; set; }
        public decimal CarRate { get; set; }
        public decimal AdditionalHourRate { get; set; }
        public decimal MaximumDailyRate { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }
} 