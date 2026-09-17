using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NO23.Web.Data;

#nullable disable

namespace NO23.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260917120000_AddManualServicePackageAssignments")]
public partial class AddManualServicePackageAssignments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "FamilyCode",
            table: "MemberProfiles",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ServicePackageVariantId",
            table: "MemberProfiles",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "SiblingDiscountPercent",
            table: "MemberProfiles",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateIndex(
            name: "IX_MemberProfiles_FamilyCode",
            table: "MemberProfiles",
            column: "FamilyCode");

        migrationBuilder.CreateIndex(
            name: "IX_MemberProfiles_ServicePackageVariantId",
            table: "MemberProfiles",
            column: "ServicePackageVariantId");

        migrationBuilder.AddForeignKey(
            name: "FK_MemberProfiles_ServicePackageVariants_ServicePackageVariantId",
            table: "MemberProfiles",
            column: "ServicePackageVariantId",
            principalTable: "ServicePackageVariants",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.Sql("""
UPDATE "ServicePackageVariants" v
SET "IsActive" = TRUE,
    "UpdatedAtUtc" = NOW()
FROM "ServicePackages" p
WHERE v."ServicePackageId" = p."Id"
  AND p."Slug" = 'kids-club'
  AND v."Name" IN ('12 Ders', '12 Ders · 6 Aylık', '12 Ders · Yıllık', '24 Ders · 3 Aylık');
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_MemberProfiles_ServicePackageVariants_ServicePackageVariantId",
            table: "MemberProfiles");

        migrationBuilder.DropIndex(name: "IX_MemberProfiles_FamilyCode", table: "MemberProfiles");
        migrationBuilder.DropIndex(name: "IX_MemberProfiles_ServicePackageVariantId", table: "MemberProfiles");
        migrationBuilder.DropColumn(name: "FamilyCode", table: "MemberProfiles");
        migrationBuilder.DropColumn(name: "ServicePackageVariantId", table: "MemberProfiles");
        migrationBuilder.DropColumn(name: "SiblingDiscountPercent", table: "MemberProfiles");

        migrationBuilder.Sql("""
UPDATE "ServicePackageVariants" v
SET "IsActive" = FALSE,
    "UpdatedAtUtc" = NOW()
FROM "ServicePackages" p
WHERE v."ServicePackageId" = p."Id"
  AND p."Slug" = 'kids-club'
  AND v."Name" IN ('12 Ders', '12 Ders · 6 Aylık', '12 Ders · Yıllık', '24 Ders · 3 Aylık');
""");
    }
}
