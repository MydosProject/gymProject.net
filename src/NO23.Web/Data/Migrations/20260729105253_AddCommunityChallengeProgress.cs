using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityChallengeProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CalorieTolerancePercent",
                table: "CommunityChallenges",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 10m);

            migrationBuilder.AddColumn<int>(
                name: "RequiredCompletionPercent",
                table: "CommunityChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 80);

            migrationBuilder.AddColumn<int>(
                name: "TargetDailyCalories",
                table: "CommunityChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 2000);

            migrationBuilder.CreateTable(
                name: "CommunityChallengeParticipations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommunityChallengeId = table.Column<int>(type: "integer", nullable: false),
                    MemberProfileId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    JoinedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityChallengeParticipations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityChallengeParticipations_CommunityChallenges_Commun~",
                        column: x => x.CommunityChallengeId,
                        principalTable: "CommunityChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommunityChallengeParticipations_MemberProfiles_MemberProfi~",
                        column: x => x.MemberProfileId,
                        principalTable: "MemberProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeProgressEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommunityChallengeParticipationId = table.Column<int>(type: "integer", nullable: false),
                    EntryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CaloriesConsumed = table.Column<int>(type: "integer", nullable: false),
                    TargetDailyCaloriesSnapshot = table.Column<int>(type: "integer", nullable: false),
                    CalorieTolerancePercentSnapshot = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    MinCaloriesSnapshot = table.Column<int>(type: "integer", nullable: false),
                    MaxCaloriesSnapshot = table.Column<int>(type: "integer", nullable: false),
                    IsCompliant = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeProgressEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeProgressEntries_CommunityChallengeParticipations_C~",
                        column: x => x.CommunityChallengeParticipationId,
                        principalTable: "CommunityChallengeParticipations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeProgressEntries_CommunityChallengeParticipationId_~",
                table: "ChallengeProgressEntries",
                columns: new[] { "CommunityChallengeParticipationId", "EntryDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityChallengeParticipations_CommunityChallengeId_Membe~",
                table: "CommunityChallengeParticipations",
                columns: new[] { "CommunityChallengeId", "MemberProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityChallengeParticipations_MemberProfileId",
                table: "CommunityChallengeParticipations",
                column: "MemberProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChallengeProgressEntries");

            migrationBuilder.DropTable(
                name: "CommunityChallengeParticipations");

            migrationBuilder.DropColumn(
                name: "CalorieTolerancePercent",
                table: "CommunityChallenges");

            migrationBuilder.DropColumn(
                name: "RequiredCompletionPercent",
                table: "CommunityChallenges");

            migrationBuilder.DropColumn(
                name: "TargetDailyCalories",
                table: "CommunityChallenges");
        }
    }
}
