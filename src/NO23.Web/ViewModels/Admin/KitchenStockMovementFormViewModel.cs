using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Admin;

public class KitchenStockMovementFormViewModel
{
    [Range(1, int.MaxValue)]
    [Display(Name = "Malzeme")]
    public int KitchenIngredientId { get; set; }

    [Display(Name = "Hareket tipi")]
    public KitchenStockMovementType Type { get; set; } = KitchenStockMovementType.StockIn;

    [Range(0, 1000000)]
    [Display(Name = "Miktar")]
    public decimal Quantity { get; set; }

    [StringLength(500)]
    [Display(Name = "Not")]
    public string? Note { get; set; }
}
