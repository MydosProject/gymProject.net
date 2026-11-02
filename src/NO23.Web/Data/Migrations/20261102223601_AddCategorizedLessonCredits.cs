using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategorizedLessonCredits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LessonAccountId",
                table: "PersonalTrainingSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LessonGrantSnapshotJson",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LessonPeriodId",
                table: "MemberProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MembershipActivationPending",
                table: "MemberProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CreditCategory",
                table: "GroupClasses",
                type: "integer",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.Sql("UPDATE \"GroupClasses\" SET \"CreditCategory\" = 2 WHERE \"Name\" IN ('Bootcamp', 'Metcon', 'Kalça & Crunch', 'Mat Pilates'); UPDATE \"GroupClasses\" SET \"CreditCategory\" = 3 WHERE \"Name\" = 'Reformer Pilates';");

            migrationBuilder.AddColumn<int>(
                name: "LessonAccountId",
                table: "ClassReservations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LessonConversionRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    From = table.Column<int>(type: "integer", nullable: false),
                    To = table.Column<int>(type: "integer", nullable: false),
                    SourceCredits = table.Column<int>(type: "integer", nullable: false),
                    TargetCredits = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IncreaseTargetMonthlyLimit = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonConversionRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MemberLessonAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MemberProfileId = table.Column<int>(type: "integer", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Granted = table.Column<int>(type: "integer", nullable: false),
                    Remaining = table.Column<int>(type: "integer", nullable: false),
                    ConvertedCredits = table.Column<int>(type: "integer", nullable: false),
                    MonthlyLimit = table.Column<int>(type: "integer", nullable: true),
                    IsUnlimited = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberLessonAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberLessonAccounts_MemberProfiles_MemberProfileId",
                        column: x => x.MemberProfileId,
                        principalTable: "MemberProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonCreditMovements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    Delta = table.Column<int>(type: "integer", nullable: false),
                    Actor = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonCreditMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonCreditMovements_MemberLessonAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "MemberLessonAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTrainingSessions_LessonAccountId",
                table: "PersonalTrainingSessions",
                column: "LessonAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassReservations_LessonAccountId",
                table: "ClassReservations",
                column: "LessonAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonCreditMovements_AccountId_OperationId",
                table: "LessonCreditMovements",
                columns: new[] { "AccountId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberLessonAccounts_MemberProfileId_PeriodId_Category",
                table: "MemberLessonAccounts",
                columns: new[] { "MemberProfileId", "PeriodId", "Category" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassReservations_MemberLessonAccounts_LessonAccountId",
                table: "ClassReservations",
                column: "LessonAccountId",
                principalTable: "MemberLessonAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalTrainingSessions_MemberLessonAccounts_LessonAccount~",
                table: "PersonalTrainingSessions",
                column: "LessonAccountId",
                principalTable: "MemberLessonAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassReservations_MemberLessonAccounts_LessonAccountId",
                table: "ClassReservations");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonalTrainingSessions_MemberLessonAccounts_LessonAccount~",
                table: "PersonalTrainingSessions");

            migrationBuilder.DropTable(
                name: "LessonConversionRules");

            migrationBuilder.DropTable(
                name: "LessonCreditMovements");

            migrationBuilder.DropTable(
                name: "MemberLessonAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PersonalTrainingSessions_LessonAccountId",
                table: "PersonalTrainingSessions");

            migrationBuilder.DropIndex(
                name: "IX_ClassReservations_LessonAccountId",
                table: "ClassReservations");

            migrationBuilder.DropColumn(
                name: "LessonAccountId",
                table: "PersonalTrainingSessions");

            migrationBuilder.DropColumn(
                name: "LessonGrantSnapshotJson",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "LessonPeriodId",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "MembershipActivationPending",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "CreditCategory",
                table: "GroupClasses");

            migrationBuilder.DropColumn(
                name: "LessonAccountId",
                table: "ClassReservations");
        }
    }
}
