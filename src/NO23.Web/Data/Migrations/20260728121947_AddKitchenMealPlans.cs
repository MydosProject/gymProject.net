using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKitchenMealPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPlanEligible",
                table: "KitchenMenuItems",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "KitchenMealPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KitchenSubscriptionId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CalculationVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SourceHeightCm = table.Column<int>(type: "integer", nullable: false),
                    SourceWeightKg = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    SourceAge = table.Column<int>(type: "integer", nullable: false),
                    SourceGender = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SourceActivityLevel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SourceGoal = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TargetDailyCalories = table.Column<int>(type: "integer", nullable: false),
                    TargetProteinGrams = table.Column<int>(type: "integer", nullable: false),
                    TargetCarbohydrateGrams = table.Column<int>(type: "integer", nullable: false),
                    TargetFatGrams = table.Column<int>(type: "integer", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenMealPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitchenMealPlans_KitchenSubscriptions_KitchenSubscriptionId",
                        column: x => x.KitchenSubscriptionId,
                        principalTable: "KitchenSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KitchenMealPlanDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KitchenMealPlanId = table.Column<int>(type: "integer", nullable: false),
                    DayNumber = table.Column<int>(type: "integer", nullable: false),
                    PlanDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalCalories = table.Column<int>(type: "integer", nullable: false),
                    TotalProteinGrams = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    TotalCarbohydrateGrams = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    TotalFatGrams = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenMealPlanDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitchenMealPlanDays_KitchenMealPlans_KitchenMealPlanId",
                        column: x => x.KitchenMealPlanId,
                        principalTable: "KitchenMealPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KitchenMealPlanItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KitchenMealPlanDayId = table.Column<int>(type: "integer", nullable: false),
                    KitchenMenuItemId = table.Column<int>(type: "integer", nullable: false),
                    MealSlot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    CaloriesSnapshot = table.Column<int>(type: "integer", nullable: false),
                    ProteinGramsSnapshot = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    CarbohydrateGramsSnapshot = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    FatGramsSnapshot = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    UnitPriceSnapshot = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenMealPlanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitchenMealPlanItems_KitchenMealPlanDays_KitchenMealPlanDay~",
                        column: x => x.KitchenMealPlanDayId,
                        principalTable: "KitchenMealPlanDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KitchenMealPlanItems_KitchenMenuItems_KitchenMenuItemId",
                        column: x => x.KitchenMenuItemId,
                        principalTable: "KitchenMenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KitchenMealPlanDays_KitchenMealPlanId_DayNumber",
                table: "KitchenMealPlanDays",
                columns: new[] { "KitchenMealPlanId", "DayNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitchenMealPlanItems_KitchenMealPlanDayId_MealSlot",
                table: "KitchenMealPlanItems",
                columns: new[] { "KitchenMealPlanDayId", "MealSlot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitchenMealPlanItems_KitchenMenuItemId",
                table: "KitchenMealPlanItems",
                column: "KitchenMenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenMealPlans_KitchenSubscriptionId",
                table: "KitchenMealPlans",
                column: "KitchenSubscriptionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KitchenMealPlanItems");

            migrationBuilder.DropTable(
                name: "KitchenMealPlanDays");

            migrationBuilder.DropTable(
                name: "KitchenMealPlans");

            migrationBuilder.DropColumn(
                name: "IsPlanEligible",
                table: "KitchenMenuItems");
        }
    }
}
