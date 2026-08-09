using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillTrainerConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO "TrainerConversations"
                    ("MemberProfileId",
                    "TrainerId",
                    "CreatedAtUtc")
                SELECT
                    "MemberProfileId",
                    "TrainerId",
                    MIN("CreatedAtUtc")
                FROM "PersonalTrainingRequests"
                GROUP BY
                    "MemberProfileId",
                    "TrainerId"
                ON CONFLICT
                    ("MemberProfileId", "TrainerId")
                DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
