using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Admin;

public class PersonalTrainingRequestFormViewModel
{
    public int Id { get; set; }

    public string MemberName { get; init; } = string.Empty;

    public string MemberEmail { get; init; } = string.Empty;

    public string TrainerName { get; init; } = string.Empty;

    public bool TrainerIsActive { get; init; }

    public DateOnly PreferredDate { get; init; }

    public string PreferredTimeWindow { get; init; } = string.Empty;

    public string? GoalNote { get; init; }

    public PersonalTrainingRequestStatus CurrentStatus { get; init; }

    public string CurrentStatusDisplayName { get; init; } = string.Empty;

    [Display(Name = "Durum")]
    public PersonalTrainingRequestStatus Status { get; set; }

    [Display(Name = "Kesin randevu tarihi")]
    public DateTime? ScheduledAtLocal { get; set; }

    public string? TrainerNote { get; init; }

    [StringLength(1200)]
    [Display(Name = "Yönetici notu")]
    public string? AdminNote { get; set; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public bool CanPlan { get; init; }
}
