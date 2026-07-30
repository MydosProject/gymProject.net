using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKitchenStockTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KitchenIngredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Unit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CurrentStockQuantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    MinimumStockQuantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenIngredients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KitchenProductionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenProductionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KitchenRecipeIngredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KitchenMenuItemId = table.Column<int>(type: "integer", nullable: false),
                    KitchenIngredientId = table.Column<int>(type: "integer", nullable: false),
                    QuantityPerPortion = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenRecipeIngredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitchenRecipeIngredients_KitchenIngredients_KitchenIngredie~",
                        column: x => x.KitchenIngredientId,
                        principalTable: "KitchenIngredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KitchenRecipeIngredients_KitchenMenuItems_KitchenMenuItemId",
                        column: x => x.KitchenMenuItemId,
                        principalTable: "KitchenMenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KitchenStockMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KitchenIngredientId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    QuantityBeforeSnapshot = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    QuantityAfterSnapshot = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenStockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitchenStockMovements_KitchenIngredients_KitchenIngredientId",
                        column: x => x.KitchenIngredientId,
                        principalTable: "KitchenIngredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KitchenProductionPlanItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KitchenProductionPlanId = table.Column<int>(type: "integer", nullable: false),
                    KitchenMenuItemId = table.Column<int>(type: "integer", nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    SubscriptionPortions = table.Column<int>(type: "integer", nullable: false),
                    OrderPortions = table.Column<int>(type: "integer", nullable: false),
                    TotalPortions = table.Column<int>(type: "integer", nullable: false),
                    HasRecipeSnapshot = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenProductionPlanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitchenProductionPlanItems_KitchenMenuItems_KitchenMenuItem~",
                        column: x => x.KitchenMenuItemId,
                        principalTable: "KitchenMenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KitchenProductionPlanItems_KitchenProductionPlans_KitchenPr~",
                        column: x => x.KitchenProductionPlanId,
                        principalTable: "KitchenProductionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KitchenProductionPlanMaterials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KitchenProductionPlanId = table.Column<int>(type: "integer", nullable: false),
                    KitchenIngredientId = table.Column<int>(type: "integer", nullable: false),
                    IngredientNameSnapshot = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UnitSnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RequiredQuantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    StockQuantitySnapshot = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    MissingQuantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenProductionPlanMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitchenProductionPlanMaterials_KitchenIngredients_KitchenIn~",
                        column: x => x.KitchenIngredientId,
                        principalTable: "KitchenIngredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KitchenProductionPlanMaterials_KitchenProductionPlans_Kitch~",
                        column: x => x.KitchenProductionPlanId,
                        principalTable: "KitchenProductionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KitchenIngredients_Name",
                table: "KitchenIngredients",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitchenProductionPlanItems_KitchenMenuItemId",
                table: "KitchenProductionPlanItems",
                column: "KitchenMenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenProductionPlanItems_KitchenProductionPlanId_KitchenM~",
                table: "KitchenProductionPlanItems",
                columns: new[] { "KitchenProductionPlanId", "KitchenMenuItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitchenProductionPlanMaterials_KitchenIngredientId",
                table: "KitchenProductionPlanMaterials",
                column: "KitchenIngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenProductionPlanMaterials_KitchenProductionPlanId_Kitc~",
                table: "KitchenProductionPlanMaterials",
                columns: new[] { "KitchenProductionPlanId", "KitchenIngredientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitchenProductionPlans_PlanDate",
                table: "KitchenProductionPlans",
                column: "PlanDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitchenRecipeIngredients_KitchenIngredientId",
                table: "KitchenRecipeIngredients",
                column: "KitchenIngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenRecipeIngredients_KitchenMenuItemId_KitchenIngredien~",
                table: "KitchenRecipeIngredients",
                columns: new[] { "KitchenMenuItemId", "KitchenIngredientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitchenStockMovements_KitchenIngredientId",
                table: "KitchenStockMovements",
                column: "KitchenIngredientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KitchenProductionPlanItems");

            migrationBuilder.DropTable(
                name: "KitchenProductionPlanMaterials");

            migrationBuilder.DropTable(
                name: "KitchenRecipeIngredients");

            migrationBuilder.DropTable(
                name: "KitchenStockMovements");

            migrationBuilder.DropTable(
                name: "KitchenProductionPlans");

            migrationBuilder.DropTable(
                name: "KitchenIngredients");
        }
    }
}
