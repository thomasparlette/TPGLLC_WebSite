using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPGLLC.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceHistoryParts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceHistoryEntries_AppointmentRequests_AppointmentRequestId",
                table: "ServiceHistoryEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceHistoryEntries_CustomerVehicles_CustomerVehicleId",
                table: "ServiceHistoryEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceHistoryEntries_Customers_CustomerId",
                table: "ServiceHistoryEntries");

            migrationBuilder.DropIndex(
                name: "IX_ServiceHistoryEntries_AppointmentRequestId",
                table: "ServiceHistoryEntries");

            migrationBuilder.DropIndex(
                name: "IX_ServiceHistoryEntries_CustomerId_ServiceDate",
                table: "ServiceHistoryEntries");

            migrationBuilder.DropIndex(
                name: "IX_ServiceHistoryEntries_ServiceDate",
                table: "ServiceHistoryEntries");

            migrationBuilder.AlterColumn<string>(
                name: "WorkOrderNumber",
                table: "ServiceHistoryEntries",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "VehicleName",
                table: "ServiceHistoryEntries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Technician",
                table: "ServiceHistoryEntries",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ServiceHistoryEntries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Service",
                table: "ServiceHistoryEntries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "ServiceHistoryEntries",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "ServiceHistoryEntries",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InternalNotes",
                table: "ServiceHistoryEntries",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Diagnosis",
                table: "ServiceHistoryEntries",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedUtc",
                table: "ServiceHistoryEntries",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<string>(
                name: "Complaint",
                table: "ServiceHistoryEntries",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApprovalStatus",
                table: "ServiceHistoryEntries",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceHistoryParts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceHistoryEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsApplied = table.Column<bool>(type: "bit", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceHistoryParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceHistoryParts_ServiceHistoryEntries_ServiceHistoryEntryId",
                        column: x => x.ServiceHistoryEntryId,
                        principalTable: "ServiceHistoryEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceHistoryParts_ServiceHistoryEntryId",
                table: "ServiceHistoryParts",
                column: "ServiceHistoryEntryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceHistoryEntries_CustomerVehicles_CustomerVehicleId",
                table: "ServiceHistoryEntries",
                column: "CustomerVehicleId",
                principalTable: "CustomerVehicles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceHistoryEntries_Customers_CustomerId",
                table: "ServiceHistoryEntries",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceHistoryEntries_CustomerVehicles_CustomerVehicleId",
                table: "ServiceHistoryEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceHistoryEntries_Customers_CustomerId",
                table: "ServiceHistoryEntries");

            migrationBuilder.DropTable(
                name: "ServiceHistoryParts");

            migrationBuilder.AlterColumn<string>(
                name: "WorkOrderNumber",
                table: "ServiceHistoryEntries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "VehicleName",
                table: "ServiceHistoryEntries",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Technician",
                table: "ServiceHistoryEntries",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ServiceHistoryEntries",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Service",
                table: "ServiceHistoryEntries",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "ServiceHistoryEntries",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "ServiceHistoryEntries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InternalNotes",
                table: "ServiceHistoryEntries",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Diagnosis",
                table: "ServiceHistoryEntries",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedUtc",
                table: "ServiceHistoryEntries",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<string>(
                name: "Complaint",
                table: "ServiceHistoryEntries",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApprovalStatus",
                table: "ServiceHistoryEntries",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceHistoryEntries_AppointmentRequestId",
                table: "ServiceHistoryEntries",
                column: "AppointmentRequestId",
                unique: true,
                filter: "[AppointmentRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceHistoryEntries_CustomerId_ServiceDate",
                table: "ServiceHistoryEntries",
                columns: new[] { "CustomerId", "ServiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceHistoryEntries_ServiceDate",
                table: "ServiceHistoryEntries",
                column: "ServiceDate");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceHistoryEntries_AppointmentRequests_AppointmentRequestId",
                table: "ServiceHistoryEntries",
                column: "AppointmentRequestId",
                principalTable: "AppointmentRequests",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceHistoryEntries_CustomerVehicles_CustomerVehicleId",
                table: "ServiceHistoryEntries",
                column: "CustomerVehicleId",
                principalTable: "CustomerVehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceHistoryEntries_Customers_CustomerId",
                table: "ServiceHistoryEntries",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");
        }
    }
}
