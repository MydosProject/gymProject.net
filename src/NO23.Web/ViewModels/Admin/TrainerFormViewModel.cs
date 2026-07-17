using System.ComponentModel.DataAnnotations;

namespace NO23.Web.ViewModels.Admin;

public class TrainerFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(80)]
    [Display(Name = "Ad")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    [Display(Name = "Soyad")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(160)]
    [Display(Name = "Uzmanlık alanı")]
    public string Specialty { get; set; } = string.Empty;

    [StringLength(600)]
    [Display(Name = "Sertifikalar")]
    public string? Certifications { get; set; }

    [StringLength(1200)]
    [Display(Name = "Kısa bio")]
    public string? Bio { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}
