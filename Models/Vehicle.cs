using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkIRC.Models
{
    public class Vehicle
    {
        public Vehicle()
        {
            // Initialize collections
            Transactions = new List<ParkingTransaction>();
            
            // Initialize required string properties
            VehicleNumber = string.Empty;
            VehicleType = string.Empty;
            TicketNumber = string.Empty;
            CreatedBy = string.Empty;
            PlateNumber = string.Empty;
            Status = "Active";
            ExternalSystemId = string.Empty;
            EntryGateId = string.Empty;
            ExitGateId = string.Empty;
        }
        
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Nomor kendaraan wajib diisi")]
        public string VehicleNumber { get; set; }
        
        [Required(ErrorMessage = "Tipe kendaraan wajib diisi")]
        public string VehicleType { get; set; }
        
        [Required(ErrorMessage = "Nomor tiket wajib diisi")]
        public string TicketNumber { get; set; }
        
        public int VehicleTypeId { get; set; }
        
        public string? DriverName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ContactNumber { get; set; }
        
        public DateTime EntryTime { get; set; }
        public DateTime? ExitTime { get; set; }
        public bool IsParked { get; set; }
        
        public string? EntryImagePath { get; set; }
        public string? ExitImagePath { get; set; }
        public string? BarcodeImagePath { get; set; }
        
        public int? ParkingSpaceId { get; set; }
        
        [ForeignKey("ParkingSpaceId")]
        public virtual ParkingSpace? ParkingSpace { get; set; }
        
        public virtual ICollection<ParkingTransaction> Transactions { get; set; }
        
        public int? ShiftId { get; set; }
        
        [ForeignKey("ShiftId")]
        public virtual Shift? Shift { get; set; }
        
        [Required]
        public string CreatedBy { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public bool IsActive { get; set; } = true;

        [Required]
        public string PlateNumber { get; set; }
        
        [Required]
        public string Status { get; set; } // Active, Exited
        
        public string ExternalSystemId { get; set; } // ID dari sistem Python jika diperlukan
        public string EntryGateId { get; set; }
        public string ExitGateId { get; set; }
        public decimal? ParkingFee { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}