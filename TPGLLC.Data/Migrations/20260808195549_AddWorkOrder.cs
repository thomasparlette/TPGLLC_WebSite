using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPGLLC.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "ServiceHistoryEntries",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Complaint",
                table: "ServiceHistoryEntries",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Diagnosis",
                table: "ServiceHistoryEntries",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimateAmount",
                table: "ServiceHistoryEntries",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternalNotes",
                table: "ServiceHistoryEntries",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InvoiceAmount",
                table: "ServiceHistoryEntries",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "ServiceHistoryEntries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkOrderNumber",
                table: "ServiceHistoryEntries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "ServiceHistoryEntries");

            migrationBuilder.DropColumn(
                name: "Complaint",
                table: "ServiceHistoryEntries");

            migrationBuilder.DropColumn(
                name: "Diagnosis",
                table: "ServiceHistoryEntries");

            migrationBuilder.DropColumn(
                name: "EstimateAmount",
                table: "ServiceHistoryEntries");

            migrationBuilder.DropColumn(
                name: "InternalNotes",
                table: "ServiceHistoryEntries");

            migrationBuilder.DropColumn(
                name: "InvoiceAmount",
                table: "ServiceHistoryEntries");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "ServiceHistoryEntries");

            migrationBuilder.DropColumn(
                name: "WorkOrderNumber",
                table: "ServiceHistoryEntries");
        }
    }
}
