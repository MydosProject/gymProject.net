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
            Description = "Günlük 1-3 ana öğün ve 1 ara öğün seçimiyle hazırlanan 5 günlük deneme paketi.",
            Days = 5,
            UnitPrice = 3000,
            TwoMainMealsPrice = 5500,
            ThreeMainMealsPrice = 7500,
            DailyDeliveryFee = 95,
            IsActive = true,
            DisplayOrder = 10
        },
        new()
        {
            Plan = KitchenSubscriptionPlan.TenDays,
            Name = "10 Günlük Kitchen Paketi",
            Description = "Düzenli beslenme ritmini kurmak için 10 günlük NO23 Kitchen yemek paketi.",
            Days = 10,
            UnitPrice = 7900,
            TwoMainMealsPrice = 7900,
            ThreeMainMealsPrice = 7900,
            DailyDeliveryFee = 95,
            IsActive = false,
            DisplayOrder = 20
        },
        new()
        {
            Plan = KitchenSubscriptionPlan.TwentyDays,
            Name = "20 Günlük Kitchen Paketi",
            Description = "Günlük 1-3 ana öğün ve 1 ara öğün seçimiyle hazırlanan 20 günlük abonelik paketi.",
            Days = 20,
            UnitPrice = 10000,
            TwoMainMealsPrice = 19000,
            ThreeMainMealsPrice = 28000,
            DailyDeliveryFee = 95,
            IsActive = true,
            DisplayOrder = 30
        },
        new()
        {
            Plan = KitchenSubscriptionPlan.Monthly,
            Name = "Aylık Kitchen Paketi",
            Description = "Aylık rutin oluşturmak isteyen üyeler için 30 günlük NO23 Kitchen yemek paketi.",
            Days = 30,
            UnitPrice = 19900,
            TwoMainMealsPrice = 19900,
            ThreeMainMealsPrice = 19900,
            DailyDeliveryFee = 95,
            IsActive = false,
            DisplayOrder = 40
        }
    ];
}
