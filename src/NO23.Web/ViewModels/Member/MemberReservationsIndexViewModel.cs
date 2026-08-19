namespace NO23.Web.ViewModels.Member;

public class MemberReservationsIndexViewModel
{
    public IReadOnlyList<MemberReservationViewModel> UpcomingReservations { get; init; } = [];

    public IReadOnlyList<AvailableClassSessionViewModel> AvailableSessions { get; init; } = [];

    public IReadOnlyList<PersonalTrainerOptionViewModel> Trainers { get; init; } = [];

    public int? SelectedTrainerId { get; init; }

    public PersonalTrainingRequestInputViewModel PersonalTrainingRequestInput { get; init; } =
        new();

    public IReadOnlyList<string> PreferredTimeWindows { get; init; } = [];

    public IReadOnlyList<PersonalTrainingRequestListItemViewModel> PersonalTrainingRequests { get; init; } = [];

    public bool CanRequestPersonalTraining { get; init; }

    public string PersonalTrainingUnavailableTitle { get; init; } =
        "Birebir talep oluşturulamıyor.";

    public string PersonalTrainingUnavailableMessage { get; init; } =
        "Birebir antrenman talebi oluşturmak için aktif ve uygun bir üyelik paketin olmalı.";
}
