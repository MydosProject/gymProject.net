namespace NO23.Web.ViewModels.Member;

public class MemberConversationDetailViewModel
{
    public int Id { get; init; }

    public string TrainerName { get; init; } =
        string.Empty;

    public string Specialty { get; init; } =
        string.Empty;

    public bool CanWrite { get; init; }

    public IReadOnlyList<MemberTrainerMessageListItemViewModel>
        Messages { get; init; } = [];
}