using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RepairMemberProfileMembershipDateNullability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "MemberProfiles"
                    ALTER COLUMN "MembershipStartsAtUtc" DROP NOT NULL,
                    ALTER COLUMN "MembershipEndsAtUtc" DROP NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "MemberProfiles"
                SET "MembershipStartsAtUtc" = COALESCE(
                        "MembershipStartsAtUtc",
                        "CreatedAtUtc",
                        NOW()),
                    "MembershipEndsAtUtc" = COALESCE(
                        "MembershipEndsAtUtc",
                        "MembershipStartsAtUtc",
                        "CreatedAtUtc",
                        NOW());

                ALTER TABLE "MemberProfiles"
                    ALTER COLUMN "MembershipStartsAtUtc" SET NOT NULL,
                    ALTER COLUMN "MembershipEndsAtUtc" SET NOT NULL;
                """);
        }
    }
}
