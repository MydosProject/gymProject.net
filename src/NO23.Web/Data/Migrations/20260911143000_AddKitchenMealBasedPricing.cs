using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NO23.Web.Data;

#nullable disable

namespace NO23.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260911143000_AddKitchenMealBasedPricing")]
public partial class AddKitchenMealBasedPricing : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "TwoMainMealsPrice",
            table: "KitchenSubscriptionPackages",
            type: "numeric(10,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "ThreeMainMealsPrice",
            table: "KitchenSubscriptionPackages",
            type: "numeric(10,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "DailyDeliveryFee",
            table: "KitchenSubscriptionPackages",
            type: "numeric(10,2)",
            nullable: false,
            defaultValue: 95m);

        migrationBuilder.AddColumn<decimal>(
            name: "DailyDeliveryFeeSnapshot",
            table: "KitchenSubscriptions",
            type: "numeric(10,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.Sql("""
UPDATE "KitchenSubscriptionPackages"
SET "UnitPrice" = 3000, "TwoMainMealsPrice" = 5500, "ThreeMainMealsPrice" = 7500,
    "DailyDeliveryFee" = 95, "Description" = 'Günlük 1-3 ana öğün ve 1 ara öğün seçimiyle hazırlanan 5 günlük deneme paketi.',
    "UpdatedAtUtc" = NOW()
WHERE "Plan" = 'FiveDays';

UPDATE "KitchenSubscriptionPackages"
SET "UnitPrice" = 10000, "TwoMainMealsPrice" = 19000, "ThreeMainMealsPrice" = 28000,
    "DailyDeliveryFee" = 95, "Description" = 'Günlük 1-3 ana öğün ve 1 ara öğün seçimiyle hazırlanan 20 günlük abonelik paketi.',
    "UpdatedAtUtc" = NOW()
WHERE "Plan" = 'TwentyDays';

UPDATE "KitchenSubscriptionPackages"
SET "TwoMainMealsPrice" = "UnitPrice", "ThreeMainMealsPrice" = "UnitPrice",
    "DailyDeliveryFee" = 95
WHERE "TwoMainMealsPrice" = 0 OR "ThreeMainMealsPrice" = 0;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "TwoMainMealsPrice", table: "KitchenSubscriptionPackages");
        migrationBuilder.DropColumn(name: "ThreeMainMealsPrice", table: "KitchenSubscriptionPackages");
        migrationBuilder.DropColumn(name: "DailyDeliveryFee", table: "KitchenSubscriptionPackages");
        migrationBuilder.DropColumn(name: "DailyDeliveryFeeSnapshot", table: "KitchenSubscriptions");
    }
}
