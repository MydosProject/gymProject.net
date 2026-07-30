using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Admin;

public class KitchenMenuItemFormViewModel
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
    [Display(Name = "Alerjen")]
    public string? Allergens { get; set; }

    [StringLength(500)]
    [Display(Name = "Etiketler")]
    public string? Tags { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    [Range(1, 100)]
    [Display(Name = "Sıralama")]
    public int DisplayOrder { get; set; }

    public List<KitchenMenuItemRecipeIngredientInputViewModel> RecipeIngredients { get; set; } = [];
}
