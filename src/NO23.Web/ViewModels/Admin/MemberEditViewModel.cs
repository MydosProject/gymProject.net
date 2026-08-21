using System.ComponentModel.DataAnnotations;

namespace NO23.Web.ViewModels.Admin;

public class MemberEditViewModel
{
    public int Id { get; set; }

    [Required, StringLength(80), Display(Name = "Ad")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80), Display(Name = "Soyad")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256), Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(40), Display(Name = "Telefon")]
    public string? PhoneNumber { get; set; }

    [Range(1, int.MaxValue), Display(Name = "Üyelik paketi")]
    public int MembershipPackageId { get; set; }

    [StringLength(160), Display(Name = "Fitness hedefi")]
    public string? FitnessGoal { get; set; }

    [Range(0, int.MaxValue), Display(Name = "Kalan ders hakkı")]
    public int RemainingClassCredits { get; set; }

    [Display(Name = "Personel trainer")]
    public int? AssignedTrainerId { get; set; }
}

public class MemberDeleteViewModel
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PackageName { get; init; } = string.Empty;
    public bool HasProtectedHistory { get; init; }
    public int OrderCount { get; init; }
    public int PersonalTrainingSessionCount { get; init; }
    public int KitchenSubscriptionCount { get; init; }
}
