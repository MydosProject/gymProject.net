namespace NO23.Web.ViewModels.Member;

public class MemberReservationsIndexViewModel
{
    public IReadOnlyList<MemberReservationViewModel> UpcomingReservations { get; init; } = [];

    public IReadOnlyList<AvailableClassSessionViewModel> AvailableSessions { get; init; } = [];

    public IReadOnlyList<PersonalTrainerOptionViewModel> Trainers { get; init; } = [];

    public int? SelectedTrainerId { get; init; }
}
