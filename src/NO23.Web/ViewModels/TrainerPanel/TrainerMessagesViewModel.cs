namespace NO23.Web.ViewModels.TrainerPanel;

public class TrainerMessagesViewModel
{
    public IReadOnlyList<TrainerConversationListItemViewModel>
        Conversations { get; init; } = [];

    public TrainerConversationDetailViewModel?
        ActiveConversation { get; init; }

    public IReadOnlyList<TrainerMessageMemberOptionViewModel> AvailableMembers { get; init; } = [];
}

public class TrainerMessageMemberOptionViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
