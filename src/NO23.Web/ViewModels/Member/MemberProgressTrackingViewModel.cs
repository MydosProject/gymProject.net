namespace NO23.Web.ViewModels.Member;

public class MemberProgressTrackingViewModel
{
    public MemberProgressEntryInputViewModel Input { get; init; } = new();

    public IReadOnlyList<MemberCalorieChartItemViewModel> CalorieChartItems
        { get; init; } = [];

    public int AverageCalories { get; init; }

    public int HighestCalories { get; init; }

    public int LoggedDayCount { get; init; }
}