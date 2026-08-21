using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationalKitchenAllergens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KitchenAllergens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenAllergens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KitchenMenuItemAllergens",
                columns: table => new
                {
                    KitchenMenuItemId = table.Column<int>(type: "integer", nullable: false),
                    KitchenAllergenId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenMenuItemAllergens", x => new { x.KitchenMenuItemId, x.KitchenAllergenId });
                    table.ForeignKey(
                        name: "FK_KitchenMenuItemAllergens_KitchenAllergens_KitchenAllergenId",
                        column: x => x.KitchenAllergenId,
                        principalTable: "KitchenAllergens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KitchenMenuItemAllergens_KitchenMenuItems_KitchenMenuItemId",
                        column: x => x.KitchenMenuItemId,
                        principalTable: "KitchenMenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemberAllergens",
                columns: table => new
                {
                    MemberProfileId = table.Column<int>(type: "integer", nullable: false),
                    KitchenAllergenId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberAllergens", x => new { x.MemberProfileId, x.KitchenAllergenId });
                    table.ForeignKey(
                        name: "FK_MemberAllergens_KitchenAllergens_KitchenAllergenId",
                        column: x => x.KitchenAllergenId,
                        principalTable: "KitchenAllergens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberAllergens_MemberProfiles_MemberProfileId",
                        column: x => x.MemberProfileId,
                        principalTable: "MemberProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KitchenAllergens_Name",
                table: "KitchenAllergens",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitchenMenuItemAllergens_KitchenAllergenId",
                table: "KitchenMenuItemAllergens",
                column: "KitchenAllergenId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberAllergens_KitchenAllergenId",
                table: "MemberAllergens",
                column: "KitchenAllergenId");

            migrationBuilder.Sql("""
                INSERT INTO "KitchenAllergens" ("Name", "Description", "IsActive", "DisplayOrder") VALUES
                ('Gluten', 'Buğday, arpa, çavdar ve bunların ürünleri.', TRUE, 1),
                ('Süt', 'Süt ve süt ürünleri.', TRUE, 2),
                ('Yumurta', NULL, TRUE, 3),
                ('Yer Fıstığı', NULL, TRUE, 4),
                ('Sert Kabuklu Yemişler', 'Badem, fındık, ceviz ve benzeri yemişler.', TRUE, 5),
                ('Soya', NULL, TRUE, 6),
                ('Balık', NULL, TRUE, 7),
                ('Kabuklu Deniz Ürünleri', NULL, TRUE, 8),
                ('Susam', NULL, TRUE, 9),
                ('Kereviz', NULL, TRUE, 10)
                ON CONFLICT ("Name") DO NOTHING;

                INSERT INTO "KitchenMenuItemAllergens" ("KitchenMenuItemId", "KitchenAllergenId")
                SELECT menu."Id", allergen."Id"
                FROM "KitchenMenuItems" menu
                CROSS JOIN "KitchenAllergens" allergen
                WHERE
                    (allergen."Name" = 'Gluten' AND menu."Allergens" ILIKE '%gluten%') OR
                    (allergen."Name" = 'Süt' AND menu."Allergens" ILIKE '%süt%') OR
                    (allergen."Name" = 'Yumurta' AND menu."Allergens" ILIKE '%yumurta%') OR
                    (allergen."Name" = 'Yer Fıstığı' AND menu."Allergens" ILIKE '%yer fıstığı%') OR
                    (allergen."Name" = 'Sert Kabuklu Yemişler' AND
                        (menu."Allergens" ILIKE '%badem%' OR menu."Allergens" ILIKE '%fındık%' OR menu."Allergens" ILIKE '%ceviz%')) OR
                    (allergen."Name" = 'Soya' AND menu."Allergens" ILIKE '%soya%') OR
                    (allergen."Name" = 'Balık' AND menu."Allergens" ILIKE '%balık%') OR
                    (allergen."Name" = 'Kabuklu Deniz Ürünleri' AND menu."Allergens" ILIKE '%kabuklu deniz%') OR
                    (allergen."Name" = 'Susam' AND menu."Allergens" ILIKE '%susam%') OR
                    (allergen."Name" = 'Kereviz' AND menu."Allergens" ILIKE '%kereviz%')
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.DropColumn(
                name: "Allergens",
                table: "KitchenMenuItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Allergens",
                table: "KitchenMenuItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "KitchenMenuItems" menu
                SET "Allergens" = source."Names"
                FROM (
                    SELECT link."KitchenMenuItemId", string_agg(allergen."Name", ', ' ORDER BY allergen."DisplayOrder") AS "Names"
                    FROM "KitchenMenuItemAllergens" link
                    JOIN "KitchenAllergens" allergen ON allergen."Id" = link."KitchenAllergenId"
                    GROUP BY link."KitchenMenuItemId"
                ) source
                WHERE source."KitchenMenuItemId" = menu."Id";
                """);

            migrationBuilder.DropTable(
                name: "KitchenMenuItemAllergens");

            migrationBuilder.DropTable(
                name: "MemberAllergens");

            migrationBuilder.DropTable(
                name: "KitchenAllergens");

        }
    }
}
