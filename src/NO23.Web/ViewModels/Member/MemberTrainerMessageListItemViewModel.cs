namespace NO23.Web.ViewModels.Member;

public class MemberTrainerMessageListItemViewModel
{
    public int Id { get; init; }

    public string Body { get; init; } =
        string.Empty;

    public DateTime SentAtUtc { get; init; }

    public bool IsMine { get; init; }
}