namespace NO23.Web.ViewModels.TrainerPanel;

public class TrainerConversationListItemViewModel
{
    public int Id { get; init; }

    public string MemberName { get; init; } =
        string.Empty;

    public string MemberEmail { get; init; } =
        string.Empty;

    public string? LastMessage { get; init; }

    public DateTime LastActivityAtUtc { get; init; }
}