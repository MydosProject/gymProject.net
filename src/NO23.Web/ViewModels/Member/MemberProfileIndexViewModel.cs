using System.ComponentModel.DataAnnotations;

namespace NO23.Web.ViewModels.Member;

public class MemberProfileIndexViewModel
{
    [Required(ErrorMessage = "Ad alanı zorunludur.")]
    [StringLength(80, ErrorMessage = "Ad en fazla 80 karakter olabilir.")]
    [Display(Name = "Ad")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad alanı zorunludur.")]
    [StringLength(80, ErrorMessage = "Soyad en fazla 80 karakter olabilir.")]
    [Display(Name = "Soyad")]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Geçerli bir telefon numarası girmelisin.")]
    [StringLength(32, ErrorMessage = "Telefon numarası en fazla 32 karakter olabilir.")]
    [Display(Name = "Telefon")]
    public string? PhoneNumber { get; set; }

    public string Email { get; set; } = string.Empty;

    public string MembershipPackageName { get; set; } = string.Empty;

    public DateTime MemberSinceUtc { get; set; }
}
