using System.ComponentModel.DataAnnotations;

namespace NO23.Web.ViewModels.Member;

public class MemberProgressEntryInputViewModel
{
    [Required(ErrorMessage = "Tarih alanı zorunludur.")]
    [Display(Name = "Tarih")]
    public DateOnly EntryDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Range(1, 10000, ErrorMessage = "Kalori 1 ile 10000 kcal arasında olmalıdır.")]
    [Display(Name = "Kalori")]
    public int? CaloriesConsumed { get; set; }

    [Range(1, 500, ErrorMessage = "Vücut ağırlığı 1 ile 500 kg arasında olmalıdır.")]
    [Display(Name = "Vücut ağırlığı")]
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(NO23.Web.Services.MeasurementDecimalBinder))]
    public decimal? BodyWeightKg { get; set; }

    [Range(0, 300, ErrorMessage = "Yağ ağırlığı 0 ile 300 kg arasında olmalıdır.")]
    [Display(Name = "Yağ ağırlığı")]
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(NO23.Web.Services.MeasurementDecimalBinder))]
    public decimal? BodyFatKg { get; set; }

    [Range(0, 100, ErrorMessage = "Yağ oranı 0 ile 100 arasında olmalıdır.")]
    [Display(Name = "Yağ oranı")]
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(NO23.Web.Services.MeasurementDecimalBinder))]
    public decimal? BodyFatPercent { get; set; }

    [Range(0, 300, ErrorMessage = "Kas kütlesi 0 ile 300 kg arasında olmalıdır.")]
    [Display(Name = "Kas kütlesi")]
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(NO23.Web.Services.MeasurementDecimalBinder))]
    public decimal? MuscleMassKg { get; set; }

    [Range(0, 100, ErrorMessage = "Kas oranı 0 ile 100 arasında olmalıdır.")]
    [Display(Name = "Kas oranı")]
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(NO23.Web.Services.MeasurementDecimalBinder))]
    public decimal? MuscleMassPercent { get; set; }

    [Range(0, 300, ErrorMessage = "Vücut suyu miktarı 0 ile 300 arasında olmalıdır.")]
    [Display(Name = "Vücut suyu miktarı")]
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(NO23.Web.Services.MeasurementDecimalBinder))]
    public decimal? BodyWaterAmount { get; set; }

    [Range(0, 100, ErrorMessage = "Vücut suyu oranı 0 ile 100 arasında olmalıdır.")]
    [Display(Name = "Vücut suyu oranı")]
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(NO23.Web.Services.MeasurementDecimalBinder))]
    public decimal? BodyWaterPercent { get; set; }
}
