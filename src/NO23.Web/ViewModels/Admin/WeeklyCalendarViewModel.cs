using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using NO23.Web.Domain.Entities;
using NO23.Web.ViewModels.TrainerPanel;

namespace NO23.Web.ViewModels.Admin;

public class WeeklyCalendarViewModel
{
    public bool IncludeCompleted { get; set; }
    public DateTime Week { get; set; }
    public int? TrainerId { get; set; }
    public int? MemberId { get; set; }
    public List<SelectListItem> Trainers { get; set; } = [];
    public List<SelectListItem> Classes { get; set; } = [];
    public List<SelectListItem> Members { get; set; } = [];
    public List<MemberPackagePeriodViewModel> MemberPackagePeriods { get; set; } = [];
    public List<ClassSession> Groups { get; set; } = [];
    public List<PersonalTrainingSession> Personal { get; set; } = [];
    public List<AppointmentRequest> Appointments { get; set; } = [];

    public IReadOnlyList<WeeklyCalendarEntryViewModel> EntriesFor(DateTime date) =>
        Groups.Where(session => NO23.Web.Services.ClubTime.ToLocal(session.StartsAtUtc).Date == date.Date)
            .Select(session => new WeeklyCalendarEntryViewModel(session.StartsAtUtc, session, null))
            .Concat(Personal.Where(session => NO23.Web.Services.ClubTime.ToLocal(session.StartsAtUtc).Date == date.Date)
                .Select(session => new WeeklyCalendarEntryViewModel(session.StartsAtUtc, null, session)))
            .Concat(Appointments.Where(request => NO23.Web.Services.ClubTime.ToLocal(request.ScheduledAtUtc!.Value).Date == date.Date)
                .Select(request => new WeeklyCalendarEntryViewModel(request.ScheduledAtUtc!.Value, null, null, request)))
            .OrderBy(entry => entry.StartsAtUtc)
            .ThenBy(entry => entry.Group is null ? 1 : 0)
            .ToList();
}

public record MemberPackagePeriodViewModel(
    int Id,
    DateTime? MembershipStartsAtUtc,
    DateTime? MembershipEndsAtUtc);

public record WeeklyCalendarEntryViewModel(
    DateTime StartsAtUtc,
    ClassSession? Group,
    PersonalTrainingSession? Personal,
    AppointmentRequest? Appointment = null);

public class WeeklyGroupInput
{
    public bool IncludeCompleted { get; set; }
    public int? FilterMemberId { get; set; }
    public int? FilterTrainerId { get; set; }
    [Range(1, int.MaxValue)] public int GroupClassId { get; set; }
    public DateTime Week { get; set; } = ClubWeek();
    public DayOfWeek[] Days { get; set; } = [];
    public TimeOnly Time { get; set; } = new(18, 0);
    [Range(1, 52)] public int Weeks { get; set; } = 1;
    [Range(1, 200)] public int? Capacity { get; set; }
    private static DateTime ClubWeek() => NO23.Web.Services.ClubTime.Monday(NO23.Web.Services.ClubTime.Now);
}

public class AdminPersonalSessionInput : CreateTrainerSessionViewModel
{
    [Range(1, int.MaxValue)] public int TrainerId { get; set; }
}

public class AdminWeeklyPersonalSessionInput : WeeklyPersonalSessionInput
{
    [Range(1, int.MaxValue)] public int TrainerId { get; set; }
}

public class AdminSessionUpdateInput : UpdateTrainerSessionViewModel
{
    [Range(1, int.MaxValue)] public int TrainerId { get; set; }
}
