using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PgManagement_WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PgId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BranchId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerformedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsReviewed = table.Column<bool>(type: "bit", nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEvents_AspNetUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditEvents_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditEvents_PGs_PgId",
                        column: x => x.PgId,
                        principalTable: "PGs",
                        principalColumn: "PgId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_PerformedByUserId",
                table: "AuditEvents",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_PgId",
                table: "AuditEvents",
                column: "PgId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_ReviewedByUserId",
                table: "AuditEvents",
                column: "ReviewedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");
        }
    }
}
