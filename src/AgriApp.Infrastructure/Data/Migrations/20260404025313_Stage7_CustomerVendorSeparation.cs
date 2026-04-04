using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AgriApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Stage7_CustomerVendorSeparation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inquiries_users_CustomerId",
                table: "inquiries");

            migrationBuilder.DropForeignKey(
                name: "FK_invoices_users_CustomerId",
                table: "invoices");

            migrationBuilder.AddColumn<decimal>(
                name: "PurchaseCost",
                table: "equipment",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PurchaseDate",
                table: "equipment",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VendorId",
                table: "equipment",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AdminUserId",
                table: "centers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CenterId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customers_centers_CenterId",
                        column: x => x.CenterId,
                        principalTable: "centers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactPerson = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CenterId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vendors_centers_CenterId",
                        column: x => x.CenterId,
                        principalTable: "centers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Seed a default customer using the first available center (only if centers exist)
            migrationBuilder.Sql(@"
                INSERT INTO customers (""Name"", ""Phone"", ""Email"", ""Address"", ""CenterId"", ""CreatedAt"")
                SELECT 'Default Customer', NULL, NULL, NULL, ""Id"", NOW()
                FROM centers
                ORDER BY ""Id""
                LIMIT 1;
            ");

            // Migrate existing inquiry/invoice rows to reference the new default customer
            migrationBuilder.Sql(@"
                UPDATE inquiries
                SET ""CustomerId"" = (SELECT ""Id"" FROM customers ORDER BY ""Id"" LIMIT 1)
                WHERE ""CustomerId"" IS NOT NULL
                  AND EXISTS (SELECT 1 FROM customers);
            ");

            migrationBuilder.Sql(@"
                UPDATE invoices
                SET ""CustomerId"" = (SELECT ""Id"" FROM customers ORDER BY ""Id"" LIMIT 1)
                WHERE EXISTS (SELECT 1 FROM customers);
            ");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_VendorId",
                table: "equipment",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_centers_AdminUserId",
                table: "centers",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_customers_CenterId",
                table: "customers",
                column: "CenterId");

            migrationBuilder.CreateIndex(
                name: "IX_vendors_CenterId",
                table: "vendors",
                column: "CenterId");

            migrationBuilder.AddForeignKey(
                name: "FK_centers_users_AdminUserId",
                table: "centers",
                column: "AdminUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_equipment_vendors_VendorId",
                table: "equipment",
                column: "VendorId",
                principalTable: "vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_inquiries_customers_CustomerId",
                table: "inquiries",
                column: "CustomerId",
                principalTable: "customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_customers_CustomerId",
                table: "invoices",
                column: "CustomerId",
                principalTable: "customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_centers_users_AdminUserId",
                table: "centers");

            migrationBuilder.DropForeignKey(
                name: "FK_equipment_vendors_VendorId",
                table: "equipment");

            migrationBuilder.DropForeignKey(
                name: "FK_inquiries_customers_CustomerId",
                table: "inquiries");

            migrationBuilder.DropForeignKey(
                name: "FK_invoices_customers_CustomerId",
                table: "invoices");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "vendors");

            migrationBuilder.DropIndex(
                name: "IX_equipment_VendorId",
                table: "equipment");

            migrationBuilder.DropIndex(
                name: "IX_centers_AdminUserId",
                table: "centers");

            migrationBuilder.DropColumn(
                name: "PurchaseCost",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "PurchaseDate",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "AdminUserId",
                table: "centers");

            migrationBuilder.AddForeignKey(
                name: "FK_inquiries_users_CustomerId",
                table: "inquiries",
                column: "CustomerId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_users_CustomerId",
                table: "invoices",
                column: "CustomerId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
