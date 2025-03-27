using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace ParkIRC.Models
{
    [Table("ParkingRateConfigurations")]
    public class ParkingRateConfiguration : ParkingRate
    {
        public new int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Jenis Kendaraan")]
        public new string VehicleType { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Tarif dasar tidak boleh negatif")]
        [Display(Name = "Tarif Dasar")]
        public new decimal BaseRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Tarif per jam tidak boleh negatif")]
        [Display(Name = "Tarif per Jam")]
        public new decimal HourlyRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Tarif harian tidak boleh negatif")]
        [Display(Name = "Tarif Harian")]
        public new decimal DailyRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Tarif mingguan tidak boleh negatif")]
        [Display(Name = "Tarif Mingguan")]
        public new decimal WeeklyRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Tarif bulanan tidak boleh negatif")]
        [Display(Name = "Tarif Bulanan")]
        public new decimal MonthlyRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Tarif denda tidak boleh negatif")]
        [Display(Name = "Tarif Denda")]
        public new decimal PenaltyRate { get; set; }

        [Display(Name = "Status Aktif")]
        public new bool IsActive { get; set; } = true;

        [Required]
        [Display(Name = "Berlaku Sejak")]
        public new DateTime EffectiveFrom { get; set; }

        [Display(Name = "Berlaku Sampai")]
        public DateTime? EffectiveTo { get; set; }

        [Required]
        [Display(Name = "Dibuat Oleh")]
        public new string CreatedBy { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Dibuat Pada")]
        public new DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "Diubah Oleh")]
        public new string LastModifiedBy { get; set; } = string.Empty;

        [Display(Name = "Diubah Pada")]
        public new DateTime? LastModifiedAt { get; set; }

        // Additional properties referenced in controllers
        [Column(TypeName = "decimal(18,2)")]
        public new decimal MotorcycleRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public new decimal CarRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public new decimal AdditionalHourRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public new decimal MaximumDailyRate { get; set; }

        public DateTime LastUpdated { get; set; }
        public new string UpdatedBy { get; set; } = string.Empty;

        // Custom validation
        public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            // Validate that daily rate is less than 24 hours of hourly rate
            if (DailyRate >= HourlyRate * 24)
            {
                results.Add(new System.ComponentModel.DataAnnotations.ValidationResult(
                    "Tarif harian harus lebih murah dari akumulasi tarif per jam selama 24 jam",
                    new[] { nameof(DailyRate) }));
            }

            // Validate that weekly rate is less than 7 days of daily rate
            if (WeeklyRate >= DailyRate * 7)
            {
                results.Add(new System.ComponentModel.DataAnnotations.ValidationResult(
                    "Tarif mingguan harus lebih murah dari akumulasi tarif harian selama 7 hari",
                    new[] { nameof(WeeklyRate) }));
            }

            // Validate that monthly rate is less than 30 days of daily rate
            if (MonthlyRate >= DailyRate * 30)
            {
                results.Add(new System.ComponentModel.DataAnnotations.ValidationResult(
                    "Tarif bulanan harus lebih murah dari akumulasi tarif harian selama 30 hari",
                    new[] { nameof(MonthlyRate) }));
            }

            // Validate effective dates
            if (EffectiveTo.HasValue && EffectiveTo.Value <= EffectiveFrom)
            {
                results.Add(new System.ComponentModel.DataAnnotations.ValidationResult(
                    "Tanggal berlaku akhir harus setelah tanggal berlaku awal",
                    new[] { nameof(EffectiveTo) }));
            }

            return results;
        }

        public string ConfigurationName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string AppliedBy { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
    }
}