using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Data.Seed;

public static class KitchenSubscriptionPackageSeed
{
    public static IReadOnlyList<KitchenSubscriptionPackage> Defaults { get; } =
    [
        new()
        {
            Plan = KitchenSubscriptionPlan.FiveDays,
            Name = "5 Günlük Kitchen Paketi",
            Description = "Kalori ve makro hedeflerine göre hazırlanan 5 günlük NO23 Kitchen yemek paketi.",
            Days = 5,
            UnitPrice = 200,
            IsActive = true,
            DisplayOrder = 10
        },
        new()
        {
            Plan = KitchenSubscriptionPlan.TenDays,
            Name = "10 Günlük Kitchen Paketi",
            Description = "Düzenli beslenme ritmini kurmak için 10 günlük NO23 Kitchen yemek paketi.",
            Days = 10,
            UnitPrice = 700,
            IsActive = true,
            DisplayOrder = 20
        },
        new()
        {
            Plan = KitchenSubscriptionPlan.TwentyDays,
            Name = "20 Günlük Kitchen Paketi",
            Description = "Uzun süreli hedef takibi için 20 günlük NO23 Kitchen yemek paketi.",
            Days = 20,
            UnitPrice = 1200,
            IsActive = true,
            DisplayOrder = 30
        },
        new()
        {
            Plan = KitchenSubscriptionPlan.Monthly,
            Name = "Aylık Kitchen Paketi",
            Description = "Aylık rutin oluşturmak isteyen üyeler için 30 günlük NO23 Kitchen yemek paketi.",
            Days = 30,
            UnitPrice = 1500,
            IsActive = true,
            DisplayOrder = 40
        }
    ];
}
