using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Admin;

public class ClassSessionFormViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Grup dersi")]
    public int GroupClassId { get; set; }

    [Required]
    [Display(Name = "Tarih/saat")]
    public DateTime StartsAtLocal { get; set; }

    [Range(1, 100)]
    [Display(Name = "Kontenjan")]
    public int? CapacityOverride { get; set; }

    [Display(Name = "Durum")]
    public ClassSessionStatus Status { get; set; } = ClassSessionStatus.Scheduled;

    public IReadOnlyList<SelectListItem> GroupClassOptions { get; set; } = [];
}
