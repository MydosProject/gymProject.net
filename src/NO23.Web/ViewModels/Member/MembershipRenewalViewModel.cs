using System.ComponentModel.DataAnnotations;

namespace NO23.Web.ViewModels.Member;

public sealed class MembershipRenewalViewModel
{
    public string CurrentPackageName { get; set; } = string.Empty;
    public DateTime? MembershipEndsAtUtc { get; set; }
    public bool PaymentEnabled { get; set; }
    public List<MembershipRenewalOptionViewModel> Options { get; set; } = [];
    public MembershipRenewalInputViewModel Input { get; set; } = new();
}

public sealed class MembershipRenewalOptionViewModel
{
    public int Id { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public int DurationMonths { get; set; }
    public int? DurationDays { get; set; }
    public int TotalCredits { get; set; }
}

public sealed class MembershipRenewalInputViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Bir paket seçmelisin.")]
    public int VariantId { get; set; }

    [Required, StringLength(160)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string BillingAddress { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string District { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;
}
