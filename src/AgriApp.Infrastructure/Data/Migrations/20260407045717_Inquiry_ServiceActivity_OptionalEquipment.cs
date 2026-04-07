using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Inquiry_ServiceActivity_OptionalEquipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inquiries_equipment_EquipmentId",
                table: "inquiries");

            migrationBuilder.AlterColumn<int>(
                name: "EquipmentId",
                table: "inquiries",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "ServiceActivityId",
                table: "inquiries",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_inquiries_ServiceActivityId",
                table: "inquiries",
                column: "ServiceActivityId");

            migrationBuilder.AddForeignKey(
                name: "FK_inquiries_equipment_EquipmentId",
                table: "inquiries",
                column: "EquipmentId",
                principalTable: "equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_inquiries_service_activities_ServiceActivityId",
                table: "inquiries",
                column: "ServiceActivityId",
                principalTable: "service_activities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inquiries_equipment_EquipmentId",
                table: "inquiries");

            migrationBuilder.DropForeignKey(
                name: "FK_inquiries_service_activities_ServiceActivityId",
                table: "inquiries");

            migrationBuilder.DropIndex(
                name: "IX_inquiries_ServiceActivityId",
                table: "inquiries");

            migrationBuilder.DropColumn(
                name: "ServiceActivityId",
                table: "inquiries");

            migrationBuilder.AlterColumn<int>(
                name: "EquipmentId",
                table: "inquiries",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_inquiries_equipment_EquipmentId",
                table: "inquiries",
                column: "EquipmentId",
                principalTable: "equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
