using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Admin;

public class KitchenIngredientFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    [Display(Name = "Malzeme adı")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Birim")]
    public KitchenIngredientUnit Unit { get; set; } = KitchenIngredientUnit.Gram;

    [Range(0, 1000000)]
    [Display(Name = "Mevcut stok")]
    public decimal CurrentStockQuantity { get; set; }

    [Range(0, 1000000)]
    [Display(Name = "Minimum stok")]
    public decimal MinimumStockQuantity { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}
