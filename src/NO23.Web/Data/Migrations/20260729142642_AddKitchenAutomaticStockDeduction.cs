using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKitchenAutomaticStockDeduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<System.DateTime>(
                name: "StockDeductedAtUtc",
                table: "KitchenProductionPlans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KitchenProductionPlanId",
                table: "KitchenStockMovements",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitchenStockMovements_KitchenProductionPlanId",
                table: "KitchenStockMovements",
                column: "KitchenProductionPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_KitchenStockMovements_KitchenProductionPlans_PlanId",
                table: "KitchenStockMovements",
                column: "KitchenProductionPlanId",
                principalTable: "KitchenProductionPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KitchenStockMovements_KitchenProductionPlans_PlanId",
                table: "KitchenStockMovements");

            migrationBuilder.DropIndex(
                name: "IX_KitchenStockMovements_KitchenProductionPlanId",
                table: "KitchenStockMovements");

            migrationBuilder.DropColumn(
                name: "KitchenProductionPlanId",
                table: "KitchenStockMovements");

            migrationBuilder.DropColumn(
                name: "StockDeductedAtUtc",
                table: "KitchenProductionPlans");
        }
    }
}
