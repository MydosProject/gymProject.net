using System.ComponentModel.DataAnnotations;

namespace NO23.Web.ViewModels.Admin;

public class ShopProductFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(140)]
    [Display(Name = "Product name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    [Display(Name = "SKU")]
    public string Sku { get; set; } = string.Empty;

    [StringLength(700)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required]
    [StringLength(80)]
    [Display(Name = "Category")]
    public string Category { get; set; } = string.Empty;

    [Range(0, 100000)]
    [Display(Name = "Unit price")]
    public decimal UnitPrice { get; set; }

    [Range(0, 100000)]
    [Display(Name = "Stock quantity")]
    public int StockQuantity { get; set; }

    [StringLength(500)]
    [Display(Name = "Image URL")]
    public string? ImageUrl { get; set; }

    [StringLength(500)]
    [Display(Name = "Tags")]
    public string? Tags { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Range(1, 100)]
    [Display(Name = "Display order")]
    public int DisplayOrder { get; set; }
}
