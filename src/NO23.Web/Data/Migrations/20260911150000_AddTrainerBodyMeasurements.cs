using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NO23.Web.Data;

#nullable disable

namespace NO23.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260911150000_AddTrainerBodyMeasurements")]
public partial class AddTrainerBodyMeasurements : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var columns = new (string Name, string Type)[]
        {
            ("HeightCm", "numeric(6,2)"), ("ShoulderCm", "numeric(6,2)"), ("ChestCm", "numeric(6,2)"),
            ("RightArmCm", "numeric(6,2)"), ("LeftArmCm", "numeric(6,2)"), ("WaistCm", "numeric(6,2)"),
            ("AbdomenCm", "numeric(6,2)"), ("HipCm", "numeric(6,2)"), ("RightUpperLegCm", "numeric(6,2)"),
            ("LeftUpperLegCm", "numeric(6,2)")
        };
        foreach (var column in columns)
            migrationBuilder.AddColumn<decimal?>(column.Name, "MemberProgressEntries", type: column.Type, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        var columns = new[] { "HeightCm", "ShoulderCm", "ChestCm", "RightArmCm", "LeftArmCm", "WaistCm", "AbdomenCm", "HipCm", "RightUpperLegCm", "LeftUpperLegCm" };
        foreach (var column in columns) migrationBuilder.DropColumn(column, "MemberProgressEntries");
    }
}
