using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPGLLC.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentAdvisorResponseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppointmentRequests_ResponseToken",
                table: "AppointmentRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_ResponseToken",
                table: "AppointmentRequests",
                column: "ResponseToken",
                unique: true,
                filter: "[ResponseToken] IS NOT NULL");
        }
    }
}
