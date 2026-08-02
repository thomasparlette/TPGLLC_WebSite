using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPGLLC.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "AppointmentRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "AppointmentRequests",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }
    }
}
