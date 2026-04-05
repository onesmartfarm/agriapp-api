using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AgriApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Stage8_ServiceActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_work_orders_equipment_EquipmentId",
                table: "work_orders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_EquipmentId",
                table: "work_orders");

            migrationBuilder.RenameColumn(
                name: "EquipmentId",
                table: "work_orders",
                newName: "ImplementId");

            migrationBuilder.AddColumn<bool>(
                name: "IsImplement",
                table: "equipment",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "service_activities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    BaseRatePerHour = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CenterId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_activities_centers_CenterId",
                        column: x => x.CenterId,
                        principalTable: "centers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_service_activities_CenterId",
                table: "service_activities",
                column: "CenterId");

            migrationBuilder.AddColumn<int>(
                name: "ServiceActivityId",
                table: "work_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TractorId",
                table: "work_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_ImplementId",
                table: "work_orders",
                column: "ImplementId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_ServiceActivityId",
                table: "work_orders",
                column: "ServiceActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TractorId",
                table: "work_orders",
                column: "TractorId");

            migrationBuilder.AddForeignKey(
                name: "FK_work_orders_equipment_ImplementId",
                table: "work_orders",
                column: "ImplementId",
                principalTable: "equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_work_orders_equipment_TractorId",
                table: "work_orders",
                column: "TractorId",
                principalTable: "equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_work_orders_service_activities_ServiceActivityId",
                table: "work_orders",
                column: "ServiceActivityId",
                principalTable: "service_activities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_work_orders_equipment_ImplementId",
                table: "work_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_work_orders_equipment_TractorId",
                table: "work_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_work_orders_service_activities_ServiceActivityId",
                table: "work_orders");

            migrationBuilder.DropTable(
                name: "service_activities");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_ImplementId",
                table: "work_orders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_ServiceActivityId",
                table: "work_orders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_TractorId",
                table: "work_orders");

            migrationBuilder.DropColumn(
                name: "ServiceActivityId",
                table: "work_orders");

            migrationBuilder.DropColumn(
                name: "TractorId",
                table: "work_orders");

            migrationBuilder.DropColumn(
                name: "IsImplement",
                table: "equipment");

            migrationBuilder.RenameColumn(
                name: "ImplementId",
                table: "work_orders",
                newName: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_EquipmentId",
                table: "work_orders",
                column: "EquipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_work_orders_equipment_EquipmentId",
                table: "work_orders",
                column: "EquipmentId",
                principalTable: "equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
