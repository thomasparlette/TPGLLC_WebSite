using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPGLLC.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredEstimatesAndInspections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServiceHistoryJobId",
                table: "ServiceHistoryParts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceHistoryInspections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceHistoryEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Area = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Finding = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsCustomerVisible = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceHistoryInspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceHistoryInspections_ServiceHistoryEntries_ServiceHistoryEntryId",
                        column: x => x.ServiceHistoryEntryId,
                        principalTable: "ServiceHistoryEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceHistoryJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceHistoryEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LaborAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsCustomerDeclined = table.Column<bool>(type: "bit", nullable: false),
                    IsDeferred = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceHistoryJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceHistoryJobs_ServiceHistoryEntries_ServiceHistoryEntryId",
                        column: x => x.ServiceHistoryEntryId,
                        principalTable: "ServiceHistoryEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceHistoryParts_ServiceHistoryJobId",
                table: "ServiceHistoryParts",
                column: "ServiceHistoryJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceHistoryInspections_Condition",
                table: "ServiceHistoryInspections",
                column: "Condition");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceHistoryInspections_ServiceHistoryEntryId_CreatedUtc",
                table: "ServiceHistoryInspections",
                columns: new[] { "ServiceHistoryEntryId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceHistoryJobs_ServiceHistoryEntryId_SortOrder",
                table: "ServiceHistoryJobs",
                columns: new[] { "ServiceHistoryEntryId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceHistoryJobs_Status",
                table: "ServiceHistoryJobs",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceHistoryParts_ServiceHistoryJobs_ServiceHistoryJobId",
                table: "ServiceHistoryParts",
                column: "ServiceHistoryJobId",
                principalTable: "ServiceHistoryJobs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceHistoryParts_ServiceHistoryJobs_ServiceHistoryJobId",
                table: "ServiceHistoryParts");

            migrationBuilder.DropTable(
                name: "ServiceHistoryInspections");

            migrationBuilder.DropTable(
                name: "ServiceHistoryJobs");

            migrationBuilder.DropIndex(
                name: "IX_ServiceHistoryParts_ServiceHistoryJobId",
                table: "ServiceHistoryParts");

            migrationBuilder.DropColumn(
                name: "ServiceHistoryJobId",
                table: "ServiceHistoryParts");
        }
    }
}
