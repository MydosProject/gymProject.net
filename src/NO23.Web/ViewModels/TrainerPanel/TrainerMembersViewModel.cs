using NO23.Web.Domain.Entities;

namespace NO23.Web.ViewModels.TrainerPanel;

public sealed class TrainerMembersViewModel
{
    public IReadOnlyDictionary<int, NO23.Web.ViewModels.Member.LessonCreditSummary> Credits { get; init; } = new Dictionary<int, NO23.Web.ViewModels.Member.LessonCreditSummary>();
    public string? Query { get; init; }
    public int Page { get; init; }
    public int TotalCount { get; init; }
    public const int PageSize = 30;
    public IReadOnlyList<MemberProfile> Members { get; init; } = [];
}
