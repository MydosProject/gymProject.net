using System.ComponentModel.DataAnnotations;

namespace NO23.Web.ViewModels.Member;

public class MemberGoalsIndexViewModel
{
    [StringLength(160, ErrorMessage = "Hedef en fazla 160 karakter olabilir.")]
    [Display(Name = "Fitness hedefi")]
    public string? FitnessGoal { get; set; }

    public string MembershipPackageName { get; set; } = string.Empty;

    public string MembershipPackageAudience { get; set; } = string.Empty;

    public string MembershipPackageDescription { get; set; } = string.Empty;

    public int RemainingClassCredits { get; set; }

    public bool HasUnlimitedClasses { get; set; }

    public IReadOnlyList<string> IncludedBenefits { get; set; } = [];
}
