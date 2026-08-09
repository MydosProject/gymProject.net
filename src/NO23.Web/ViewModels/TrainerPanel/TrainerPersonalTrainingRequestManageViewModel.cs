using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.TrainerPanel;

public class TrainerPersonalTrainingRequestManageViewModel
{
    public int Id { get; set; }

    public string MemberName { get; set; } = string.Empty;

    public string MemberEmail { get; set; } = string.Empty;

    public DateOnly PreferredDate { get; set; }

    public string PreferredTimeWindow { get; set; } = string.Empty;

    public string? GoalNote { get; set; }

    public PersonalTrainingRequestStatus CurrentStatus { get; set; }

    public string CurrentStatusDisplayName { get; set; } = string.Empty;

    [Display(Name = "Kesin randevu tarihi ve saati")]
    public DateTime? ScheduledAtLocal { get; set; }

    [StringLength(1200)]
    [Display(Name = "Eğitmen notu")]
    public string? TrainerNote { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public bool CanManage =>
        CurrentStatus == PersonalTrainingRequestStatus.Pending;
}