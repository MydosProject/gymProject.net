using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKitchenMealDeliveryPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddressLine",
                table: "KitchenMealPlanDays",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryCity",
                table: "KitchenMealPlanDays",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryDistrict",
                table: "KitchenMealPlanDays",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryFullName",
                table: "KitchenMealPlanDays",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryMethod",
                table: "KitchenMealPlanDays",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryPhoneNumber",
                table: "KitchenMealPlanDays",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryPostalCode",
                table: "KitchenMealPlanDays",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryPreferenceUpdatedAtUtc",
                table: "KitchenMealPlanDays",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryAddressLine",
                table: "KitchenMealPlanDays");

            migrationBuilder.DropColumn(
                name: "DeliveryCity",
                table: "KitchenMealPlanDays");

            migrationBuilder.DropColumn(
                name: "DeliveryDistrict",
                table: "KitchenMealPlanDays");

            migrationBuilder.DropColumn(
                name: "DeliveryFullName",
                table: "KitchenMealPlanDays");

            migrationBuilder.DropColumn(
                name: "DeliveryMethod",
                table: "KitchenMealPlanDays");

            migrationBuilder.DropColumn(
                name: "DeliveryPhoneNumber",
                table: "KitchenMealPlanDays");

            migrationBuilder.DropColumn(
                name: "DeliveryPostalCode",
                table: "KitchenMealPlanDays");

            migrationBuilder.DropColumn(
                name: "DeliveryPreferenceUpdatedAtUtc",
                table: "KitchenMealPlanDays");
        }
    }
}
