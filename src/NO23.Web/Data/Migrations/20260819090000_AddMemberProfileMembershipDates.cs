using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NO23.Web.Data;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260819090000_AddMemberProfileMembershipDates")]
    public partial class AddMemberProfileMembershipDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "MembershipStartsAtUtc",
                table: "MemberProfiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<DateTime>(
                name: "MembershipEndsAtUtc",
                table: "MemberProfiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() + INTERVAL '28 days'");

            migrationBuilder.Sql(
                """
                UPDATE "MemberProfiles"
                SET
                    "MembershipStartsAtUtc" = "CreatedAtUtc",
                    "MembershipEndsAtUtc" = "CreatedAtUtc" + INTERVAL '28 days'
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MembershipEndsAtUtc",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "MembershipStartsAtUtc",
                table: "MemberProfiles");
        }
    }
}
