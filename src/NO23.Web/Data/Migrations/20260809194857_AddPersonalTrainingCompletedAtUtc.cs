using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalTrainingCompletedAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                table: "PersonalTrainingRequests",
                type: "timestamp with time zone",
                nullable: true);
            
            migrationBuilder.Sql(
                """
                UPDATE "PersonalTrainingRequests"
                SET "CompletedAtUtc" =
                    COALESCE(
                        "UpdatedAtUtc",
                        "ScheduledAtUtc",
                        "CreatedAtUtc"
                    )
                WHERE "Status" = 'Completed'
                AND "CompletedAtUtc" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "PersonalTrainingRequests");
        }
    }
}
