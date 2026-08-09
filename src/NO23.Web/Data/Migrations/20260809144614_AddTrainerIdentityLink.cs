using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainerIdentityLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Trainers",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trainers_ApplicationUserId",
                table: "Trainers",
                column: "ApplicationUserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Trainers_AspNetUsers_ApplicationUserId",
                table: "Trainers",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trainers_AspNetUsers_ApplicationUserId",
                table: "Trainers");

            migrationBuilder.DropIndex(
                name: "IX_Trainers_ApplicationUserId",
                table: "Trainers");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Trainers");
        }
    }
}
