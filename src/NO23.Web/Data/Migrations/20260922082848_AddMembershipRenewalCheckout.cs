using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipRenewalCheckout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Some deployed databases already contain one or more of these columns,
            // but do not have this migration in __EFMigrationsHistory. Keep the
            // migration repeatable so startup can reconcile those databases safely.
            migrationBuilder.Sql(
                """
                ALTER TABLE "Orders"
                    ADD COLUMN IF NOT EXISTS "ServicePackageVariantId" integer;

                ALTER TABLE "MemberProfiles"
                    ADD COLUMN IF NOT EXISTS "LastMembershipOrderId" integer,
                    ADD COLUMN IF NOT EXISTS "MembershipEndsAtUtc" timestamp with time zone,
                    ADD COLUMN IF NOT EXISTS "MembershipStartsAtUtc" timestamp with time zone;

                CREATE INDEX IF NOT EXISTS "IX_Orders_ServicePackageVariantId"
                    ON "Orders" ("ServicePackageVariantId");

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_Orders_ServicePackageVariants_ServicePackageVariantId'
                          AND conrelid = '"Orders"'::regclass
                    ) THEN
                        ALTER TABLE "Orders"
                            ADD CONSTRAINT "FK_Orders_ServicePackageVariants_ServicePackageVariantId"
                            FOREIGN KEY ("ServicePackageVariantId")
                            REFERENCES "ServicePackageVariants" ("Id")
                            ON DELETE RESTRICT;
                    END IF;
                END
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Orders"
                    DROP CONSTRAINT IF EXISTS "FK_Orders_ServicePackageVariants_ServicePackageVariantId";

                DROP INDEX IF EXISTS "IX_Orders_ServicePackageVariantId";

                ALTER TABLE "Orders"
                    DROP COLUMN IF EXISTS "ServicePackageVariantId";

                ALTER TABLE "MemberProfiles"
                    DROP COLUMN IF EXISTS "LastMembershipOrderId",
                    DROP COLUMN IF EXISTS "MembershipEndsAtUtc",
                    DROP COLUMN IF EXISTS "MembershipStartsAtUtc";
                """);
        }
    }
}
