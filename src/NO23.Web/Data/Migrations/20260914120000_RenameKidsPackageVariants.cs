using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NO23.Web.Data;

#nullable disable

namespace NO23.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260914120000_RenameKidsPackageVariants")]
public partial class RenameKidsPackageVariants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
UPDATE "ServicePackageVariants" v
SET "IsActive" = FALSE,
    "UpdatedAtUtc" = NOW()
FROM "ServicePackages" p
WHERE v."ServicePackageId" = p."Id"
  AND p."Slug" = 'kids-club'
  AND v."Name" NOT IN ('8 Ders', '8 Ders · 6 Aylık', '8 Ders · Yıllık');

UPDATE "ServicePackageVariants" v
SET "Name" = CASE v."Name"
        WHEN '8 Ders' THEN 'Aylık'
        WHEN '8 Ders · 6 Aylık' THEN '6 Aylık'
        WHEN '8 Ders · Yıllık' THEN 'Yıllık'
    END,
    "IsActive" = TRUE,
    "UpdatedAtUtc" = NOW()
FROM "ServicePackages" p
WHERE v."ServicePackageId" = p."Id"
  AND p."Slug" = 'kids-club'
  AND v."Name" IN ('8 Ders', '8 Ders · 6 Aylık', '8 Ders · Yıllık');
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
UPDATE "ServicePackageVariants" v
SET "Name" = CASE v."Name"
        WHEN 'Aylık' THEN '8 Ders'
        WHEN '6 Aylık' THEN '8 Ders · 6 Aylık'
        WHEN 'Yıllık' THEN '8 Ders · Yıllık'
    END,
    "UpdatedAtUtc" = NOW()
FROM "ServicePackages" p
WHERE v."ServicePackageId" = p."Id"
  AND p."Slug" = 'kids-club'
  AND v."Name" IN ('Aylık', '6 Aylık', 'Yıllık');
""");
    }
}
