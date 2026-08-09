using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKitchenPaymentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceActivityLevel",
                table: "KitchenSubscriptions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceAge",
                table: "KitchenSubscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceGender",
                table: "KitchenSubscriptions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceHeightCm",
                table: "KitchenSubscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SourceWeightKg",
                table: "KitchenSubscriptions",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceActivityLevel",
                table: "KitchenSubscriptions");

            migrationBuilder.DropColumn(
                name: "SourceAge",
                table: "KitchenSubscriptions");

            migrationBuilder.DropColumn(
                name: "SourceGender",
                table: "KitchenSubscriptions");

            migrationBuilder.DropColumn(
                name: "SourceHeightCm",
                table: "KitchenSubscriptions");

            migrationBuilder.DropColumn(
                name: "SourceWeightKg",
                table: "KitchenSubscriptions");
        }
    }
}
