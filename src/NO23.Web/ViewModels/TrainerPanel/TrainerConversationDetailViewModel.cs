namespace NO23.Web.ViewModels.TrainerPanel;

public class TrainerConversationDetailViewModel
{
    public int Id { get; init; }

    public string MemberName { get; init; } = string.Empty;

    public string MemberEmail { get; init; } = string.Empty;

    public bool CanWrite { get; init; }

    public IReadOnlyList<TrainerMessageListItemViewModel> Messages { get; init; } = [];
}