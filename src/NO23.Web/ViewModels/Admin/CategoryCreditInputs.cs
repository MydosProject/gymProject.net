using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;
namespace NO23.Web.ViewModels.Admin;

public sealed class CategoryBalanceInput
{
    public LessonCategory Category { get; set; }
    [Range(0, 100000)] public int Granted { get; set; }
    [Range(0, 100000)] public int Remaining { get; set; }
}
public sealed class CategoryMigrationInput
{
    public int MemberId { get; set; }
    public int ExpectedBalance { get; set; }
    public DateOnly? StartsOn { get; set; }
    public List<CategoryBalanceInput> Balances { get; set; } = [];
    [Required, StringLength(400, MinimumLength = 5)] public string Reason { get; set; } = "";
    public bool Confirmed { get; set; }
}
public sealed class CreditConversionInput
{
    public int MemberId { get; set; }
    public int RuleId { get; set; }
    [Range(1, 1000)] public int Times { get; set; } = 1;
    [Required, StringLength(300, MinimumLength = 5)] public string Reason { get; set; } = "";
    public Guid OperationId { get; set; }
}
public sealed class CreditRuleInput
{
    public bool IncreaseTargetMonthlyLimit { get; set; }
    [Required, StringLength(120)] public string Name { get; set; } = "";
    public LessonCategory From { get; set; }
    public LessonCategory To { get; set; }
    [Range(1, 1000)] public int SourceCredits { get; set; } = 1;
    [Range(1, 1000)] public int TargetCredits { get; set; } = 1;
}
