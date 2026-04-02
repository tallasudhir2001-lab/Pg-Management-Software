using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PgManagement_WebApi.Migrations
{
    /// <inheritdoc />
    public partial class TenantStayType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix existing rows that have empty string StayType — set them to MONTHLY
            migrationBuilder.Sql("UPDATE TenantRooms SET StayType = 'MONTHLY' WHERE StayType IS NULL OR StayType = ''");

            // Update the column default so future rows get MONTHLY if not specified
            migrationBuilder.AlterColumn<string>(
                name: "StayType",
                table: "TenantRooms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "MONTHLY");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "StayType",
                table: "TenantRooms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
