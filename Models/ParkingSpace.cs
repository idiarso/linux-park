using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkIRC.Models
{
    public class ParkingSpace
    {
        public ParkingSpace()
        {
            // Initialize collections
            Transactions = new List<ParkingTransaction>();
            
            // Initialize required string properties
            SpaceNumber = string.Empty;
            SpaceType = string.Empty;
            Location = string.Empty;
            
            // Set default value for IsActive
            IsActive = true;
        }
        
        public int Id { get; set; }
        
        [Required]
        public string SpaceNumber { get; set; }
        
        [Required]
        public string SpaceType { get; set; }
        
        public bool IsOccupied { get; set; }
        
        public bool IsReserved { get; set; }
        
        public string Location { get; set; }
        
        public string? ReservedFor { get; set; }
        
        public DateTime? LastOccupiedTime { get; set; }
        
        public decimal HourlyRate { get; set; }
        
        public int? CurrentVehicleId { get; set; }
        
        public bool IsActive { get; set; }
        
        [InverseProperty("ParkingSpace")]
        public virtual Vehicle? CurrentVehicle { get; set; }
        
        public virtual ICollection<ParkingTransaction> Transactions { get; set; }
        
        public string VehicleType => SpaceType;
    }
}