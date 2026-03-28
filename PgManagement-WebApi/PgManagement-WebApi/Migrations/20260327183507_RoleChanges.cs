using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PgManagement_WebApi.Migrations
{
    /// <inheritdoc />
    public partial class RoleChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoleAccessPoints_PgRoles_RoleId",
                table: "RoleAccessPoints");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPgs_PgRoles_RoleId",
                table: "UserPgs");

            migrationBuilder.DropTable(
                name: "PgRoles");

            migrationBuilder.DropIndex(
                name: "IX_UserPgs_RoleId",
                table: "UserPgs");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "UserPgs");

            // SQL Server cannot alter a column that is part of a PK.
            // Drop the PK first, change the column type, then recreate it.
            migrationBuilder.DropPrimaryKey(
                name: "PK_RoleAccessPoints",
                table: "RoleAccessPoints");

            // Existing rows (if any) cannot be converted int→nvarchar; clear them.
            migrationBuilder.Sql("DELETE FROM [RoleAccessPoints]");

            migrationBuilder.AlterColumn<string>(
                name: "RoleId",
                table: "RoleAccessPoints",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoleAccessPoints",
                table: "RoleAccessPoints",
                columns: new[] { "RoleId", "AccessPointId" });

            migrationBuilder.AddForeignKey(
                name: "FK_RoleAccessPoints_AspNetRoles_RoleId",
                table: "RoleAccessPoints",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoleAccessPoints_AspNetRoles_RoleId",
                table: "RoleAccessPoints");

            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "UserPgs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoleAccessPoints",
                table: "RoleAccessPoints");

            migrationBuilder.Sql("DELETE FROM [RoleAccessPoints]");

            migrationBuilder.AlterColumn<int>(
                name: "RoleId",
                table: "RoleAccessPoints",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoleAccessPoints",
                table: "RoleAccessPoints",
                columns: new[] { "RoleId", "AccessPointId" });

            migrationBuilder.CreateTable(
                name: "PgRoles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PgRoles", x => x.RoleId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPgs_RoleId",
                table: "UserPgs",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_RoleAccessPoints_PgRoles_RoleId",
                table: "RoleAccessPoints",
                column: "RoleId",
                principalTable: "PgRoles",
                principalColumn: "RoleId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserPgs_PgRoles_RoleId",
                table: "UserPgs",
                column: "RoleId",
                principalTable: "PgRoles",
                principalColumn: "RoleId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
