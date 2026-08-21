using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedTrainerCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedTrainerId",
                table: "MemberProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PersonalTrainingSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainerId = table.Column<int>(type: "integer", nullable: false),
                    MemberProfileId = table.Column<int>(type: "integer", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreditConsumed = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalTrainingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalTrainingSessions_MemberProfiles_MemberProfileId",
                        column: x => x.MemberProfileId,
                        principalTable: "MemberProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonalTrainingSessions_Trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "Trainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PersonalTrainingSessionHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonalTrainingSessionId = table.Column<int>(type: "integer", nullable: false),
                    PreviousStatus = table.Column<int>(type: "integer", nullable: false),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    PreviousStartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NewStartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    ChangedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalTrainingSessionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalTrainingSessionHistories_PersonalTrainingSessions_P~",
                        column: x => x.PersonalTrainingSessionId,
                        principalTable: "PersonalTrainingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberProfiles_AssignedTrainerId",
                table: "MemberProfiles",
                column: "AssignedTrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainingSessionHistories_PersonalTrainingSessionId",
                table: "PersonalTrainingSessionHistories",
                column: "PersonalTrainingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainingSessions_MemberProfileId",
                table: "PersonalTrainingSessions",
                column: "MemberProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainingSessions_TrainerId_StartsAtUtc",
                table: "PersonalTrainingSessions",
                columns: new[] { "TrainerId", "StartsAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_MemberProfiles_Trainers_AssignedTrainerId",
                table: "MemberProfiles",
                column: "AssignedTrainerId",
                principalTable: "Trainers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberProfiles_Trainers_AssignedTrainerId",
                table: "MemberProfiles");

            migrationBuilder.DropTable(
                name: "PersonalTrainingSessionHistories");

            migrationBuilder.DropTable(
                name: "PersonalTrainingSessions");

            migrationBuilder.DropIndex(
                name: "IX_MemberProfiles_AssignedTrainerId",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "AssignedTrainerId",
                table: "MemberProfiles");
        }
    }
}
