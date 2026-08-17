using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPGLLC.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentAdvisorWorkOrderLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentRequestId",
                table: "ServiceHistoryEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceHistoryEntries_AppointmentRequestId",
                table: "ServiceHistoryEntries",
                column: "AppointmentRequestId",
                unique: true,
                filter: "[AppointmentRequestId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceHistoryEntries_AppointmentRequests_AppointmentRequestId",
                table: "ServiceHistoryEntries",
                column: "AppointmentRequestId",
                principalTable: "AppointmentRequests",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceHistoryEntries_AppointmentRequests_AppointmentRequestId",
                table: "ServiceHistoryEntries");

            migrationBuilder.DropIndex(
                name: "IX_ServiceHistoryEntries_AppointmentRequestId",
                table: "ServiceHistoryEntries");

            migrationBuilder.DropColumn(
                name: "AppointmentRequestId",
                table: "ServiceHistoryEntries");
        }
    }
}
