using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using NO23.Web.Domain.Entities;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.ViewModels.Admin;

public class MemberProgressPage
{
    public int? MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public List<SelectListItem> Members { get; set; } = [];
    public MemberProgressEntryInputViewModel Input { get; set; } = new();
    public List<MemberProgressEntry> Entries { get; set; } = [];
    public List<ProgressPhotoInfo> Photos { get; set; } = [];
    public int? Before { get; set; }
    public int? After { get; set; }
}

public record ProgressPhotoInfo(int Id, DateOnly TakenOn, string? Caption);

public class ProgressPhotoInput
{
    [Range(1, int.MaxValue)] public int MemberId { get; set; }
    public DateOnly TakenOn { get; set; }
    [StringLength(200)] public string? Caption { get; set; }
    [Required] public IFormFile? Photo { get; set; }
}
