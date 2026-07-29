using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKitchenSubscriptionPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KitchenSubscriptionPackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Plan = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    Days = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenSubscriptionPackages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KitchenSubscriptionPackages_Plan",
                table: "KitchenSubscriptionPackages",
                column: "Plan",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO "KitchenSubscriptionPackages"
                    ("Id", "Plan", "Name", "Description", "Days", "UnitPrice", "IsActive", "DisplayOrder", "CreatedAtUtc")
                VALUES
                    (1, 'FiveDays', '5 Günlük Kitchen Paketi', 'Kalori ve makro hedeflerine göre hazırlanan 5 günlük NO23 Kitchen yemek paketi.', 5, 4250, TRUE, 10, NOW()),
                    (2, 'TenDays', '10 Günlük Kitchen Paketi', 'Düzenli beslenme ritmini kurmak için 10 günlük NO23 Kitchen yemek paketi.', 10, 7900, TRUE, 20, NOW()),
                    (3, 'TwentyDays', '20 Günlük Kitchen Paketi', 'Uzun süreli hedef takibi için 20 günlük NO23 Kitchen yemek paketi.', 20, 14500, TRUE, 30, NOW()),
                    (4, 'Monthly', 'Aylık Kitchen Paketi', 'Aylık rutin oluşturmak isteyen üyeler için 30 günlük NO23 Kitchen yemek paketi.', 30, 19900, TRUE, 40, NOW())
                ON CONFLICT ("Plan") DO NOTHING;

                SELECT setval(
                    pg_get_serial_sequence('"KitchenSubscriptionPackages"', 'Id'),
                    (SELECT GREATEST(COALESCE(MAX("Id"), 1), 1) FROM "KitchenSubscriptionPackages"));
                """);

            migrationBuilder.AddColumn<int>(
                name: "KitchenSubscriptionPackageId",
                table: "KitchenSubscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PackageDaysSnapshot",
                table: "KitchenSubscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackageNameSnapshot",
                table: "KitchenSubscriptions",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PackagePriceSnapshot",
                table: "KitchenSubscriptions",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "KitchenSubscriptions" AS subscription
                SET
                    "KitchenSubscriptionPackageId" = package."Id",
                    "PackageNameSnapshot" = package."Name",
                    "PackagePriceSnapshot" = package."UnitPrice",
                    "PackageDaysSnapshot" = package."Days"
                FROM "KitchenSubscriptionPackages" AS package
                WHERE subscription."Plan" = package."Plan";

                UPDATE "KitchenSubscriptions" AS subscription
                SET
                    "KitchenSubscriptionPackageId" = package."Id",
                    "PackageNameSnapshot" = package."Name",
                    "PackagePriceSnapshot" = package."UnitPrice",
                    "PackageDaysSnapshot" = package."Days"
                FROM "KitchenSubscriptionPackages" AS package
                WHERE
                    subscription."KitchenSubscriptionPackageId" IS NULL AND
                    package."Plan" = 'FiveDays';
                """);

            migrationBuilder.AlterColumn<int>(
                name: "KitchenSubscriptionPackageId",
                table: "KitchenSubscriptions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PackageDaysSnapshot",
                table: "KitchenSubscriptions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PackageNameSnapshot",
                table: "KitchenSubscriptions",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PackagePriceSnapshot",
                table: "KitchenSubscriptions",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitchenSubscriptions_KitchenSubscriptionPackageId",
                table: "KitchenSubscriptions",
                column: "KitchenSubscriptionPackageId");

            migrationBuilder.AddForeignKey(
                name: "FK_KitchenSubscriptions_KitchenSubscriptionPackages_KitchenSub~",
                table: "KitchenSubscriptions",
                column: "KitchenSubscriptionPackageId",
                principalTable: "KitchenSubscriptionPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KitchenSubscriptions_KitchenSubscriptionPackages_KitchenSub~",
                table: "KitchenSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_KitchenSubscriptions_KitchenSubscriptionPackageId",
                table: "KitchenSubscriptions");

            migrationBuilder.DropTable(
                name: "KitchenSubscriptionPackages");

            migrationBuilder.DropColumn(
                name: "KitchenSubscriptionPackageId",
                table: "KitchenSubscriptions");

            migrationBuilder.DropColumn(
                name: "PackageDaysSnapshot",
                table: "KitchenSubscriptions");

            migrationBuilder.DropColumn(
                name: "PackageNameSnapshot",
                table: "KitchenSubscriptions");

            migrationBuilder.DropColumn(
                name: "PackagePriceSnapshot",
                table: "KitchenSubscriptions");
        }
    }
}
