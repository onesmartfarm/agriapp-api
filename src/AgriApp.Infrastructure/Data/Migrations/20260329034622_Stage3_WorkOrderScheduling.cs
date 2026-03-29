using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Stage3_WorkOrderScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_work_orders_StaffId",
                table: "work_orders",
                newName: "IX_WorkOrders_ResponsibleUserId");

            migrationBuilder.RenameIndex(
                name: "IX_work_orders_EquipmentId",
                table: "work_orders",
                newName: "IX_WorkOrders_EquipmentId");

            migrationBuilder.AlterColumn<int>(
                name: "EquipmentId",
                table: "work_orders",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualEndDate",
                table: "work_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualStartDate",
                table: "work_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InquiryId",
                table: "work_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledEndDate",
                table: "work_orders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledStartDate",
                table: "work_orders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "TotalMaterialCost",
                table: "work_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "work_orders",
                type: "text",
                nullable: false,
                defaultValue: "RentalBooking");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_InquiryId",
                table: "work_orders",
                column: "InquiryId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_CenterId_Schedule",
                table: "work_orders",
                columns: new[] { "CenterId", "ScheduledStartDate", "ScheduledEndDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_work_orders_inquiries_InquiryId",
                table: "work_orders",
                column: "InquiryId",
                principalTable: "inquiries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_work_orders_inquiries_InquiryId",
                table: "work_orders");

            migrationBuilder.DropIndex(
                name: "IX_work_orders_InquiryId",
                table: "work_orders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_CenterId_Schedule",
                table: "work_orders");

            migrationBuilder.DropColumn(
                name: "ActualEndDate",
                table: "work_orders");

            migrationBuilder.DropColumn(
                name: "ActualStartDate",
                table: "work_orders");

            migrationBuilder.DropColumn(
                name: "InquiryId",
                table: "work_orders");

            migrationBuilder.DropColumn(
                name: "ScheduledEndDate",
                table: "work_orders");

            migrationBuilder.DropColumn(
                name: "ScheduledStartDate",
                table: "work_orders");

            migrationBuilder.DropColumn(
                name: "TotalMaterialCost",
                table: "work_orders");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "work_orders");

            migrationBuilder.RenameIndex(
                name: "IX_WorkOrders_ResponsibleUserId",
                table: "work_orders",
                newName: "IX_work_orders_StaffId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkOrders_EquipmentId",
                table: "work_orders",
                newName: "IX_work_orders_EquipmentId");

            migrationBuilder.AlterColumn<int>(
                name: "EquipmentId",
                table: "work_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
