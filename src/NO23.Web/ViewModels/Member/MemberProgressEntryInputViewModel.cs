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

    [Range(50, 250, ErrorMessage = "Boy 50 ile 250 cm arasında olmalıdır.")]
    [Display(Name = "Boy")]
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(NO23.Web.Services.MeasurementDecimalBinder))]
    public decimal? HeightCm { get; set; }

    [Range(10, 300, ErrorMessage = "Omuz ölçüsü 10 ile 300 cm arasında olmalıdır.")]
    [Display(Name = "Omuz")]
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(NO23.Web.Services.MeasurementDecimalBinder))]
    public decimal? ShoulderCm { get; set; }

    [Range(10, 300, ErrorMessage = "Göğüs ölçüsü 10 ile 300 cm arasında olmalıdır.")]
    [Display(Name = "Göğüs")]
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(NO23.Web.Services.MeasurementDecimalBinder))]
    public decimal? ChestCm { get; set; }

    [Range(10, 150, ErrorMessage = "Sağ kol ölçüsü 10 ile 150 cm arasında olmalıdır.")]
    [Display(Name = "Sağ kol")]
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(NO23.Web.Services.MeasurementDecimalBinder))]
    public decimal? RightArmCm { get; set; }

    [Range(10, 150, ErrorMessage = "Sol kol ölçüsü 10 ile 150 cm arasında olmalıdır.")]
    [Display(Name = "Sol kol")]
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(NO23.Web.Services.MeasurementDecimalBinder))]
    public decimal? LeftArmCm { get; set; }

    [Range(10, 300, ErrorMessage = "Bel ölçüsü 10 ile 300 cm arasında olmalıdır.")]
    [Display(Name = "Bel")]
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(NO23.Web.Services.MeasurementDecimalBinder))]
    public decimal? WaistCm { get; set; }

    [Range(10, 300, ErrorMessage = "Karın ölçüsü 10 ile 300 cm arasında olmalıdır.")]
    [Display(Name = "Karın")]
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(NO23.Web.Services.MeasurementDecimalBinder))]
    public decimal? AbdomenCm { get; set; }

    [Range(10, 300, ErrorMessage = "Basen ölçüsü 10 ile 300 cm arasında olmalıdır.")]
    [Display(Name = "Basen")]
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(NO23.Web.Services.MeasurementDecimalBinder))]
    public decimal? HipCm { get; set; }

    [Range(10, 200, ErrorMessage = "Sağ üst bacak ölçüsü 10 ile 200 cm arasında olmalıdır.")]
    [Display(Name = "Sağ üst bacak")]
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(NO23.Web.Services.MeasurementDecimalBinder))]
    public decimal? RightUpperLegCm { get; set; }

    [Range(10, 200, ErrorMessage = "Sol üst bacak ölçüsü 10 ile 200 cm arasında olmalıdır.")]
    [Display(Name = "Sol üst bacak")]
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(NO23.Web.Services.MeasurementDecimalBinder))]
    public decimal? LeftUpperLegCm { get; set; }

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

    [Range(0, 20, ErrorMessage = "Günlük su tüketimi 0 ile 20 litre arasında olmalıdır.")]
    [Display(Name = "Günlük su tüketimi")]
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(NO23.Web.Services.MeasurementDecimalBinder))]
    public decimal? DailyWaterIntakeLiters { get; set; }
}
