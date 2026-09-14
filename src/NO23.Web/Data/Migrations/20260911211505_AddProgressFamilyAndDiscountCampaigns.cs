using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProgressFamilyAndDiscountCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FamilyCode",
                table: "ServicePackageApplications",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SiblingDiscountPercent",
                table: "ServicePackageApplications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CampaignDiscountPercent",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Orders",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DiscountCampaignId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscountCode",
                table: "Orders",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyWaterIntakeLiters",
                table: "MemberProgressEntries",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "SourceWeightKg",
                table: "KitchenMealPlans",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(6,2)",
                oldPrecision: 6,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "SourceHeightCm",
                table: "KitchenMealPlans",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "SourceGender",
                table: "KitchenMealPlans",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<int>(
                name: "SourceAge",
                table: "KitchenMealPlans",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "SourceActivityLevel",
                table: "KitchenMealPlans",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.CreateTable(
                name: "DiscountCampaigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DiscountPercent = table.Column<int>(type: "integer", nullable: false),
                    MinimumSubtotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsageLimit = table.Column<int>(type: "integer", nullable: true),
                    UsedCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountCampaigns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePackageApplications_FamilyCode",
                table: "ServicePackageApplications",
                column: "FamilyCode");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DiscountCampaignId",
                table: "Orders",
                column: "DiscountCampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountCampaigns_Code",
                table: "DiscountCampaigns",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscountCampaigns_IsActive",
                table: "DiscountCampaigns",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountCampaigns_StartsAtUtc_EndsAtUtc",
                table: "DiscountCampaigns",
                columns: new[] { "StartsAtUtc", "EndsAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_DiscountCampaigns_DiscountCampaignId",
                table: "Orders",
                column: "DiscountCampaignId",
                principalTable: "DiscountCampaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("""
UPDATE "ServicePackages"
SET "Description" = '6–14 yaş arası çocuklar için postür analizi dahil, ayda 8 derslik aylık, 6 aylık ve yıllık paketler.',
    "UpdatedAtUtc" = NOW()
WHERE "Slug" = 'kids-club';

UPDATE "ServicePackageVariants" v
SET "IsActive" = FALSE,
    "UpdatedAtUtc" = NOW()
FROM "ServicePackages" p
WHERE v."ServicePackageId" = p."Id"
  AND p."Slug" = 'kids-club'
  AND v."KidsClassCreditCount" <> 8;
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_DiscountCampaigns_DiscountCampaignId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "DiscountCampaigns");

            migrationBuilder.DropIndex(
                name: "IX_ServicePackageApplications_FamilyCode",
                table: "ServicePackageApplications");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DiscountCampaignId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "FamilyCode",
                table: "ServicePackageApplications");

            migrationBuilder.DropColumn(
                name: "SiblingDiscountPercent",
                table: "ServicePackageApplications");

            migrationBuilder.DropColumn(
                name: "CampaignDiscountPercent",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountCampaignId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DailyWaterIntakeLiters",
                table: "MemberProgressEntries");

            migrationBuilder.AlterColumn<decimal>(
                name: "SourceWeightKg",
                table: "KitchenMealPlans",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(6,2)",
                oldPrecision: 6,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SourceHeightCm",
                table: "KitchenMealPlans",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceGender",
                table: "KitchenMealPlans",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SourceAge",
                table: "KitchenMealPlans",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceActivityLevel",
                table: "KitchenMealPlans",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);
        }
    }
}
