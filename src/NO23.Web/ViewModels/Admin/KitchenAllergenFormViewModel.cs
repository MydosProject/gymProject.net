using System.ComponentModel.DataAnnotations;

namespace NO23.Web.ViewModels.Admin;

public class KitchenAllergenFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100), Display(Name = "Alerjen adı")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500), Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    [Range(1, 1000), Display(Name = "Sıralama")]
    public int DisplayOrder { get; set; } = 10;
}
