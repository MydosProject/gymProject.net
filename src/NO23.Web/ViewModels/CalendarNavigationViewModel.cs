using Microsoft.AspNetCore.Mvc.Rendering;
using NO23.Web.Services;

namespace NO23.Web.ViewModels;

public sealed class CalendarNavigationViewModel
{
    public DateTime Date { get; init; }
    public string Mode { get; init; } = "week";
    public int? TrainerId { get; init; }
    public int? MemberId { get; init; }
    public bool IncludeCompleted { get; init; }
    public bool ShowTrainer { get; init; }
    public IReadOnlyList<SelectListItem> Trainers { get; init; } = [];
    public IReadOnlyList<SelectListItem> Members { get; init; } = [];
    public DateTime Previous => Date.AddDays(Mode == "day" ? -1 : -7);
    public DateTime Next => Date.AddDays(Mode == "day" ? 1 : 7);
    public string Title => Mode == "day" ? Date.ToString("dd MMMM yyyy dddd", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"))
        : ClubTime.Monday(Date).ToString("dd MMM", System.Globalization.CultureInfo.GetCultureInfo("tr-TR")) + " – " + ClubTime.Monday(Date).AddDays(6).ToString("dd MMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
}
