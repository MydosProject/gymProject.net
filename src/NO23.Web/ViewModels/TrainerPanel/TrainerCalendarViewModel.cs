using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;
using NO23.Web.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace NO23.Web.ViewModels.TrainerPanel;

public class TrainerCalendarViewModel
{
    public DateTime SelectedDate { get; init; }
    public string ViewMode { get; init; } = "week";
    public NO23.Web.ViewModels.Member.LessonCreditSummary? Credits { get; init; }
    public IEnumerable<TrainerCalendarDayViewModel> VisibleDays => ViewMode == "day" ? Days.Where(x => x.Date == SelectedDate) : Days;
    public IReadOnlyList<TrainerCalendarSessionViewModel> VisibleSessions => ViewMode == "day" ? Sessions.Where(x => NO23.Web.Services.ClubTime.ToLocal(x.StartsAtUtc).Date == SelectedDate).ToList() : Sessions;
    public NO23.Web.ViewModels.CalendarNavigationViewModel Navigation => new() { Date = SelectedDate, Mode = ViewMode, MemberId = MemberId, IncludeCompleted = IncludeCompleted, Members = FilterMembers };
    public bool IncludeCompleted { get; set; }
    public int? MemberId { get; init; }
    public IReadOnlyList<SelectListItem> FilterMembers { get; init; } = [];
    public IReadOnlyList<ClassSession> Groups { get; init; } = [];
    public IReadOnlyList<AppointmentRequest> Appointments { get; init; } = [];
    public IReadOnlyList<TrainerCalendarSessionViewModel> Sessions { get; init; } = [];
    public IReadOnlyList<TrainerAssignedMemberViewModel> Members { get; init; } = [];
    public DateTime WeekStart { get; init; }
    public DateTime WeekEnd { get; init; }
    public IReadOnlyList<TrainerCalendarDayViewModel> Days { get; init; } = [];
}

public class TrainerCalendarDayViewModel
{
    public IReadOnlyList<AppointmentRequest> Appointments { get; init; } = [];
    public IReadOnlyList<ClassSession> Groups { get; init; } = [];
    public DateTime Date { get; init; }
    public string DayName { get; init; } = string.Empty;
    public bool IsToday { get; init; }
    public IReadOnlyList<TrainerCalendarSessionViewModel> Sessions { get; init; } = [];
    public IReadOnlyList<TrainerCalendarEntryViewModel> Entries => Groups
        .Select(x => new TrainerCalendarEntryViewModel(x.StartsAtUtc, x, null, null))
        .Concat(Sessions.Select(x => new TrainerCalendarEntryViewModel(x.StartsAtUtc, null, x, null)))
        .Concat(Appointments.Select(x => new TrainerCalendarEntryViewModel(x.ScheduledAtUtc!.Value, null, null, x)))
        .OrderBy(x => x.StartsAtUtc).ToList();
}

public record TrainerCalendarEntryViewModel(DateTime StartsAtUtc, ClassSession? Group,
    TrainerCalendarSessionViewModel? Personal, AppointmentRequest? Appointment);

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
    public int RemainingCredits { get; set; }
    public bool IsUnlimited { get; init; }
    public DateTime? MembershipStartsAtUtc { get; init; }
    public DateTime? MembershipEndsAtUtc { get; init; }
}

public class CreateTrainerSessionViewModel
{
    public bool IncludeCompleted { get; set; }
    public int? FilterMemberId { get; set; }
    public int? FilterTrainerId { get; set; }
    [Range(1, int.MaxValue)] public int MemberProfileId { get; set; }
    [Required] public DateTime StartsAt { get; set; }
    [Range(15, 240)] public int DurationMinutes { get; set; } = 50;
    [StringLength(600)] public string? Note { get; set; }
    public DateTime? Week { get; set; }
}

public class WeeklyPersonalSessionInput
{
    public bool IncludeCompleted { get; set; }
    public int? FilterMemberId { get; set; }
    public int? FilterTrainerId { get; set; }
    [Range(1, int.MaxValue)] public int MemberProfileId { get; set; }
    [Required] public DateTime Week { get; set; } = NO23.Web.Services.ClubTime.Monday(NO23.Web.Services.ClubTime.Now);
    public DayOfWeek[] Days { get; set; } = [];
    public TimeOnly Time { get; set; } = new(11, 0);
    public int Weeks { get; set; } = 1;
    [Range(15, 240)] public int DurationMinutes { get; set; } = 50;
    [StringLength(600)] public string? Note { get; set; }
}

public class UpdateTrainerSessionViewModel
{
    public bool IncludeCompleted { get; set; }
    public int? FilterMemberId { get; set; }
    public int? FilterTrainerId { get; set; }
    public int Id { get; set; }
    public PersonalTrainingSessionStatus Status { get; set; }
    public DateTime? PostponedStartsAt { get; set; }
    [StringLength(600)] public string? Note { get; set; }
    public DateTime? Week { get; set; }
}
