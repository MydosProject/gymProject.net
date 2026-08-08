using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIyzicoCheckoutExpiration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CheckoutExpiresAtUtc",
                table: "PaymentTransactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiredAtUtc",
                table: "PaymentTransactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
            """
            UPDATE "PaymentTransactions"
            SET "CheckoutExpiresAtUtc" =
                "CreatedAtUtc" + INTERVAL '30 minutes'
            WHERE "Provider" = 'iyzico'
            AND "PaymentStatus" = 'Pending'
            AND "CheckoutExpiresAtUtc" IS NULL;
            """);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Provider_PaymentStatus_CheckoutExpiresA~",
                table: "PaymentTransactions",
                columns: new[] { "Provider", "PaymentStatus", "CheckoutExpiresAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_Provider_PaymentStatus_CheckoutExpiresA~",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "CheckoutExpiresAtUtc",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "ExpiredAtUtc",
                table: "PaymentTransactions");
        }
    }
}
