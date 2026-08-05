using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalTrainingRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonalTrainingRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MemberProfileId = table.Column<int>(type: "integer", nullable: false),
                    TrainerId = table.Column<int>(type: "integer", nullable: false),
                    PreferredDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PreferredTimeWindow = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    GoalNote = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdminNote = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalTrainingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalTrainingRequests_MemberProfiles_MemberProfileId",
                        column: x => x.MemberProfileId,
                        principalTable: "MemberProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonalTrainingRequests_Trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "Trainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainingRequests_CreatedAtUtc",
                table: "PersonalTrainingRequests",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainingRequests_MemberProfileId_TrainerId_Preferre~",
                table: "PersonalTrainingRequests",
                columns: new[] { "MemberProfileId", "TrainerId", "PreferredDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainingRequests_Status",
                table: "PersonalTrainingRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainingRequests_TrainerId",
                table: "PersonalTrainingRequests",
                column: "TrainerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonalTrainingRequests");
        }
    }
}
