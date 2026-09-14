namespace NO23.Web.ViewModels.Member;

public class MemberProgressTrackingViewModel
{
    public MemberProgressEntryInputViewModel Input { get; init; } = new();

    public IReadOnlyList<MemberCalorieChartItemViewModel> CalorieChartItems
        { get; init; } = [];

    public int AverageCalories { get; init; }

    public int HighestCalories { get; init; }

    public int LoggedDayCount { get; init; }

    public IReadOnlyList<MemberProgressMeasurementHistoryItemViewModel>
        MeasurementHistory { get; init; } = [];

    public int MeasurementDayCount => MeasurementHistory.Count;
}

public class MemberProgressMeasurementHistoryItemViewModel
{
    public DateOnly EntryDate { get; init; }

    public decimal? BodyWeightKg { get; init; }

    public decimal? BodyFatKg { get; init; }

    public decimal? BodyFatPercent { get; init; }

    public decimal? MuscleMassKg { get; init; }

    public decimal? MuscleMassPercent { get; init; }

    public decimal? BodyWaterAmount { get; init; }

    public decimal? BodyWaterPercent { get; init; }

    public decimal? DailyWaterIntakeLiters { get; init; }
}
