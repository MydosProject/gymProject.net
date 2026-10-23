using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using NO23.Web.Domain.Enums;
using NO23.Web.Infrastructure.Validation;
using NO23.Web.Services;

namespace NO23.Web.ViewModels.Admin;

public class ManualPaymentInputViewModel
{
    [EnumDataType(typeof(PaymentMethod)), Display(Name = "Ödeme yöntemi")]
    public PaymentMethod Method { get; set; } = PaymentMethod.CreditCard;

    [ModelBinder(BinderType = typeof(MoneyAmountModelBinder)), Display(Name = "Toplam ödeme tutarı")]
    public decimal? Amount { get; set; }

    [ModelBinder(BinderType = typeof(MoneyAmountModelBinder)), Display(Name = "İlave indirim (%)")]
    public decimal DiscountPercent { get; set; }

    [StringLength(600), Display(Name = "İndirim / özel fiyat açıklaması")]
    public string? PriceAdjustmentNote { get; set; }

    [Range(1, 36), Display(Name = "Taksit sayısı")]
    public int InstallmentCount { get; set; } = 1;

    [DataType(DataType.Date), Display(Name = "İlk vade")]
    public DateOnly FirstDueDate { get; set; } = DateOnly.FromDateTime(ClubTime.Now);

    [StringLength(160), Display(Name = "POS / dekont referansı")]
    public string? Reference { get; set; }

    public List<PaymentInstallmentInputViewModel> Installments { get; set; } = [];
}

public class PaymentInstallmentInputViewModel
{
    [ModelBinder(BinderType = typeof(MoneyAmountModelBinder)), Display(Name = "Tutar")]
    public decimal? Amount { get; set; }

    [DataType(DataType.Date), Display(Name = "Vade")]
    public DateOnly DueDate { get; set; }

    [DataType(DataType.Date), Display(Name = "Tahsilat tarihi")]
    public DateOnly? PaidOn { get; set; }
}
