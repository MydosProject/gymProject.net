using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestAppointmentScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "AppointmentRequests",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleStatus",
                table: "AppointmentRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledAtUtc",
                table: "AppointmentRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScheduledTrainerId",
                table: "AppointmentRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Version",
                table: "AppointmentRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "AppointmentScheduleHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppointmentRequestId = table.Column<int>(type: "integer", nullable: false),
                    PreviousStartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PreviousTrainerId = table.Column<int>(type: "integer", nullable: true),
                    TrainerId = table.Column<int>(type: "integer", nullable: true),
                    PreviousDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    PreviousStatus = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentScheduleHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentScheduleHistories_AppointmentRequests_Appointmen~",
                        column: x => x.AppointmentRequestId,
                        principalTable: "AppointmentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_ScheduledTrainerId_ScheduledAtUtc",
                table: "AppointmentRequests",
                columns: new[] { "ScheduledTrainerId", "ScheduledAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_Status_CreatedAtUtc",
                table: "AppointmentRequests",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentScheduleHistories_AppointmentRequestId",
                table: "AppointmentScheduleHistories",
                column: "AppointmentRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentRequests_Trainers_ScheduledTrainerId",
                table: "AppointmentRequests",
                column: "ScheduledTrainerId",
                principalTable: "Trainers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentRequests_Trainers_ScheduledTrainerId",
                table: "AppointmentRequests");

            migrationBuilder.DropTable(
                name: "AppointmentScheduleHistories");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentRequests_ScheduledTrainerId_ScheduledAtUtc",
                table: "AppointmentRequests");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentRequests_Status_CreatedAtUtc",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "ScheduleStatus",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "ScheduledAtUtc",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "ScheduledTrainerId",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AppointmentRequests");
        }
    }
}
