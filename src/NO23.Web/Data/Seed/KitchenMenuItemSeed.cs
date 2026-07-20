using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Data.Seed;

public static class KitchenMenuItemSeed
{
    public static IReadOnlyList<KitchenMenuItem> Defaults { get; } =
    [
        new()
        {
            Name = "Protein Power Bowl",
            Description = "Izgara tavuk, kinoa, avokado ve yeşilliklerle dengeli ana öğün.",
            Category = MenuItemCategory.MainMeal,
            Calories = 520,
            ProteinGrams = 42,
            CarbohydrateGrams = 48,
            FatGrams = 18,
            Ingredients = "Izgara tavuk, kinoa, avokado, roka, cherry domates, zeytinyağı",
            Allergens = "Yok",
            Tags = "High Protein, Performance",
            DisplayOrder = 1
        },
        new()
        {
            Name = "Lean Breakfast Plate",
            Description = "Yumurta beyazı, lor peyniri ve tam tahıllı ekmek ile hafif kahvaltı.",
            Category = MenuItemCategory.Breakfast,
            Calories = 360,
            ProteinGrams = 32,
            CarbohydrateGrams = 30,
            FatGrams = 12,
            Ingredients = "Yumurta beyazı, lor peyniri, tam tahıllı ekmek, salatalık, domates",
            Allergens = "Yumurta, süt, gluten",
            Tags = "High Protein, Low Calorie",
            DisplayOrder = 2
        },
        new()
        {
            Name = "Green Recovery Smoothie",
            Description = "Antrenman sonrası toparlanma için yeşil smoothie.",
            Category = MenuItemCategory.Beverage,
            Calories = 240,
            ProteinGrams = 22,
            CarbohydrateGrams = 28,
            FatGrams = 4,
            Ingredients = "Muz, ıspanak, whey protein, badem sütü",
            Allergens = "Süt, badem",
            Tags = "Recovery, Beverage",
            DisplayOrder = 3
        },
        new()
        {
            Name = "Gluten Free Fit Brownie",
            Description = "Düşük şekerli, glutensiz tatlı alternatifi.",
            Category = MenuItemCategory.Dessert,
            Calories = 210,
            ProteinGrams = 12,
            CarbohydrateGrams = 22,
            FatGrams = 9,
            Ingredients = "Badem unu, kakao, hurma, yumurta, bitter çikolata",
            Allergens = "Yumurta, badem",
            Tags = "Gluten Free, Dessert",
            DisplayOrder = 4
        }
    ];
}
