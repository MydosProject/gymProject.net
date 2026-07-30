using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Admin;

public class KitchenMenuItemRecipeIngredientInputViewModel
{
    public int KitchenIngredientId { get; set; }

    public string IngredientName { get; set; } = string.Empty;

    public KitchenIngredientUnit Unit { get; set; }

    public string UnitDisplayName { get; set; } = string.Empty;

    [Range(0, 1000000)]
    [Display(Name = "Porsiyon başı miktar")]
    public decimal QuantityPerPortion { get; set; }
}
