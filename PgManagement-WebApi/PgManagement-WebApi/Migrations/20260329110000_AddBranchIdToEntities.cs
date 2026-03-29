using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PgManagement_WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchIdToEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BranchId",
                table: "Tenants",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BranchId",
                table: "Rooms",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BranchId",
                table: "Payments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BranchId",
                table: "Expenses",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BranchId",
                table: "Advances",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BranchId",
                table: "Bookings",
                type: "nvarchar(450)",
                nullable: true);

            // Backfill BranchId from PG table (via PgId)
            migrationBuilder.Sql(@"
                UPDATE t SET t.BranchId = p.BranchId
                FROM Tenants t JOIN PGs p ON t.PgId = p.PgId
                WHERE p.BranchId IS NOT NULL;

                UPDATE r SET r.BranchId = p.BranchId
                FROM Rooms r JOIN PGs p ON r.PgId = p.PgId
                WHERE p.BranchId IS NOT NULL;

                UPDATE pay SET pay.BranchId = p.BranchId
                FROM Payments pay JOIN PGs p ON pay.PgId = p.PgId
                WHERE p.BranchId IS NOT NULL;

                UPDATE e SET e.BranchId = p.BranchId
                FROM Expenses e JOIN PGs p ON e.PgId = p.PgId
                WHERE p.BranchId IS NOT NULL;

                UPDATE a SET a.BranchId = p.BranchId
                FROM Advances a
                JOIN Tenants t ON a.TenantId = t.TenantId
                JOIN PGs p ON t.PgId = p.PgId
                WHERE p.BranchId IS NOT NULL;

                UPDATE b SET b.BranchId = p.BranchId
                FROM Bookings b JOIN PGs p ON b.PgId = p.PgId
                WHERE p.BranchId IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "BranchId", table: "Tenants");
            migrationBuilder.DropColumn(name: "BranchId", table: "Rooms");
            migrationBuilder.DropColumn(name: "BranchId", table: "Payments");
            migrationBuilder.DropColumn(name: "BranchId", table: "Expenses");
            migrationBuilder.DropColumn(name: "BranchId", table: "Advances");
            migrationBuilder.DropColumn(name: "BranchId", table: "Bookings");
        }
    }
}
