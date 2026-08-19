using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NO23.Web.Data;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260819093000_AddMemberMembershipLifecycle")]
    public partial class AddMemberMembershipLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MembershipStatus",
                table: "MemberProfiles",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<DateTime>(
                name: "MembershipCancellationRequestedAtUtc",
                table: "MemberProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MembershipCancellationEffectiveAtUtc",
                table: "MemberProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MembershipCancellationReason",
                table: "MemberProfiles",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IyzicoCustomerReferenceCode",
                table: "MemberProfiles",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IyzicoSubscriptionReferenceCode",
                table: "MemberProfiles",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IyzicoPricingPlanReferenceCode",
                table: "MemberProfiles",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "MemberProfiles"
                SET "MembershipStatus" =
                    CASE
                        WHEN "MembershipEndsAtUtc" <= NOW() THEN 'Expired'
                        ELSE 'Active'
                    END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IyzicoPricingPlanReferenceCode",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "IyzicoSubscriptionReferenceCode",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "IyzicoCustomerReferenceCode",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "MembershipCancellationReason",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "MembershipCancellationEffectiveAtUtc",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "MembershipCancellationRequestedAtUtc",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "MembershipStatus",
                table: "MemberProfiles");
        }
    }
}
