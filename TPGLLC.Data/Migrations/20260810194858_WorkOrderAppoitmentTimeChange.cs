using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPGLLC.Data.Migrations
{
    /// <inheritdoc />
    public partial class WorkOrderAppoitmentTimeChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdvisorMessage",
                table: "AppointmentRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposedDate",
                table: "AppointmentRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposedTime",
                table: "AppointmentRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseToken",
                table: "AppointmentRequests",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResponseTokenExpiresUtc",
                table: "AppointmentRequests",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_ResponseToken",
                table: "AppointmentRequests",
                column: "ResponseToken",
                unique: true,
                filter: "[ResponseToken] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppointmentRequests_ResponseToken",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "AdvisorMessage",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "ProposedDate",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "ProposedTime",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "ResponseToken",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "ResponseTokenExpiresUtc",
                table: "AppointmentRequests");
        }
    }
}
