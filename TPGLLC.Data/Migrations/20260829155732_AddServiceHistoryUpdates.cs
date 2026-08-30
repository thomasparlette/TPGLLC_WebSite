using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPGLLC.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceHistoryUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceHistoryUpdates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceHistoryEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AuthorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsCustomerVisible = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceHistoryUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceHistoryUpdates_ServiceHistoryEntries_ServiceHistoryEntryId",
                        column: x => x.ServiceHistoryEntryId,
                        principalTable: "ServiceHistoryEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceHistoryUpdates_ServiceHistoryEntryId_CreatedUtc",
                table: "ServiceHistoryUpdates",
                columns: new[] { "ServiceHistoryEntryId", "CreatedUtc" });

            migrationBuilder.Sql("""
                INSERT INTO [ServiceHistoryUpdates]
                    ([Id], [ServiceHistoryEntryId], [Status], [Message], [AuthorName], [IsCustomerVisible], [CreatedUtc])
                SELECT
                    NEWID(),
                    [Id],
                    [Status],
                    CONCAT('Work order status: ', [Status], '.'),
                    NULL,
                    CAST(1 AS bit),
                    COALESCE([UpdatedUtc], [CreatedUtc])
                FROM [ServiceHistoryEntries]
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceHistoryUpdates");
        }
    }
}
