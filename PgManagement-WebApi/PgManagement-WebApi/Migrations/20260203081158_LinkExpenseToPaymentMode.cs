using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PgManagement_WebApi.Migrations
{
    /// <inheritdoc />
    public partial class LinkExpenseToPaymentMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_PaymentModes_PaymentModeCode",
                table: "Expenses");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_PaymentModes_PaymentModeCode",
                table: "Expenses",
                column: "PaymentModeCode",
                principalTable: "PaymentModes",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_PaymentModes_PaymentModeCode",
                table: "Expenses");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_PaymentModes_PaymentModeCode",
                table: "Expenses",
                column: "PaymentModeCode",
                principalTable: "PaymentModes",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
