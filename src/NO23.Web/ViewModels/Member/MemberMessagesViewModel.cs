namespace NO23.Web.ViewModels.Member;

public class MemberMessagesViewModel
{
    public IReadOnlyList<MemberConversationListItemViewModel>
        Conversations { get; init; } = [];

    public MemberConversationDetailViewModel?
        ActiveConversation { get; init; }
}