using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarRecurrenceIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RecurrenceId",
                table: "PersonalTrainingSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RecurrenceId",
                table: "ClassSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainingSessions_RecurrenceId",
                table: "PersonalTrainingSessions",
                column: "RecurrenceId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessions_RecurrenceId",
                table: "ClassSessions",
                column: "RecurrenceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PersonalTrainingSessions_RecurrenceId",
                table: "PersonalTrainingSessions");

            migrationBuilder.DropIndex(
                name: "IX_ClassSessions_RecurrenceId",
                table: "ClassSessions");

            migrationBuilder.DropColumn(
                name: "RecurrenceId",
                table: "PersonalTrainingSessions");

            migrationBuilder.DropColumn(
                name: "RecurrenceId",
                table: "ClassSessions");
        }
    }
}
