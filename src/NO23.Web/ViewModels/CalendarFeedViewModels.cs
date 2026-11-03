using System.ComponentModel.DataAnnotations;
using NO23.Web.Services;

namespace NO23.Web.ViewModels;

public sealed record CalendarFeedQuery(DateTime FromUtc, DateTime ToUtc, int? TrainerId, int? MemberId, bool IncludeCompleted);

public sealed record CalendarLessonProgress(string? PackageName, int? Ordinal, int? Total, int? Remaining, DateTime? PackageEndsAtUtc)
{
    public string Describe()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(PackageName)) parts.Add(PackageName);
        if (Ordinal.HasValue) parts.Add(Total.HasValue ? $"{Ordinal}. ders / {Total}" : $"{Ordinal}. ders");
        if (Remaining.HasValue) parts.Add($"Kalan {Remaining}");
        if (PackageEndsAtUtc.HasValue) parts.Add($"Bitiş {ClubTime.ToLocal(PackageEndsAtUtc.Value):dd.MM.yyyy}");
        return string.Join(" · ", parts);
    }
}

// Shape of one FullCalendar event; fields FullCalendar does not know end up in extendedProps.
public sealed class CalendarFeedEvent
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Start { get; init; } = string.Empty;
    public string End { get; init; } = string.Empty;
    public bool StartEditable { get; init; }
    public bool DurationEditable { get; init; }
    public string Kind { get; init; } = string.Empty;
    public int EntityId { get; init; }
    public int? TrainerId { get; init; }
    public string? TrainerName { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public bool IsDone { get; init; }
    public string? Progress { get; init; }
    public int? Capacity { get; init; }
    public int? ReservedCount { get; init; }
    public IReadOnlyList<string> Participants { get; init; } = [];
}

public sealed class CalendarMoveInput
{
    public const string Personal = "personal";
    public const string Group = "group";

    [Required, RegularExpression("^(personal|group)$")] public string Kind { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int Id { get; set; }
    [Required] public DateTime Start { get; set; }
    [Range(15, 240)] public int? DurationMinutes { get; set; }
}
