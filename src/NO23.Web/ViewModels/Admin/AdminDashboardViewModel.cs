namespace NO23.Web.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public int TotalMembers { get; set; }

    public int ActiveTrainers { get; set; }

    public int TodayClassSessions { get; set; }

    public int UpcomingCommunityEvents { get; set; }

    public int PendingPersonalTrainingRequests { get; set; }

    public int PendingOrders { get; set; }
}