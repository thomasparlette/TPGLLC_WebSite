using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPGLLC.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialWebsiteAndCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentRequests",
                columns: table => new
                {
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VehicleType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    VehicleYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    VehicleMake = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    VehicleModel = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Vin = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: true),
                    Mileage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PreferredDate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PreferredTime = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ServiceNeeded = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Company = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentRequests", x => x.RequestId);
                });

            migrationBuilder.CreateTable(
                name: "VehicleCatalogEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ModelYear = table.Column<int>(type: "int", nullable: false),
                    MakeId = table.Column<int>(type: "int", nullable: false),
                    ModelId = table.Column<int>(type: "int", nullable: false),
                    Make = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SyncedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleCatalogEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_Status",
                table: "AppointmentRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_SubmittedAtUtc",
                table: "AppointmentRequests",
                column: "SubmittedAtUtc");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentRequests");

            migrationBuilder.DropTable(
                name: "VehicleCatalogEntries");
        }
    }
}
