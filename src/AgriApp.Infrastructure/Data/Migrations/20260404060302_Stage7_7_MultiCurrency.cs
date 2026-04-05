using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Stage7_7_MultiCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrencySymbol",
                table: "centers",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "₹");

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "centers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "India Standard Time");

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_centers_CenterId",
                table: "invoices",
                column: "CenterId",
                principalTable: "centers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_work_orders_centers_CenterId",
                table: "work_orders",
                column: "CenterId",
                principalTable: "centers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoices_centers_CenterId",
                table: "invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_work_orders_centers_CenterId",
                table: "work_orders");

            migrationBuilder.DropColumn(
                name: "CurrencySymbol",
                table: "centers");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "centers");
        }
    }
}
