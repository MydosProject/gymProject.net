using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Admin;

public class KitchenMenuItemFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required]
    [StringLength(140)]
    [Display(Name = "Ürün adı")]
    public string Name { get; set; } = string.Empty;

    [StringLength(700)]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Kategori")]
    public MenuItemCategory Category { get; set; }

    [Range(1, 3000)]
    [Display(Name = "Kalori")]
    public int Calories { get; set; }

    [Range(0, 100000)]
    [Display(Name = "Fiyat")]
    public decimal UnitPrice { get; set; }

    [Range(0, 300)]
    [Display(Name = "Protein")]
    public decimal ProteinGrams { get; set; }

    [Range(0, 400)]
    [Display(Name = "Karbonhidrat")]
    public decimal CarbohydrateGrams { get; set; }

    [Range(0, 200)]
    [Display(Name = "Yağ")]
    public decimal FatGrams { get; set; }

    [Required]
    [StringLength(1000)]
    [Display(Name = "İçerik")]
    public string Ingredients { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Etiketler")]
    public string? Tags { get; set; }

    [StringLength(500)]
    [Display(Name = "Görsel URL")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    [Range(1, 100)]
    [Display(Name = "Sıralama")]
    public int DisplayOrder { get; set; }

    public List<KitchenMenuItemRecipeIngredientInputViewModel> RecipeIngredients { get; set; } = [];

    [Display(Name = "Alerjenler")]
    public List<int> SelectedAllergenIds { get; set; } = [];

    public List<KitchenAllergenOptionViewModel> AllergenOptions { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsAllowedImageUrl(ImageUrl))
        {
            yield return new ValidationResult(
                "Görsel bağlantısı https:// ile başlamalı.",
                [nameof(ImageUrl)]);
        }
    }

    // Empty, an http(s) link, or a path on this site ("/images/...").
    public static bool IsAllowedImageUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var url = value.Trim();
        if (url.StartsWith('/') && !url.StartsWith("//"))
        {
            return true;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }
}
