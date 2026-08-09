namespace NO23.Web.ViewModels.Member;

public class MemberConversationListItemViewModel
{
    public int Id { get; init; }

    public string TrainerName { get; init; } =
        string.Empty;

    public string Specialty { get; init; } =
        string.Empty;

    public string? LastMessage { get; init; }

    public DateTime LastActivityAtUtc { get; init; }
}