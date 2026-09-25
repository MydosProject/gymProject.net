using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberNationalIdentityNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NationalIdentityNumber",
                table: "MemberProfiles",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberProfiles_NationalIdentityNumber",
                table: "MemberProfiles",
                column: "NationalIdentityNumber",
                unique: true,
                filter: "\"NationalIdentityNumber\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MemberProfiles_NationalIdentityNumber",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "NationalIdentityNumber",
                table: "MemberProfiles");
        }
    }
}
