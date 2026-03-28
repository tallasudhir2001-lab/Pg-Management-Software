using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PgManagement_WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Route = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessPoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleAccessPoints",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    AccessPointId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleAccessPoints", x => new { x.RoleId, x.AccessPointId });
                    table.ForeignKey(
                        name: "FK_RoleAccessPoints_AccessPoints_AccessPointId",
                        column: x => x.AccessPointId,
                        principalTable: "AccessPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleAccessPoints_PgRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "PgRoles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessPoints_Key",
                table: "AccessPoints",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleAccessPoints_AccessPointId",
                table: "RoleAccessPoints",
                column: "AccessPointId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleAccessPoints");

            migrationBuilder.DropTable(
                name: "AccessPoints");
        }
    }
}
