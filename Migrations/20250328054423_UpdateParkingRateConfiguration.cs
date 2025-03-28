using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ParkIRC.Migrations
{
    public partial class UpdateParkingRateConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SiteSettings",
                table: "SiteSettings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ParkingRates",
                table: "ParkingRates");

            migrationBuilder.RenameTable(
                name: "SiteSettings",
                newName: "parking_settings");

            migrationBuilder.RenameTable(
                name: "ParkingRates",
                newName: "parking_rate_configurations");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "parking_settings",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "parking_settings",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "ThemeColor",
                table: "parking_settings",
                newName: "theme_color");

            migrationBuilder.RenameColumn(
                name: "SiteName",
                table: "parking_settings",
                newName: "site_name");

            migrationBuilder.RenameColumn(
                name: "ShowLogo",
                table: "parking_settings",
                newName: "show_logo");

            migrationBuilder.RenameColumn(
                name: "PostgresConnectionString",
                table: "parking_settings",
                newName: "postgres_connection_string");

            migrationBuilder.RenameColumn(
                name: "LogoPath",
                table: "parking_settings",
                newName: "logo_path");

            migrationBuilder.RenameColumn(
                name: "LastUpdated",
                table: "parking_settings",
                newName: "last_updated");

            migrationBuilder.RenameColumn(
                name: "FooterText",
                table: "parking_settings",
                newName: "footer_text");

            migrationBuilder.RenameColumn(
                name: "FaviconPath",
                table: "parking_settings",
                newName: "favicon_path");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "parking_rate_configurations",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WeeklyRate",
                table: "parking_rate_configurations",
                newName: "weekly_rate");

            migrationBuilder.RenameColumn(
                name: "VehicleType",
                table: "parking_rate_configurations",
                newName: "vehicle_type");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "parking_rate_configurations",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "PenaltyRate",
                table: "parking_rate_configurations",
                newName: "penalty_rate");

            migrationBuilder.RenameColumn(
                name: "MotorcycleRate",
                table: "parking_rate_configurations",
                newName: "motorcycle_rate");

            migrationBuilder.RenameColumn(
                name: "MonthlyRate",
                table: "parking_rate_configurations",
                newName: "monthly_rate");

            migrationBuilder.RenameColumn(
                name: "MaximumDailyRate",
                table: "parking_rate_configurations",
                newName: "maximum_daily_rate");

            migrationBuilder.RenameColumn(
                name: "LastModifiedBy",
                table: "parking_rate_configurations",
                newName: "last_modified_by");

            migrationBuilder.RenameColumn(
                name: "LastModifiedAt",
                table: "parking_rate_configurations",
                newName: "last_modified_at");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "parking_rate_configurations",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "HourlyRate",
                table: "parking_rate_configurations",
                newName: "hourly_rate");

            migrationBuilder.RenameColumn(
                name: "EffectiveFrom",
                table: "parking_rate_configurations",
                newName: "effective_from");

            migrationBuilder.RenameColumn(
                name: "DailyRate",
                table: "parking_rate_configurations",
                newName: "daily_rate");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "parking_rate_configurations",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "parking_rate_configurations",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CarRate",
                table: "parking_rate_configurations",
                newName: "car_rate");

            migrationBuilder.RenameColumn(
                name: "BaseRate",
                table: "parking_rate_configurations",
                newName: "base_rate");

            migrationBuilder.RenameColumn(
                name: "AdditionalHourRate",
                table: "parking_rate_configurations",
                newName: "additional_hour_rate");

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "parking_settings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "updated_by",
                table: "parking_settings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "theme_color",
                table: "parking_settings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "#007bff");

            migrationBuilder.AlterColumn<string>(
                name: "site_name",
                table: "parking_settings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "show_logo",
                table: "parking_settings",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "postgres_connection_string",
                table: "parking_settings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "logo_path",
                table: "parking_settings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_updated",
                table: "parking_settings",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<string>(
                name: "footer_text",
                table: "parking_settings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "favicon_path",
                table: "parking_settings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "weekly_rate",
                table: "parking_rate_configurations",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "penalty_rate",
                table: "parking_rate_configurations",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "monthly_rate",
                table: "parking_rate_configurations",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "hourly_rate",
                table: "parking_rate_configurations",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "daily_rate",
                table: "parking_rate_configurations",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "base_rate",
                table: "parking_rate_configurations",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_parking_settings",
                table: "parking_settings",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_parking_rate_configurations",
                table: "parking_rate_configurations",
                column: "id");

            migrationBuilder.CreateTable(
                name: "ParkingRateConfigurations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ConfigurationName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AppliedBy = table.Column<string>(type: "text", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingRateConfigurations", x => x.id);
                    table.ForeignKey(
                        name: "FK_ParkingRateConfigurations_parking_rate_configurations_id",
                        column: x => x.id,
                        principalTable: "parking_rate_configurations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingRateConfigurations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_parking_settings",
                table: "parking_settings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_parking_rate_configurations",
                table: "parking_rate_configurations");

            migrationBuilder.RenameTable(
                name: "parking_settings",
                newName: "SiteSettings");

            migrationBuilder.RenameTable(
                name: "parking_rate_configurations",
                newName: "ParkingRates");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "SiteSettings",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "SiteSettings",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "theme_color",
                table: "SiteSettings",
                newName: "ThemeColor");

            migrationBuilder.RenameColumn(
                name: "site_name",
                table: "SiteSettings",
                newName: "SiteName");

            migrationBuilder.RenameColumn(
                name: "show_logo",
                table: "SiteSettings",
                newName: "ShowLogo");

            migrationBuilder.RenameColumn(
                name: "postgres_connection_string",
                table: "SiteSettings",
                newName: "PostgresConnectionString");

            migrationBuilder.RenameColumn(
                name: "logo_path",
                table: "SiteSettings",
                newName: "LogoPath");

            migrationBuilder.RenameColumn(
                name: "last_updated",
                table: "SiteSettings",
                newName: "LastUpdated");

            migrationBuilder.RenameColumn(
                name: "footer_text",
                table: "SiteSettings",
                newName: "FooterText");

            migrationBuilder.RenameColumn(
                name: "favicon_path",
                table: "SiteSettings",
                newName: "FaviconPath");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ParkingRates",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "weekly_rate",
                table: "ParkingRates",
                newName: "WeeklyRate");

            migrationBuilder.RenameColumn(
                name: "vehicle_type",
                table: "ParkingRates",
                newName: "VehicleType");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "ParkingRates",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "penalty_rate",
                table: "ParkingRates",
                newName: "PenaltyRate");

            migrationBuilder.RenameColumn(
                name: "motorcycle_rate",
                table: "ParkingRates",
                newName: "MotorcycleRate");

            migrationBuilder.RenameColumn(
                name: "monthly_rate",
                table: "ParkingRates",
                newName: "MonthlyRate");

            migrationBuilder.RenameColumn(
                name: "maximum_daily_rate",
                table: "ParkingRates",
                newName: "MaximumDailyRate");

            migrationBuilder.RenameColumn(
                name: "last_modified_by",
                table: "ParkingRates",
                newName: "LastModifiedBy");

            migrationBuilder.RenameColumn(
                name: "last_modified_at",
                table: "ParkingRates",
                newName: "LastModifiedAt");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "ParkingRates",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "hourly_rate",
                table: "ParkingRates",
                newName: "HourlyRate");

            migrationBuilder.RenameColumn(
                name: "effective_from",
                table: "ParkingRates",
                newName: "EffectiveFrom");

            migrationBuilder.RenameColumn(
                name: "daily_rate",
                table: "ParkingRates",
                newName: "DailyRate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "ParkingRates",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "ParkingRates",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "car_rate",
                table: "ParkingRates",
                newName: "CarRate");

            migrationBuilder.RenameColumn(
                name: "base_rate",
                table: "ParkingRates",
                newName: "BaseRate");

            migrationBuilder.RenameColumn(
                name: "additional_hour_rate",
                table: "ParkingRates",
                newName: "AdditionalHourRate");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "SiteSettings",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "SiteSettings",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ThemeColor",
                table: "SiteSettings",
                type: "text",
                nullable: false,
                defaultValue: "#007bff",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SiteName",
                table: "SiteSettings",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "ShowLogo",
                table: "SiteSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PostgresConnectionString",
                table: "SiteSettings",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LogoPath",
                table: "SiteSettings",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdated",
                table: "SiteSettings",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FooterText",
                table: "SiteSettings",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FaviconPath",
                table: "SiteSettings",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "WeeklyRate",
                table: "ParkingRates",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "PenaltyRate",
                table: "ParkingRates",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "MonthlyRate",
                table: "ParkingRates",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "HourlyRate",
                table: "ParkingRates",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "DailyRate",
                table: "ParkingRates",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "BaseRate",
                table: "ParkingRates",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SiteSettings",
                table: "SiteSettings",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ParkingRates",
                table: "ParkingRates",
                column: "Id");
        }
    }
}
