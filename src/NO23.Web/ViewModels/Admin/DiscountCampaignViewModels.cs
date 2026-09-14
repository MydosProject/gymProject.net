using System.ComponentModel.DataAnnotations;

namespace NO23.Web.ViewModels.Admin;

public class DiscountCampaignFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Kampanya kodu zorunludur.")]
    [StringLength(40)]
    [RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "Kod yalnızca harf, rakam, tire ve alt çizgi içerebilir.")]
    [Display(Name = "Kampanya kodu")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kampanya adı zorunludur.")]
    [StringLength(120)]
    [Display(Name = "Kampanya adı")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "İndirim oranı %1 ile %100 arasında olmalıdır.")]
    [Display(Name = "İndirim oranı (%)")]
    public int DiscountPercent { get; set; } = 10;

    [Range(typeof(decimal), "0", "10000000")]
    [Display(Name = "Minimum sepet tutarı")]
    public decimal MinimumSubtotal { get; set; }

    [Display(Name = "Başlangıç")]
    public DateTime? StartsAt { get; set; }

    [Display(Name = "Bitiş")]
    public DateTime? EndsAt { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Kullanım limiti en az 1 olmalıdır.")]
    [Display(Name = "Kullanım limiti")]
    public int? UsageLimit { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartsAt.HasValue && EndsAt.HasValue && EndsAt <= StartsAt)
        {
            yield return new ValidationResult(
                "Bitiş zamanı başlangıç zamanından sonra olmalıdır.",
                [nameof(EndsAt)]);
        }
    }
}

public class DiscountCampaignListItemViewModel
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int DiscountPercent { get; init; }
    public decimal MinimumSubtotal { get; init; }
    public DateTime? StartsAtUtc { get; init; }
    public DateTime? EndsAtUtc { get; init; }
    public int? UsageLimit { get; init; }
    public int UsedCount { get; init; }
    public bool IsActive { get; init; }
}
