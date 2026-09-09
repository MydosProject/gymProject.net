using System.ComponentModel.DataAnnotations;

namespace NO23.Web.ViewModels;

public class AppointmentRequestViewModel
{
    public static IReadOnlyList<string> Services { get; } = ["Tanışma ve paket danışmanlığı", "Personal Training", "Reformer", "Performance grup dersleri", "Kids Club"];
    [Required(ErrorMessage = "Adını ve soyadını yazmalısın."), StringLength(160)]
    [Display(Name = "Ad Soyad")] public string FullName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Telefon numaranı yazmalısın."), Phone, StringLength(40)]
    [Display(Name = "Telefon")] public string PhoneNumber { get; set; } = string.Empty;
    [Required, Display(Name = "İlgilendiğin hizmet")] public string Service { get; set; } = Services[0];
    [Required(ErrorMessage = "Tercih ettiğin tarihi seçmelisin."), DataType(DataType.Date)]
    [Display(Name = "Tercih edilen tarih")] public DateOnly? PreferredDate { get; set; }
    [Required(ErrorMessage = "Tercih ettiğin saati seçmelisin."), DataType(DataType.Time)]
    [Display(Name = "Tercih edilen saat")] public TimeOnly? PreferredTime { get; set; }
    [StringLength(1000), Display(Name = "Notun")] public string? Notes { get; set; }
}
