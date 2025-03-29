using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Models;
using ParkIRC;

namespace ParkIRC.Data
{
    public class ApplicationDbContext : IdentityDbContext<Operator>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ParkingSpace> ParkingSpaces { get; set; } = null!;
        public DbSet<Vehicle> Vehicles { get; set; } = null!;
        public DbSet<ParkingTransaction> ParkingTransactions { get; set; } = null!;
        public DbSet<Shift> Shifts { get; set; } = null!;
        public DbSet<Operator> Operators { get; set; } = null!;
        public DbSet<ParkingTicket> ParkingTickets { get; set; } = null!;
        public DbSet<Journal> Journals { get; set; } = null!;
        public DbSet<ParkingRate> ParkingRates { get; set; } = null!;
        public DbSet<CameraSettings> CameraSettings { get; set; } = null!;
        public DbSet<ParkingRateConfiguration> ParkingRateConfigurations { get; set; } = null!;
        public DbSet<EntryGate> EntryGates { get; set; } = null!;
        public DbSet<SiteSettings> SiteSettings { get; set; } = null!;
        public DbSet<PrinterConfig> PrinterConfigs { get; set; } = null!;
        public override DbSet<Operator> Users { get; set; } = null!;
        public override DbSet<IdentityRole> Roles { get; set; } = null!;
        public DbSet<SystemLog> SystemLogs { get; set; }
        public DbSet<HardwareStatus> HardwareStatuses { get; set; }
        public DbSet<AccessToken> AccessTokens { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<DeviceConfig> DeviceConfigs { get; set; }
        public DbSet<LoopDetector> LoopDetectors { get; set; }
        public DbSet<Camera> Cameras { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ParkingSpace>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SpaceNumber).IsRequired();
                entity.Property(e => e.SpaceType).IsRequired();
                entity.Property(e => e.HourlyRate).HasColumnType("decimal(18,2)");
                entity.Property(e => e.IsOccupied).IsRequired();
                entity.Property(e => e.IsReserved).IsRequired();
                entity.Property(e => e.Location);
                entity.Property(e => e.ReservedFor);
                
                entity.HasOne(e => e.CurrentVehicle)
                    .WithMany()
                    .HasForeignKey(p => p.CurrentVehicleId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.VehicleNumber).IsRequired();
                entity.Property(e => e.VehicleType).IsRequired();
                entity.Property(e => e.TicketNumber).IsRequired();
                
                entity.HasOne(e => e.ParkingSpace)
                    .WithMany()
                    .HasForeignKey(e => e.ParkingSpaceId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                entity.HasOne(e => e.Shift)
                    .WithMany(s => s.Vehicles)
                    .HasForeignKey(e => e.ShiftId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<ParkingTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TransactionNumber).IsRequired();
                entity.Property(e => e.HourlyRate).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ParkingSpaceId).HasColumnType("int");
                entity.HasOne(e => e.Vehicle)
                    .WithMany(v => v.Transactions)
                    .HasForeignKey(e => e.VehicleId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ParkingSpace)
                    .WithMany(p => p.Transactions)
                    .HasForeignKey(e => e.ParkingSpaceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Shift>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ShiftName).IsRequired();
                entity.Property(e => e.StartTime).IsRequired();
                entity.Property(e => e.EndTime).IsRequired();
            });

            builder.Entity<Operator>(entity =>
            {
                entity.Property(e => e.FullName).IsRequired();
                entity.HasMany(e => e.Shifts)
                    .WithMany(s => s.Operators)
                    .UsingEntity(j => j.ToTable("OperatorShifts"));
            });

            builder.Entity<ParkingTicket>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TicketNumber).IsRequired();
                entity.Property(e => e.BarcodeData).IsRequired();
                entity.HasOne(e => e.Vehicle)
                    .WithMany()
                    .HasForeignKey(e => e.VehicleId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.IssuedByOperator)
                    .WithMany()
                    .HasForeignKey(e => e.OperatorId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Shift)
                    .WithMany()
                    .HasForeignKey(e => e.ShiftId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Journal>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired();
                entity.Property(e => e.Description).IsRequired();
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.OperatorId).IsRequired();
                
                entity.HasOne(e => e.Operator)
                      .WithMany()
                      .HasForeignKey(e => e.OperatorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ParkingRate>(entity =>
            {
                entity.ToTable("parking_rate_configurations");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.VehicleType).HasColumnName("vehicle_type").IsRequired();
                entity.Property(e => e.BaseRate).HasColumnName("base_rate");
                entity.Property(e => e.HourlyRate).HasColumnName("hourly_rate");
                entity.Property(e => e.DailyRate).HasColumnName("daily_rate");
                entity.Property(e => e.WeeklyRate).HasColumnName("weekly_rate");
                entity.Property(e => e.MonthlyRate).HasColumnName("monthly_rate");
                entity.Property(e => e.PenaltyRate).HasColumnName("penalty_rate");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.LastModifiedBy).HasColumnName("last_modified_by");
                entity.Property(e => e.LastModifiedAt).HasColumnName("last_modified_at");
                entity.Property(e => e.MotorcycleRate).HasColumnName("motorcycle_rate");
                entity.Property(e => e.CarRate).HasColumnName("car_rate");
                entity.Property(e => e.AdditionalHourRate).HasColumnName("additional_hour_rate");
                entity.Property(e => e.MaximumDailyRate).HasColumnName("maximum_daily_rate");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            });

            builder.Entity<CameraSettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.GateId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.IpAddress).HasMaxLength(100);
                entity.Property(e => e.Port).HasMaxLength(10);
                entity.Property(e => e.Path).HasMaxLength(100);
                entity.Property(e => e.Username).HasMaxLength(50);
                entity.Property(e => e.Password).HasMaxLength(100);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            builder.Entity<EntryGate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Location).HasMaxLength(255);
                entity.Property(e => e.IpAddress).HasMaxLength(100);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            builder.Entity<SiteSettings>(entity =>
            {
                entity.ToTable("parking_settings");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.SiteName).HasColumnName("site_name");
                entity.Property(e => e.LogoPath).HasColumnName("logo_path");
                entity.Property(e => e.FaviconPath).HasColumnName("favicon_path");
                entity.Property(e => e.FooterText).HasColumnName("footer_text");
                entity.Property(e => e.ThemeColor).HasColumnName("theme_color");
                entity.Property(e => e.ShowLogo).HasColumnName("show_logo");
                entity.Property(e => e.LastUpdated).HasColumnName("last_updated");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
                entity.Property(e => e.PostgresConnectionString).HasColumnName("postgres_connection_string");
            });

            builder.Entity<PrinterConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Port).IsRequired();
                entity.Property(e => e.ConnectionType).HasDefaultValue("Serial");
                entity.Property(e => e.Status).HasDefaultValue("Offline");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.LastChecked).IsRequired();
            });

            builder.Entity<ParkingTransaction>()
                .HasIndex(p => p.TicketNumber)
                .IsUnique();

            builder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            builder.Entity<Role>()
                .HasIndex(r => r.Name)
                .IsUnique();
        }
    }
}