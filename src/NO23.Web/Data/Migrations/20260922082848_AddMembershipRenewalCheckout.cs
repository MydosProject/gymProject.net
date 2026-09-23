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
            migrationBuilder.AddColumn<int>(
                name: "ServicePackageVariantId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastMembershipOrderId",
                table: "MemberProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MembershipEndsAtUtc",
                table: "MemberProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MembershipStartsAtUtc",
                table: "MemberProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ServicePackageVariantId",
                table: "Orders",
                column: "ServicePackageVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ServicePackageVariants_ServicePackageVariantId",
                table: "Orders",
                column: "ServicePackageVariantId",
                principalTable: "ServicePackageVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ServicePackageVariants_ServicePackageVariantId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ServicePackageVariantId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ServicePackageVariantId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "LastMembershipOrderId",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "MembershipEndsAtUtc",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "MembershipStartsAtUtc",
                table: "MemberProfiles");
        }
    }
}
