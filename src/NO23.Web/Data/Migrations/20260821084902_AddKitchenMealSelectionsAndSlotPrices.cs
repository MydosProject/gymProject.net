using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKitchenMealSelectionsAndSlotPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KitchenMealSlotPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MealSlot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DailyPrice = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenMealSlotPrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KitchenSubscriptionMealSelections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KitchenSubscriptionId = table.Column<int>(type: "integer", nullable: false),
                    MealSlot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DailyPriceSnapshot = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CalorieRatioSnapshot = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenSubscriptionMealSelections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitchenSubscriptionMealSelections_KitchenSubscriptions_Kitc~",
                        column: x => x.KitchenSubscriptionId,
                        principalTable: "KitchenSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KitchenMealSlotPrices_MealSlot",
                table: "KitchenMealSlotPrices",
                column: "MealSlot",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitchenSubscriptionMealSelections_KitchenSubscriptionId_Mea~",
                table: "KitchenSubscriptionMealSelections",
                columns: new[] { "KitchenSubscriptionId", "MealSlot" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KitchenMealSlotPrices");

            migrationBuilder.DropTable(
                name: "KitchenSubscriptionMealSelections");
        }
    }
}
