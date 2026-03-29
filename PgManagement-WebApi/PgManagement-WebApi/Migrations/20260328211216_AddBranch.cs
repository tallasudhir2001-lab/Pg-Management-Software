using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PgManagement_WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.AddColumn<string>(
                name: "BranchId",
                table: "PGs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PGs_BranchId",
                table: "PGs",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_PGs_Branches_BranchId",
                table: "PGs",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PGs_Branches_BranchId",
                table: "PGs");

            migrationBuilder.DropIndex(
                name: "IX_PGs_BranchId",
                table: "PGs");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "PGs");

            migrationBuilder.DropTable(
                name: "Branches");
        }
    }
}
