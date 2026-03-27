using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PgManagement_WebApi.Migrations
{
    /// <inheritdoc />
    public partial class BookingsFieldRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CheckInDate",
                table: "Bookings",
                newName: "ScheduledCheckInDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ScheduledCheckInDate",
                table: "Bookings",
                newName: "CheckInDate");
        }
    }
}
