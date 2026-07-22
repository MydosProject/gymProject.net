namespace NO23.Web.ViewModels.Classes;

public class ClassesIndexViewModel
{
    public IReadOnlyList<GroupClassPublicViewModel> GroupClasses { get; init; } = [];

    public IReadOnlyList<UpcomingClassSessionPublicViewModel> UpcomingSessions { get; init; } = [];

    public string ReservationTargetUrl { get; init; } = string.Empty;
}
