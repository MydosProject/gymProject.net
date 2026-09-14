using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public sealed record KitchenSubscriptionPriceQuote(
    int SelectedMealSlotsMask,
    int MainMealCount,
    KitchenMealSlot SnackSlot,
    decimal PackagePrice);

public static class KitchenSubscriptionPricing
{
    public static bool TryCalculate(
        KitchenSubscriptionPackage package,
        IEnumerable<KitchenMealSlot>? selectedMeals,
        out KitchenSubscriptionPriceQuote? quote,
        out string? errorMessage)
    {
        quote = null;
        errorMessage = null;

        if (!KitchenMealSelection.TryCreateMask(selectedMeals, out var selectedMealMask))
        {
            errorMessage = "Paketin için geçerli öğün seçimleri yapmalısın.";
            return false;
        }

        var meals = selectedMeals!.Distinct().ToList();
        var mainMealCount = meals.Count(KitchenMealSelection.IsMainMeal);
        var snackSlots = meals.Where(KitchenMealSelection.IsSnack).ToList();

        if (mainMealCount is < 1 or > 3)
        {
            errorMessage = "Kahvaltı, öğle ve akşam seçeneklerinden en az birini seçmelisin.";
            return false;
        }

        if (snackSlots.Count != 1)
        {
            errorMessage = "Paket fiyatına dahil olan bir ara öğün saatini seçmelisin.";
            return false;
        }

        var packagePrice = mainMealCount switch
        {
            1 => package.UnitPrice,
            2 => package.TwoMainMealsPrice,
            3 => package.ThreeMainMealsPrice,
            _ => 0
        };

        if (packagePrice <= 0)
        {
            errorMessage = "Seçilen öğün düzeni için paket fiyatı tanımlanmamış.";
            return false;
        }

        quote = new KitchenSubscriptionPriceQuote(
            selectedMealMask,
            mainMealCount,
            snackSlots[0],
            packagePrice);
        return true;
    }
}
