namespace NO23.Web.ViewModels.Member;

public class MemberReservationsIndexViewModel
{
    public LessonCreditSummary? Credits { get; init; }
    public IReadOnlyList<MemberPersonalSessionViewModel> UpcomingPersonalSessions { get; init; } = [];
    public IReadOnlyList<MemberReservationViewModel> UpcomingReservations { get; init; } = [];

    public IReadOnlyList<AvailableClassSessionViewModel> AvailableSessions { get; init; } = [];

    public MemberClassScheduleViewModel Schedule { get; init; } = new();

    public IReadOnlyList<PersonalTrainerOptionViewModel> Trainers { get; init; } = [];

    public int? SelectedTrainerId { get; init; }

    public PersonalTrainingRequestInputViewModel PersonalTrainingRequestInput { get; init; } =
        new();

    public IReadOnlyList<string> PreferredTimeWindows { get; init; } = [];

    public IReadOnlyList<PersonalTrainingRequestListItemViewModel> PersonalTrainingRequests { get; init; } = [];

    public bool CanRequestPersonalTraining { get; init; }
}
