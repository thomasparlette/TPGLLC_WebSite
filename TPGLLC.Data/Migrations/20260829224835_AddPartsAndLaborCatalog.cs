using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPGLLC.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPartsAndLaborCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PartsCatalogItemId",
                table: "ServiceHistoryParts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LaborCatalogItemId",
                table: "ServiceHistoryJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LaborHours",
                table: "ServiceHistoryJobs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LaborRate",
                table: "ServiceHistoryJobs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LaborCatalogItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DefaultHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaborCatalogItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartsCatalogItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartsCatalogItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceHistoryParts_PartsCatalogItemId",
                table: "ServiceHistoryParts",
                column: "PartsCatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceHistoryJobs_LaborCatalogItemId",
                table: "ServiceHistoryJobs",
                column: "LaborCatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LaborCatalogItems_Code",
                table: "LaborCatalogItems",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LaborCatalogItems_IsActive_Name",
                table: "LaborCatalogItems",
                columns: new[] { "IsActive", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_PartsCatalogItems_IsActive_Name",
                table: "PartsCatalogItems",
                columns: new[] { "IsActive", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_PartsCatalogItems_PartNumber",
                table: "PartsCatalogItems",
                column: "PartNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceHistoryJobs_LaborCatalogItems_LaborCatalogItemId",
                table: "ServiceHistoryJobs",
                column: "LaborCatalogItemId",
                principalTable: "LaborCatalogItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceHistoryParts_PartsCatalogItems_PartsCatalogItemId",
                table: "ServiceHistoryParts",
                column: "PartsCatalogItemId",
                principalTable: "PartsCatalogItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceHistoryJobs_LaborCatalogItems_LaborCatalogItemId",
                table: "ServiceHistoryJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceHistoryParts_PartsCatalogItems_PartsCatalogItemId",
                table: "ServiceHistoryParts");

            migrationBuilder.DropTable(
                name: "LaborCatalogItems");

            migrationBuilder.DropTable(
                name: "PartsCatalogItems");

            migrationBuilder.DropIndex(
                name: "IX_ServiceHistoryParts_PartsCatalogItemId",
                table: "ServiceHistoryParts");

            migrationBuilder.DropIndex(
                name: "IX_ServiceHistoryJobs_LaborCatalogItemId",
                table: "ServiceHistoryJobs");

            migrationBuilder.DropColumn(
                name: "PartsCatalogItemId",
                table: "ServiceHistoryParts");

            migrationBuilder.DropColumn(
                name: "LaborCatalogItemId",
                table: "ServiceHistoryJobs");

            migrationBuilder.DropColumn(
                name: "LaborHours",
                table: "ServiceHistoryJobs");

            migrationBuilder.DropColumn(
                name: "LaborRate",
                table: "ServiceHistoryJobs");
        }
    }
}
