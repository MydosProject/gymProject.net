using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipPackageServiceOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MembershipPackageOptionId",
                table: "MemberProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MembershipPackageOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MembershipPackageId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DurationDays = table.Column<int>(type: "integer", nullable: false),
                    PersonalTrainingSessionCount = table.Column<int>(type: "integer", nullable: false),
                    GroupClassCreditCount = table.Column<int>(type: "integer", nullable: false),
                    IncludesGymAccess = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipPackageOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MembershipPackageOptions_MembershipPackages_MembershipPacka~",
                        column: x => x.MembershipPackageId,
                        principalTable: "MembershipPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberProfiles_MembershipPackageOptionId",
                table: "MemberProfiles",
                column: "MembershipPackageOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPackageOptions_MembershipPackageId_Name",
                table: "MembershipPackageOptions",
                columns: new[] { "MembershipPackageId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MemberProfiles_MembershipPackageOptions_MembershipPackageOp~",
                table: "MemberProfiles",
                column: "MembershipPackageOptionId",
                principalTable: "MembershipPackageOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberProfiles_MembershipPackageOptions_MembershipPackageOp~",
                table: "MemberProfiles");

            migrationBuilder.DropTable(
                name: "MembershipPackageOptions");

            migrationBuilder.DropIndex(
                name: "IX_MemberProfiles_MembershipPackageOptionId",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "MembershipPackageOptionId",
                table: "MemberProfiles");
        }
    }
}
