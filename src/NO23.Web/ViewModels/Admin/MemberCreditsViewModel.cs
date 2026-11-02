using NO23.Web.ViewModels.Member;

namespace NO23.Web.ViewModels.Admin;

public sealed record MemberCreditHistoryRow(DateTime StartsAtUtc, string Kind, string Description, string Status, string CreditState);
public sealed class MemberCreditsViewModel
{
    public IReadOnlyList<NO23.Web.Domain.Entities.LessonConversionRule> ConversionRules { get; init; } = [];
    public IReadOnlyList<NO23.Web.Domain.Entities.LessonCreditMovement> Movements { get; init; } = [];
    public CategoryMigrationInput Migration { get; init; } = new();
    public string MemberName { get; init; } = string.Empty;
    public LessonCreditSummary Summary { get; init; } = new();
    public IReadOnlyList<MemberCreditHistoryRow> History { get; init; } = [];
}
