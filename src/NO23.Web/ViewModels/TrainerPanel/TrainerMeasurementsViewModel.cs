using Microsoft.AspNetCore.Mvc.Rendering;
using NO23.Web.Domain.Entities;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.ViewModels.TrainerPanel;

public class TrainerMeasurementsViewModel
{
    public List<SelectListItem> Members { get; init; } = [];

    public int? MemberId { get; init; }

    public string MemberName { get; init; } = string.Empty;

    public MemberProgressEntryInputViewModel Input { get; set; } = new();

    public List<MemberProgressEntry> Entries { get; init; } = [];
}
