using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Admin;

public class CommunityChallengeFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Başlık alanı zorunludur.")]
    [StringLength(180, ErrorMessage = "Başlık en fazla 180 karakter olabilir.")]
    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "URL kısa adı alanı zorunludur.")]
    [StringLength(180, ErrorMessage = "URL kısa adı en fazla 180 karakter olabilir.")]
    [Display(Name = "URL kısa adı")]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kısa özet alanı zorunludur.")]
    [StringLength(500, ErrorMessage = "Kısa özet en fazla 500 karakter olabilir.")]
    [Display(Name = "Kısa özet")]
    public string Summary { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama alanı zorunludur.")]
    [StringLength(4000, ErrorMessage = "Açıklama en fazla 4000 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hedef alanı zorunludur.")]
    [StringLength(500, ErrorMessage = "Hedef en fazla 500 karakter olabilir.")]
    [Display(Name = "Hedef")]
    public string Goal { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Ödül en fazla 500 karakter olabilir.")]
    [Display(Name = "Ödül")]
    public string? Reward { get; set; }

    [Range(800, 5000, ErrorMessage = "Hedef kalori 800 ile 5000 kcal arasında olmalıdır.")]
    [Display(Name = "Günlük kalori hedefi")]
    public int TargetDailyCalories { get; set; } = 2000;

    [Range(0, 50, ErrorMessage = "Tolerans yüzde 0 ile 50 arasında olmalıdır.")]
    [Display(Name = "Kalori toleransı")]
    public decimal CalorieTolerancePercent { get; set; } = 10;

    [Range(1, 100, ErrorMessage = "Tamamlama oranı yüzde 1 ile 100 arasında olmalıdır.")]
    [Display(Name = "Tamamlama oranı")]
    public int RequiredCompletionPercent { get; set; } = 80;

    [Display(Name = "Başlangıç tarihi")]
    public DateOnly StartsOn { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(7));

    [Display(Name = "Bitiş tarihi")]
    public DateOnly EndsOn { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(28));

    [Display(Name = "Durum")]
    public CommunityChallengeStatus Status { get; set; } = CommunityChallengeStatus.Upcoming;

    [StringLength(500, ErrorMessage = "Görsel URL en fazla 500 karakter olabilir.")]
    [Display(Name = "Görsel URL")]
    public string? ImageUrl { get; set; }

    [Range(1, 100, ErrorMessage = "Sıralama 1 ile 100 arasında olmalıdır.")]
    [Display(Name = "Sıralama")]
    public int DisplayOrder { get; set; } = 10;
}
