using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NO23.Web.Data.Migrations;

public partial class AddMemberReferrals2 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "ReferralCode", table: "MemberProfiles", type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<int>(name: "ReferredByMemberProfileId", table: "MemberProfiles", type: "integer", nullable: true);
        migrationBuilder.Sql("UPDATE \"MemberProfiles\" SET \"ReferralCode\" = 'NO23-' || LPAD(\"Id\"::text, 8, '0') WHERE \"ReferralCode\" = '';");
        migrationBuilder.CreateIndex(name: "IX_MemberProfiles_ReferralCode", table: "MemberProfiles", column: "ReferralCode", unique: true);
        migrationBuilder.CreateIndex(name: "IX_MemberProfiles_ReferredByMemberProfileId", table: "MemberProfiles", column: "ReferredByMemberProfileId");
        migrationBuilder.AddForeignKey(name: "FK_MemberProfiles_MemberProfiles_ReferredByMemberProfileId", table: "MemberProfiles", column: "ReferredByMemberProfileId", principalTable: "MemberProfiles", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_MemberProfiles_MemberProfiles_ReferredByMemberProfileId", "MemberProfiles");
        migrationBuilder.DropIndex("IX_MemberProfiles_ReferralCode", "MemberProfiles");
        migrationBuilder.DropIndex("IX_MemberProfiles_ReferredByMemberProfileId", "MemberProfiles");
        migrationBuilder.DropColumn("ReferralCode", "MemberProfiles");
        migrationBuilder.DropColumn("ReferredByMemberProfileId", "MemberProfiles");
    }
}
