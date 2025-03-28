﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using ParkIRC.Data;

#nullable disable

namespace ParkIRC.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.27")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex");

                    b.ToTable("AspNetRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<string>("RoleId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasColumnType("text");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("text");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("text");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("text");

                    b.Property<string>("RoleId")
                        .HasColumnType("text");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("text");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Value")
                        .HasColumnType("text");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens", (string)null);
                });

            modelBuilder.Entity("OperatorShift", b =>
                {
                    b.Property<string>("OperatorsId")
                        .HasColumnType("text");

                    b.Property<int>("ShiftsId")
                        .HasColumnType("integer");

                    b.HasKey("OperatorsId", "ShiftsId");

                    b.HasIndex("ShiftsId");

                    b.ToTable("OperatorShifts", (string)null);
                });

            modelBuilder.Entity("ParkIRC.Models.CameraSettings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("Brightness")
                        .HasColumnType("integer");

                    b.Property<int>("Contrast")
                        .HasColumnType("integer");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int>("Exposure")
                        .HasColumnType("integer");

                    b.Property<double>("Gain")
                        .HasColumnType("double precision");

                    b.Property<bool>("IsActive")
                        .HasColumnType("boolean");

                    b.Property<string>("LightingCondition")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text")
                        .HasDefaultValue("Normal");

                    b.Property<DateTime?>("ModifiedAt")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("ProfileName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("ROI_Height")
                        .HasColumnType("integer");

                    b.Property<int>("ROI_Width")
                        .HasColumnType("integer");

                    b.Property<int>("ROI_X")
                        .HasColumnType("integer");

                    b.Property<int>("ROI_Y")
                        .HasColumnType("integer");

                    b.Property<int>("ResolutionHeight")
                        .HasColumnType("integer");

                    b.Property<int>("ResolutionWidth")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("CameraSettings", (string)null);
                });

            modelBuilder.Entity("ParkIRC.Models.EntryGate", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("IpAddress")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<bool>("IsOnline")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsOpen")
                        .HasColumnType("boolean");

                    b.Property<DateTime?>("LastActivity")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime>("LastActivityTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Location")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<int>("PortNumber")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("EntryGates", (string)null);
                });

            modelBuilder.Entity("ParkIRC.Models.Journal", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("OperatorId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id");

                    b.HasIndex("OperatorId");

                    b.ToTable("Journals", (string)null);
                });

            modelBuilder.Entity("ParkIRC.Models.Operator", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("integer");

                    b.Property<string>("BadgeNumber")
                        .HasColumnType("text");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("boolean");

                    b.Property<string>("FullName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("IsActive")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("JoinDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime?>("LastModifiedAt")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("LastModifiedBy")
                        .HasColumnType("text");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("text");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("text");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("boolean");

                    b.Property<string>("PhotoPath")
                        .HasColumnType("text");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("text");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("boolean");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex");

                    b.ToTable("AspNetUsers", (string)null);
                });

            modelBuilder.Entity("ParkIRC.Models.ParkingRateConfiguration", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<decimal>("AdditionalHourRate")
                        .HasColumnType("numeric(18,2)");

                    b.Property<decimal>("BaseRate")
                        .HasColumnType("numeric(18,2)");

                    b.Property<decimal>("CarRate")
                        .HasColumnType("numeric(18,2)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("CreatedBy")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("DailyRate")
                        .HasColumnType("numeric(18,2)");

                    b.Property<DateTime>("EffectiveFrom")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime?>("EffectiveTo")
                        .HasColumnType("timestamp without time zone");

                    b.Property<decimal>("HourlyRate")
                        .HasColumnType("numeric(18,2)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("boolean");

                    b.Property<DateTime?>("LastModifiedAt")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("LastModifiedBy")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("timestamp without time zone");

                    b.Property<decimal>("MaximumDailyRate")
                        .HasColumnType("numeric(18,2)");

                    b.Property<decimal>("MonthlyRate")
                        .HasColumnType("numeric(18,2)");

                    b.Property<decimal>("MotorcycleRate")
                        .HasColumnType("numeric(18,2)");

                    b.Property<decimal>("PenaltyRate")
                        .HasColumnType("numeric(18,2)");

                    b.Property<string>("UpdatedBy")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("VehicleType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<decimal>("WeeklyRate")
                        .HasColumnType("numeric(18,2)");

                    b.HasKey("Id");

                    b.ToTable("ParkingRateConfigurations", (string)null);
                });

            modelBuilder.Entity("ParkIRC.Models.ParkingSpace", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int?>("CurrentVehicleId")
                        .HasColumnType("integer");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<decimal>("HourlyRate")
                        .HasColumnType("numeric(18,2)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsOccupied")
                        .HasColumnType("boolean");

                    b.Property<DateTime?>("LastOccupiedTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("SpaceNumber")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("SpaceType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("CurrentVehicleId")
                        .IsUnique();

                    b.ToTable("ParkingSpaces", (string)null);
                });

            modelBuilder.Entity("ParkIRC.Models.ParkingTicket", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("BarcodeData")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("BarcodeImagePath")
                        .HasColumnType("text");

                    b.Property<DateTime>("EntryTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<bool>("IsUsed")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsValid")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("IssueTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime>("IssuedAt")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("OperatorId")
                        .HasColumnType("text");

                    b.Property<string>("QRCodeImage")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime?>("ScanTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int>("ShiftId")
                        .HasColumnType("integer");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("TicketNumber")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime?>("UsedAt")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int?>("VehicleId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("OperatorId");

                    b.HasIndex("ShiftId");

                    b.HasIndex("VehicleId");

                    b.ToTable("ParkingTickets", (string)null);
                });

            modelBuilder.Entity("ParkIRC.Models.ParkingTransaction", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<decimal>("Amount")
                        .HasColumnType("numeric(18,2)");

                    b.Property<string>("EntryPoint")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("EntryTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime?>("ExitTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<decimal>("HourlyRate")
                        .HasColumnType("numeric(18,2)");

                    b.Property<string>("ImagePath")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("IsManualEntry")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsOfflineEntry")
                        .HasColumnType("boolean");

                    b.Property<string>("OperatorId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("ParkingSpaceId")
                        .HasColumnType("int");

                    b.Property<decimal>("PaymentAmount")
                        .HasColumnType("numeric(18,2)");

                    b.Property<string>("PaymentMethod")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("PaymentStatus")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime?>("PaymentTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("TicketNumber")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("TotalAmount")
                        .HasColumnType("numeric(18,2)");

                    b.Property<string>("TransactionNumber")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("VehicleId")
                        .HasColumnType("integer");

                    b.Property<string>("VehicleImagePath")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("VehicleNumber")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("VehicleType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ParkingSpaceId");

                    b.HasIndex("VehicleId");

                    b.ToTable("ParkingTransactions", (string)null);
                });

            modelBuilder.Entity("ParkIRC.Models.PrinterConfig", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("ConnectionType")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text")
                        .HasDefaultValue("Serial");

                    b.Property<string>("IpAddress")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<DateTime>("LastChecked")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Port")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Status")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text")
                        .HasDefaultValue("Offline");

                    b.Property<int?>("TcpPort")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("PrinterConfigs", (string)null);
                });

            modelBuilder.Entity("ParkIRC.Models.Shift", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime>("Date")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("EndTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<bool>("IsActive")
                        .HasColumnType("boolean");

                    b.Property<int>("MaxOperators")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ShiftName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("WorkDaysString")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("WorkDays");

                    b.HasKey("Id");

                    b.ToTable("Shifts", (string)null);
                });

            modelBuilder.Entity("ParkIRC.Models.SiteSettings", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("FaviconPath")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("FooterText")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("LogoPath")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("ShowLogo")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<string>("SiteName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ThemeColor")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text")
                        .HasDefaultValue("#007bff");

                    b.Property<string>("UpdatedBy")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("SiteSettings", (string)null);
                });

            modelBuilder.Entity("ParkIRC.Models.Vehicle", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("BarcodeImagePath")
                        .HasColumnType("text");

                    b.Property<string>("ContactNumber")
                        .HasColumnType("text");

                    b.Property<string>("DriverName")
                        .HasColumnType("text");

                    b.Property<string>("EntryPhotoPath")
                        .HasColumnType("text");

                    b.Property<DateTime>("EntryTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("ExitPhotoPath")
                        .HasColumnType("text");

                    b.Property<DateTime?>("ExitTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<bool>("IsParked")
                        .HasColumnType("boolean");

                    b.Property<int?>("ParkingSpaceId")
                        .HasColumnType("integer");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("text");

                    b.Property<int?>("ShiftId")
                        .HasColumnType("integer");

                    b.Property<string>("VehicleNumber")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("VehicleType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ShiftId");

                    b.ToTable("Vehicles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("ParkIRC.Models.Operator", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("ParkIRC.Models.Operator", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ParkIRC.Models.Operator", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("ParkIRC.Models.Operator", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OperatorShift", b =>
                {
                    b.HasOne("ParkIRC.Models.Operator", null)
                        .WithMany()
                        .HasForeignKey("OperatorsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ParkIRC.Models.Shift", null)
                        .WithMany()
                        .HasForeignKey("ShiftsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ParkIRC.Models.Journal", b =>
                {
                    b.HasOne("ParkIRC.Models.Operator", "Operator")
                        .WithMany()
                        .HasForeignKey("OperatorId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Operator");
                });

            modelBuilder.Entity("ParkIRC.Models.ParkingSpace", b =>
                {
                    b.HasOne("ParkIRC.Models.Vehicle", "CurrentVehicle")
                        .WithOne("ParkingSpace")
                        .HasForeignKey("ParkIRC.Models.ParkingSpace", "CurrentVehicleId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("CurrentVehicle");
                });

            modelBuilder.Entity("ParkIRC.Models.ParkingTicket", b =>
                {
                    b.HasOne("ParkIRC.Models.Operator", "IssuedByOperator")
                        .WithMany()
                        .HasForeignKey("OperatorId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("ParkIRC.Models.Shift", "Shift")
                        .WithMany()
                        .HasForeignKey("ShiftId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ParkIRC.Models.Vehicle", "Vehicle")
                        .WithMany()
                        .HasForeignKey("VehicleId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("IssuedByOperator");

                    b.Navigation("Shift");

                    b.Navigation("Vehicle");
                });

            modelBuilder.Entity("ParkIRC.Models.ParkingTransaction", b =>
                {
                    b.HasOne("ParkIRC.Models.ParkingSpace", "ParkingSpace")
                        .WithMany("Transactions")
                        .HasForeignKey("ParkingSpaceId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ParkIRC.Models.Vehicle", "Vehicle")
                        .WithMany("Transactions")
                        .HasForeignKey("VehicleId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("ParkingSpace");

                    b.Navigation("Vehicle");
                });

            modelBuilder.Entity("ParkIRC.Models.Vehicle", b =>
                {
                    b.HasOne("ParkIRC.Models.Shift", "Shift")
                        .WithMany("Vehicles")
                        .HasForeignKey("ShiftId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("Shift");
                });

            modelBuilder.Entity("ParkIRC.Models.ParkingSpace", b =>
                {
                    b.Navigation("Transactions");
                });

            modelBuilder.Entity("ParkIRC.Models.Shift", b =>
                {
                    b.Navigation("Vehicles");
                });

            modelBuilder.Entity("ParkIRC.Models.Vehicle", b =>
                {
                    b.Navigation("ParkingSpace");

                    b.Navigation("Transactions");
                });
#pragma warning restore 612, 618
        }
    }
}
