namespace NO23.Web.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public int TotalMembers { get; set; }

    public int ActiveTrainers { get; set; }

    public int TodayClassSessions { get; set; }

    public int UpcomingCommunityEvents { get; set; }

    public int PendingPersonalTrainingRequests { get; set; }

    public int PendingOrders { get; set; }
    public int PendingAppointmentRequests { get; set; }
    public int PendingPackageApplications { get; set; }
    public int TotalPendingRequests => PendingAppointmentRequests + PendingPackageApplications + PendingPersonalTrainingRequests;
    public IReadOnlyList<AdminPendingRequestItem> PendingRequests { get; set; } = [];

    public IReadOnlyList<PersonalTrainingRequestListItemViewModel>
        RecentPersonalTrainingRequests { get; init; } = [];

    public IReadOnlyList<TrainerMonthlyLessonViewModel> MonthlyTrainerLessons { get; set; } = [];
}

public record AdminPendingRequestItem(int Id, string Controller, string Kind, string Name, string Phone,
    string Detail, DateTime CreatedAtUtc);

public class TrainerMonthlyLessonViewModel
{
    public string TrainerName { get; init; } = string.Empty;
    public int CompletedLessonCountThisWeek { get; init; }
    public int CompletedLessonCount { get; init; }
}
