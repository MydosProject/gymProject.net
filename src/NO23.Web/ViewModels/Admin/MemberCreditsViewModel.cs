using NO23.Web.ViewModels.Member;

namespace NO23.Web.ViewModels.Admin;

public sealed record MemberCreditHistoryRow(DateTime StartsAtUtc, string Kind, string Description, string Status, string CreditState);
public sealed class MemberCreditsViewModel
{
    public string MemberName { get; init; } = string.Empty;
    public LessonCreditSummary Summary { get; init; } = new();
    public IReadOnlyList<MemberCreditHistoryRow> History { get; init; } = [];
}
