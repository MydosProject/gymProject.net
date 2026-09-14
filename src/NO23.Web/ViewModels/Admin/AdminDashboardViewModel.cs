namespace NO23.Web.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public int TotalMembers { get; set; }

    public int ActiveTrainers { get; set; }

    public int TodayClassSessions { get; set; }

    public int UpcomingCommunityEvents { get; set; }

    public int PendingPersonalTrainingRequests { get; set; }

    public int PendingOrders { get; set; }

    public IReadOnlyList<PersonalTrainingRequestListItemViewModel>
        RecentPersonalTrainingRequests { get; init; } = [];

    public IReadOnlyList<TrainerMonthlyLessonViewModel> MonthlyTrainerLessons { get; set; } = [];
}

public class TrainerMonthlyLessonViewModel
{
    public string TrainerName { get; init; } = string.Empty;
    public int CompletedLessonCountThisWeek { get; init; }
    public int CompletedLessonCount { get; init; }
}
