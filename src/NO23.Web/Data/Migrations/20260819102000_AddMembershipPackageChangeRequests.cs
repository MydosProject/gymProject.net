using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NO23.Web.Data;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260819102000_AddMembershipPackageChangeRequests")]
    public partial class AddMembershipPackageChangeRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MembershipPackageChangeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MemberProfileId = table.Column<int>(type: "integer", nullable: false),
                    CurrentMembershipPackageId = table.Column<int>(type: "integer", nullable: false),
                    RequestedMembershipPackageId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Pending"),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    AdminNote = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipPackageChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageChangeRequests_AspNetUsers_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PackageChangeRequests_MemberProfiles_MemberProfileId",
                        column: x => x.MemberProfileId,
                        principalTable: "MemberProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackageChangeRequests_Packages_CurrentPackageId",
                        column: x => x.CurrentMembershipPackageId,
                        principalTable: "MembershipPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageChangeRequests_Packages_RequestedPackageId",
                        column: x => x.RequestedMembershipPackageId,
                        principalTable: "MembershipPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPackageChangeRequests_CurrentMembershipPackageId",
                table: "MembershipPackageChangeRequests",
                column: "CurrentMembershipPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPackageChangeRequests_MemberProfileId_Status",
                table: "MembershipPackageChangeRequests",
                columns: new[] { "MemberProfileId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPackageChangeRequests_MemberProfileId_Pending",
                table: "MembershipPackageChangeRequests",
                column: "MemberProfileId",
                unique: true,
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPackageChangeRequests_RequestedAtUtc",
                table: "MembershipPackageChangeRequests",
                column: "RequestedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPackageChangeRequests_RequestedMembershipPackageId",
                table: "MembershipPackageChangeRequests",
                column: "RequestedMembershipPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPackageChangeRequests_ResolvedByUserId",
                table: "MembershipPackageChangeRequests",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPackageChangeRequests_Status",
                table: "MembershipPackageChangeRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MembershipPackageChangeRequests");
        }
    }
}
