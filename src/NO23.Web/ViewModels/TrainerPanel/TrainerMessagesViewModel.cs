namespace NO23.Web.ViewModels.TrainerPanel;

public class TrainerMessagesViewModel
{
    public IReadOnlyList<TrainerConversationListItemViewModel>
        Conversations { get; init; } = [];

    public TrainerConversationDetailViewModel?
        ActiveConversation { get; init; }
}