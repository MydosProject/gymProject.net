using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NO23.Web.Data.Migrations;

public partial class ActivatePersonalTrainingVariants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The catalog migration deactivated all variants before synchronizing them.
        // Restore the published FLEX, ROUTINE and COMMIT choices for existing databases.
        migrationBuilder.Sql("""
UPDATE "ServicePackageVariants" v SET "IsActive" = TRUE, "UpdatedAtUtc" = NOW()
FROM "ServicePackages" p
WHERE v."ServicePackageId" = p."Id"
  AND p."Slug" = 'pt-flex' AND v."Name" IN ('8 Ders', '12 Ders');
UPDATE "ServicePackageVariants" v SET "IsActive" = TRUE, "UpdatedAtUtc" = NOW()
FROM "ServicePackages" p
WHERE v."ServicePackageId" = p."Id"
  AND p."Slug" = 'pt-routine' AND v."Name" IN ('24 Ders', '36 Ders');
UPDATE "ServicePackageVariants" v SET "IsActive" = TRUE, "UpdatedAtUtc" = NOW()
FROM "ServicePackages" p
WHERE v."ServicePackageId" = p."Id"
  AND p."Slug" = 'pt-commit' AND v."Name" IN ('50 Ders', '70 Ders', '100 Ders');
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Keep published choices active when rolling back the repair migration.
    }
}
