using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPGLLC.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleIdentificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BodyStyle",
                table: "CustomerVehicles",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Brake",
                table: "CustomerVehicles",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "CustomerVehicles",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriveType",
                table: "CustomerVehicles",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EngineFuel",
                table: "CustomerVehicles",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FleetNumber",
                table: "CustomerVehicles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gvw",
                table: "CustomerVehicles",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Memo",
                table: "CustomerVehicles",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StateProvince",
                table: "CustomerVehicles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Submodel",
                table: "CustomerVehicles",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Transmission",
                table: "CustomerVehicles",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitNumber",
                table: "CustomerVehicles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyStyle",
                table: "AppointmentRequests",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Brake",
                table: "AppointmentRequests",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "AppointmentRequests",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriveType",
                table: "AppointmentRequests",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EngineFuel",
                table: "AppointmentRequests",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FleetNumber",
                table: "AppointmentRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gvw",
                table: "AppointmentRequests",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicensePlate",
                table: "AppointmentRequests",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StateProvince",
                table: "AppointmentRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Transmission",
                table: "AppointmentRequests",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitNumber",
                table: "AppointmentRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleMemo",
                table: "AppointmentRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleSubmodel",
                table: "AppointmentRequests",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BodyStyle",
                table: "CustomerVehicles");

            migrationBuilder.DropColumn(
                name: "Brake",
                table: "CustomerVehicles");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "CustomerVehicles");

            migrationBuilder.DropColumn(
                name: "DriveType",
                table: "CustomerVehicles");

            migrationBuilder.DropColumn(
                name: "EngineFuel",
                table: "CustomerVehicles");

            migrationBuilder.DropColumn(
                name: "FleetNumber",
                table: "CustomerVehicles");

            migrationBuilder.DropColumn(
                name: "Gvw",
                table: "CustomerVehicles");

            migrationBuilder.DropColumn(
                name: "Memo",
                table: "CustomerVehicles");

            migrationBuilder.DropColumn(
                name: "StateProvince",
                table: "CustomerVehicles");

            migrationBuilder.DropColumn(
                name: "Submodel",
                table: "CustomerVehicles");

            migrationBuilder.DropColumn(
                name: "Transmission",
                table: "CustomerVehicles");

            migrationBuilder.DropColumn(
                name: "UnitNumber",
                table: "CustomerVehicles");

            migrationBuilder.DropColumn(
                name: "BodyStyle",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "Brake",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "DriveType",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "EngineFuel",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "FleetNumber",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "Gvw",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "LicensePlate",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "StateProvince",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "Transmission",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "UnitNumber",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "VehicleMemo",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "VehicleSubmodel",
                table: "AppointmentRequests");
        }
    }
}
