using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainerMessagingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainerConversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MemberProfileId = table.Column<int>(type: "integer", nullable: false),
                    TrainerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastMessageAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainerConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainerConversations_MemberProfiles_MemberProfileId",
                        column: x => x.MemberProfileId,
                        principalTable: "MemberProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainerConversations_Trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "Trainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainerMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainerConversationId = table.Column<int>(type: "integer", nullable: false),
                    SenderApplicationUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainerMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainerMessages_AspNetUsers_SenderApplicationUserId",
                        column: x => x.SenderApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainerMessages_TrainerConversations_TrainerConversationId",
                        column: x => x.TrainerConversationId,
                        principalTable: "TrainerConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainerConversations_LastMessageAtUtc",
                table: "TrainerConversations",
                column: "LastMessageAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerConversations_MemberProfileId_TrainerId",
                table: "TrainerConversations",
                columns: new[] { "MemberProfileId", "TrainerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainerConversations_TrainerId",
                table: "TrainerConversations",
                column: "TrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerMessages_SenderApplicationUserId",
                table: "TrainerMessages",
                column: "SenderApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerMessages_TrainerConversationId_ReadAtUtc",
                table: "TrainerMessages",
                columns: new[] { "TrainerConversationId", "ReadAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainerMessages_TrainerConversationId_SentAtUtc",
                table: "TrainerMessages",
                columns: new[] { "TrainerConversationId", "SentAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainerMessages");

            migrationBuilder.DropTable(
                name: "TrainerConversations");
        }
    }
}
