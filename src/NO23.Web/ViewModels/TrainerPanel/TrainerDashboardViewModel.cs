namespace NO23.Web.ViewModels.TrainerPanel;

public class TrainerDashboardViewModel
{
    public string TrainerName { get; set; } = string.Empty;

    public string Specialty { get; set; } = string.Empty;

    public int PendingRequestCount { get; set; }

    public int UpcomingPersonalTrainingCount { get; set; }

    public int ActiveGroupClassCount { get; set; }

    public int UpcomingClassSessionCount { get; set; }

    public List<TrainerPersonalTrainingRequestListItemViewModel>
        RecentPersonalTrainingRequests { get; set; } = [];

    public List<TrainerClassSessionListItemViewModel>
        UpcomingClassSessions { get; set; } = [];
}