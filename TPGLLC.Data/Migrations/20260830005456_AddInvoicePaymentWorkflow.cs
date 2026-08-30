using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPGLLC.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePaymentWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InvoiceDueUtc",
                table: "ServiceHistoryEntries",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InvoiceIssuedUtc",
                table: "ServiceHistoryEntries",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNotes",
                table: "ServiceHistoryEntries",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceStatus",
                table: "ServiceHistoryEntries",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.CreateTable(
                name: "ServiceHistoryPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceHistoryEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReceivedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ReceivedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceHistoryPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceHistoryPayments_ServiceHistoryEntries_ServiceHistoryEntryId",
                        column: x => x.ServiceHistoryEntryId,
                        principalTable: "ServiceHistoryEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceHistoryPayments_PaymentMethod",
                table: "ServiceHistoryPayments",
                column: "PaymentMethod");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceHistoryPayments_ServiceHistoryEntryId_ReceivedUtc",
                table: "ServiceHistoryPayments",
                columns: new[] { "ServiceHistoryEntryId", "ReceivedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceHistoryPayments");

            migrationBuilder.DropColumn(
                name: "InvoiceDueUtc",
                table: "ServiceHistoryEntries");

            migrationBuilder.DropColumn(
                name: "InvoiceIssuedUtc",
                table: "ServiceHistoryEntries");

            migrationBuilder.DropColumn(
                name: "InvoiceNotes",
                table: "ServiceHistoryEntries");

            migrationBuilder.DropColumn(
                name: "InvoiceStatus",
                table: "ServiceHistoryEntries");
        }
    }
}
