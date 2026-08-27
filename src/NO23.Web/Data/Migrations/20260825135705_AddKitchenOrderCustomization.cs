using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKitchenOrderCustomization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddedIngredientNames",
                table: "OrderItems",
                type: "character varying(3000)",
                maxLength: 3000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RemovedIngredientNames",
                table: "OrderItems",
                type: "character varying(3000)",
                maxLength: 3000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddedIngredientNames",
                table: "CartItems",
                type: "character varying(3000)",
                maxLength: 3000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RemovedIngredientNames",
                table: "CartItems",
                type: "character varying(3000)",
                maxLength: 3000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddedIngredientNames",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "RemovedIngredientNames",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "AddedIngredientNames",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "RemovedIngredientNames",
                table: "CartItems");
        }
    }
}
