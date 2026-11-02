using NO23.Web.Domain.Enums;
namespace NO23.Web.Domain.Entities;

public class MemberLessonAccount
{
    public int Id { get; set; }
    public int MemberProfileId { get; set; }
    public MemberProfile MemberProfile { get; set; } = null!;
    public Guid PeriodId { get; set; }
    public LessonCategory Category { get; set; }
    public int Granted { get; set; }
    public int Remaining { get; set; }
    public int ConvertedCredits { get; set; }
    public int? MonthlyLimit { get; set; }
    public bool IsUnlimited { get; set; }
    public ICollection<LessonCreditMovement> Movements { get; set; } = [];
}

public class LessonCreditMovement
{
    public long Id { get; set; }
    public int AccountId { get; set; }
    public MemberLessonAccount Account { get; set; } = null!;
    public int Delta { get; set; }
    public string Actor { get; set; } = "";
    public string Reason { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? OperationId { get; set; }
}

public class LessonConversionRule
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public LessonCategory From { get; set; }
    public LessonCategory To { get; set; }
    public int SourceCredits { get; set; } = 1;
    public int TargetCredits { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public bool IncreaseTargetMonthlyLimit { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
