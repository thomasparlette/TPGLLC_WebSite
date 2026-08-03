using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPGLLC.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCompanyandVehicleType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleCatalogEntries_VehicleType_ModelYear_Make_Model",
                table: "VehicleCatalogEntries");

            migrationBuilder.DropIndex(
                name: "IX_VehicleCatalogEntries_VehicleType_ModelYear_MakeId_ModelId",
                table: "VehicleCatalogEntries");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "VehicleCatalogEntries");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "CustomerVehicles");

            migrationBuilder.DropColumn(
                name: "Company",
                table: "CustomerProfiles");

            migrationBuilder.DropColumn(
                name: "Company",
                table: "AppointmentRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "VehicleCatalogEntries",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "CustomerVehicles",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Company",
                table: "CustomerProfiles",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Company",
                table: "AppointmentRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleCatalogEntries_VehicleType_ModelYear_Make_Model",
                table: "VehicleCatalogEntries",
                columns: new[] { "VehicleType", "ModelYear", "Make", "Model" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleCatalogEntries_VehicleType_ModelYear_MakeId_ModelId",
                table: "VehicleCatalogEntries",
                columns: new[] { "VehicleType", "ModelYear", "MakeId", "ModelId" },
                unique: true);
        }
    }
}
