namespace NO23.Web.Domain.Entities;

public class MemberProgressEntry
{
    public int Id { get; set; }

    public int MemberProfileId { get; set; }

    public MemberProfile MemberProfile { get; set; } = null!;

    public DateOnly EntryDate { get; set; }

    public int? CaloriesConsumed { get; set; }

    public decimal? BodyWeightKg { get; set; }

    public decimal? BodyFatKg { get; set; }

    public decimal? BodyFatPercent { get; set; }

    public decimal? MuscleMassKg { get; set; }

    public decimal? MuscleMassPercent { get; set; }

    public decimal? BodyWaterAmount { get; set; }

    public decimal? BodyWaterPercent { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
