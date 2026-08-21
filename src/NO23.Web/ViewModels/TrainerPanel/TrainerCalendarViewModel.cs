using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.TrainerPanel;

public class TrainerCalendarViewModel
{
    public IReadOnlyList<TrainerCalendarSessionViewModel> Sessions { get; init; } = [];
    public IReadOnlyList<TrainerAssignedMemberViewModel> Members { get; init; } = [];
    public DateTime WeekStart { get; init; }
    public DateTime WeekEnd { get; init; }
    public IReadOnlyList<TrainerCalendarDayViewModel> Days { get; init; } = [];
}

public class TrainerCalendarDayViewModel
{
    public DateTime Date { get; init; }
    public string DayName { get; init; } = string.Empty;
    public bool IsToday { get; init; }
    public IReadOnlyList<TrainerCalendarSessionViewModel> Sessions { get; init; } = [];
}

public class TrainerCalendarSessionViewModel
{
    public int Id { get; init; }
    public string MemberName { get; init; } = string.Empty;
    public DateTime StartsAtUtc { get; init; }
    public int DurationMinutes { get; init; }
    public PersonalTrainingSessionStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public int RemainingCredits { get; init; }
    public bool IsUnlimited { get; init; }
    public string? Note { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public IReadOnlyList<TrainerCalendarHistoryViewModel> History { get; init; } = [];
}

public class TrainerCalendarHistoryViewModel
{
    public string StatusName { get; init; } = string.Empty;
    public PersonalTrainingSessionStatus Status { get; init; }
    public DateTime PreviousStartsAtUtc { get; init; }
    public DateTime NewStartsAtUtc { get; init; }
    public string? Note { get; init; }
    public DateTime ChangedAtUtc { get; init; }
}

public class TrainerAssignedMemberViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int RemainingCredits { get; init; }
    public bool IsUnlimited { get; init; }
}

public class CreateTrainerSessionViewModel
{
    [Range(1, int.MaxValue)] public int MemberProfileId { get; set; }
    [Required] public DateTime StartsAt { get; set; }
    [Range(15, 240)] public int DurationMinutes { get; set; } = 60;
    [StringLength(600)] public string? Note { get; set; }
    public DateTime? Week { get; set; }
}

public class UpdateTrainerSessionViewModel
{
    public int Id { get; set; }
    public PersonalTrainingSessionStatus Status { get; set; }
    public DateTime? PostponedStartsAt { get; set; }
    [StringLength(600)] public string? Note { get; set; }
    public DateTime? Week { get; set; }
}
