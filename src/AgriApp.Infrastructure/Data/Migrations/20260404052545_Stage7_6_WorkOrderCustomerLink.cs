using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Stage7_6_WorkOrderCustomerLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkOrderTimeLogs_EndTimeAfterStartTime",
                table: "work_order_time_logs");

            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "work_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_CustomerId",
                table: "work_orders",
                column: "CustomerId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkOrderTimeLogs_EndTimeAfterStartTime",
                table: "work_order_time_logs",
                sql: "\"EndTime\" > \"StartTime\"");

            migrationBuilder.AddForeignKey(
                name: "FK_work_orders_customers_CustomerId",
                table: "work_orders",
                column: "CustomerId",
                principalTable: "customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_work_orders_customers_CustomerId",
                table: "work_orders");

            migrationBuilder.DropIndex(
                name: "IX_work_orders_CustomerId",
                table: "work_orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkOrderTimeLogs_EndTimeAfterStartTime",
                table: "work_order_time_logs");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "work_orders");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkOrderTimeLogs_EndTimeAfterStartTime",
                table: "work_order_time_logs",
                sql: "\"EndTime\" > \"StartTime\"");
        }
    }
}
