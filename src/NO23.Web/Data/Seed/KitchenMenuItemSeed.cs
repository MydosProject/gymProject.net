using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Data.Seed;

public static class KitchenMenuItemSeed
{
    public static IReadOnlyList<KitchenMenuItem> Defaults { get; } =
    [
        new()
        {
            Name = "Chicken Quinoa Bowl",
            Description = "Izgara tavuk, kinoa, avokado ve yeşilliklerle hazırlanan ana öğün.",
            Category = MenuItemCategory.MainMeal,
            Calories = 520,
            UnitPrice = 295,
            ProteinGrams = 42,
            CarbohydrateGrams = 48,
            FatGrams = 18,
            Ingredients = "Izgara tavuk, kinoa, avokado, roka, cherry domates, zeytinyağı",
            Allergens = "Yok",
            Tags = "yuksek protein, performans",
            DisplayOrder = 1
        },
        new()
        {
            Name = "Egg Cheese Plate",
            Description = "Yumurta beyazı, lor peyniri ve tam tahıllı ekmekle hazırlanan kahvaltı tabağı.",
            Category = MenuItemCategory.Breakfast,
            Calories = 360,
            UnitPrice = 245,
            ProteinGrams = 32,
            CarbohydrateGrams = 30,
            FatGrams = 12,
            Ingredients = "Yumurta beyazı, lor peyniri, tam tahıllı ekmek, salatalık, domates",
            Allergens = "Yumurta, süt, gluten",
            Tags = "yuksek protein, dusuk kalori",
            DisplayOrder = 2
        },
        new()
        {
            Name = "Spinach Banana Smoothie",
            Description = "Muz, ıspanak, protein tozu ve badem sütüyle hazırlanan smoothie.",
            Category = MenuItemCategory.Beverage,
            Calories = 240,
            UnitPrice = 180,
            ProteinGrams = 22,
            CarbohydrateGrams = 28,
            FatGrams = 4,
            Ingredients = "Muz, ıspanak, whey protein, badem sütü",
            Allergens = "Süt, badem",
            Tags = "saglikli yasam, performans",
            DisplayOrder = 3
        },
        new()
        {
            Name = "Cocoa Brownie",
            Description = "Badem unu, kakao ve hurmayla hazırlanan brownie.",
            Category = MenuItemCategory.Dessert,
            Calories = 210,
            UnitPrice = 165,
            ProteinGrams = 12,
            CarbohydrateGrams = 22,
            FatGrams = 9,
            Ingredients = "Badem unu, kakao, hurma, yumurta, bitter çikolata",
            Allergens = "Yumurta, badem",
            Tags = "glutensiz, saglikli yasam",
            DisplayOrder = 4
        },
        new()
        {
            Name = "Chickpea Vegetable Box",
            Description = "Nohut, lor, taze sebzeler ve yoğurtlu dip sos ile vejetaryen ara öğün.",
            Category = MenuItemCategory.Snack,
            Calories = 290,
            UnitPrice = 210,
            ProteinGrams = 21,
            CarbohydrateGrams = 26,
            FatGrams = 10,
            Ingredients = "Haşlanmış nohut, lor peyniri, havuç, salatalık, yoğurtlu dip sos",
            Allergens = "Süt",
            Tags = "vejetaryen, dusuk kalori",
            DisplayOrder = 5
        },
        new()
        {
            Name = "Oat Yogurt Cup",
            Description = "Yulaf, süzme yoğurt, chia ve orman meyveleriyle hazırlanan kahvaltı kasesi.",
            Category = MenuItemCategory.Breakfast,
            Calories = 430,
            UnitPrice = 235,
            ProteinGrams = 28,
            CarbohydrateGrams = 54,
            FatGrams = 11,
            Ingredients = "Yulaf, süzme yoğurt, chia, orman meyveleri, bal",
            Allergens = "Süt, gluten",
            Tags = "dengeli, saglikli yasam",
            DisplayOrder = 6
        },
        new()
        {
            Name = "Turkey Egg Wrap",
            Description = "Tam buğday tortilla, yumurta, hindi füme ve avokadoyla hazırlanan kahvaltı dürümü.",
            Category = MenuItemCategory.Breakfast,
            Calories = 520,
            UnitPrice = 275,
            ProteinGrams = 36,
            CarbohydrateGrams = 48,
            FatGrams = 20,
            Ingredients = "Tam buğday tortilla, yumurta, hindi füme, avokado, yeşillik",
            Allergens = "Yumurta, gluten",
            Tags = "performans, yuksek protein",
            DisplayOrder = 7
        },
        new()
        {
            Name = "Quinoa Banana Bowl",
            Description = "Kinoa, badem sütü, muz ve keten tohumuyla hazırlanan kahvaltı kasesi.",
            Category = MenuItemCategory.Breakfast,
            Calories = 390,
            UnitPrice = 250,
            ProteinGrams = 18,
            CarbohydrateGrams = 58,
            FatGrams = 10,
            Ingredients = "Kinoa, badem sütü, muz, keten tohumu, tarçın",
            Allergens = "Badem",
            Tags = "vejetaryen, saglikli yasam, glutensiz",
            DisplayOrder = 8
        },
        new()
        {
            Name = "Turkey Rice Box",
            Description = "Hindi göğüs, basmati pirinç ve sebzelerle hazırlanan ana öğün.",
            Category = MenuItemCategory.MainMeal,
            Calories = 470,
            UnitPrice = 285,
            ProteinGrams = 44,
            CarbohydrateGrams = 50,
            FatGrams = 9,
            Ingredients = "Hindi göğüs, basmati pirinç, brokoli, havuç, zeytinyağı",
            Allergens = "Yok",
            Tags = "dusuk kalori, yuksek protein",
            DisplayOrder = 9
        },
        new()
        {
            Name = "Salmon Potato Plate",
            Description = "Somon, tatlı patates, kuşkonmaz ve yeşilliklerle hazırlanan ana öğün.",
            Category = MenuItemCategory.MainMeal,
            Calories = 650,
            UnitPrice = 420,
            ProteinGrams = 46,
            CarbohydrateGrams = 58,
            FatGrams = 25,
            Ingredients = "Somon, tatlı patates, kuşkonmaz, yeşillik",
            Allergens = "Balık",
            Tags = "performans, dengeli",
            DisplayOrder = 10
        },
        new()
        {
            Name = "Beef Bulgur Bowl",
            Description = "Dana eti, bulgur, biber, kabak ve yoğurt sosla hazırlanan ana öğün.",
            Category = MenuItemCategory.MainMeal,
            Calories = 690,
            UnitPrice = 390,
            ProteinGrams = 52,
            CarbohydrateGrams = 62,
            FatGrams = 24,
            Ingredients = "Dana eti, bulgur, biber, kabak, yoğurt sos",
            Allergens = "Süt, gluten",
            Tags = "yuksek protein, performans",
            DisplayOrder = 11
        },
        new()
        {
            Name = "Chickpea Rice Plate",
            Description = "Nohut, esmer pirinç, taze sebzeler ve yoğurt sosla hazırlanan ana öğün.",
            Category = MenuItemCategory.MainMeal,
            Calories = 510,
            UnitPrice = 260,
            ProteinGrams = 24,
            CarbohydrateGrams = 66,
            FatGrams = 16,
            Ingredients = "Nohut, esmer pirinç, salatalık, domates, yoğurt sos",
            Allergens = "Süt",
            Tags = "vejetaryen, dengeli, saglikli yasam",
            DisplayOrder = 12
        },
        new()
        {
            Name = "Chicken Salad",
            Description = "Tavuk göğüs, yeşillik, salatalık, domates ve yoğurt sosla hazırlanan salata.",
            Category = MenuItemCategory.MainMeal,
            Calories = 390,
            UnitPrice = 270,
            ProteinGrams = 40,
            CarbohydrateGrams = 24,
            FatGrams = 13,
            Ingredients = "Tavuk göğüs, marul, salatalık, domates, yoğurt sos",
            Allergens = "Süt",
            Tags = "dusuk kalori, yuksek protein",
            DisplayOrder = 13
        },
        new()
        {
            Name = "Chicken Potato Box",
            Description = "Tavuk göğüs, patates ve yeşil fasulyeyle hazırlanan ana öğün.",
            Category = MenuItemCategory.MainMeal,
            Calories = 560,
            UnitPrice = 305,
            ProteinGrams = 43,
            CarbohydrateGrams = 60,
            FatGrams = 15,
            Ingredients = "Tavuk göğüs, patates, yeşil fasulye, zeytinyağı",
            Allergens = "Yok",
            Tags = "glutensiz, dengeli",
            DisplayOrder = 14
        },
        new()
        {
            Name = "Tofu Noodle Bowl",
            Description = "Tofu, pirinç eriştesi, brokoli, biber ve susam sosla hazırlanan ana öğün.",
            Category = MenuItemCategory.MainMeal,
            Calories = 540,
            UnitPrice = 295,
            ProteinGrams = 30,
            CarbohydrateGrams = 68,
            FatGrams = 16,
            Ingredients = "Tofu, pirinç eriştesi, brokoli, biber, susam sos",
            Allergens = "Soya, susam",
            Tags = "vejetaryen, saglikli yasam",
            DisplayOrder = 15
        },
        new()
        {
            Name = "Yogurt Berry Jar",
            Description = "Süzme yoğurt, whey protein ve orman meyveleriyle hazırlanan ara öğün.",
            Category = MenuItemCategory.Snack,
            Calories = 250,
            UnitPrice = 190,
            ProteinGrams = 30,
            CarbohydrateGrams = 22,
            FatGrams = 5,
            Ingredients = "Süzme yoğurt, whey protein, orman meyveleri",
            Allergens = "Süt",
            Tags = "yuksek protein, dusuk kalori",
            DisplayOrder = 16
        },
        new()
        {
            Name = "Rice Cake Peanut Stack",
            Description = "Pirinç patlağı, fıstık ezmesi ve muzla hazırlanan ara öğün.",
            Category = MenuItemCategory.Snack,
            Calories = 310,
            UnitPrice = 175,
            ProteinGrams = 12,
            CarbohydrateGrams = 42,
            FatGrams = 11,
            Ingredients = "Pirinç patlağı, fıstık ezmesi, muz",
            Allergens = "Yer fıstığı",
            Tags = "performans, glutensiz",
            DisplayOrder = 17
        },
        new()
        {
            Name = "Cottage Cheese Fruit Cup",
            Description = "Lor peyniri, elma ve tarçınla hazırlanan ara öğün.",
            Category = MenuItemCategory.Snack,
            Calories = 220,
            UnitPrice = 185,
            ProteinGrams = 24,
            CarbohydrateGrams = 20,
            FatGrams = 5,
            Ingredients = "Lor peyniri, elma, tarçın",
            Allergens = "Süt",
            Tags = "dengeli, yuksek protein",
            DisplayOrder = 18
        },
        new()
        {
            Name = "Hummus Vegetable Cup",
            Description = "Humus, havuç, salatalık ve kereviz sapıyla hazırlanan ara öğün.",
            Category = MenuItemCategory.Snack,
            Calories = 260,
            UnitPrice = 170,
            ProteinGrams = 10,
            CarbohydrateGrams = 30,
            FatGrams = 11,
            Ingredients = "Humus, havuç, salatalık, kereviz sapı",
            Allergens = "Susam",
            Tags = "vejetaryen, saglikli yasam",
            DisplayOrder = 19
        },
        new()
        {
            Name = "Oat Cocoa Bites",
            Description = "Yulaf, whey protein, kakao ve fıstık ezmesiyle hazırlanan ara öğün.",
            Category = MenuItemCategory.Snack,
            Calories = 330,
            UnitPrice = 195,
            ProteinGrams = 24,
            CarbohydrateGrams = 34,
            FatGrams = 11,
            Ingredients = "Yulaf, whey protein, kakao, fıstık ezmesi",
            Allergens = "Süt, yer fıstığı, gluten",
            Tags = "performans, yuksek protein",
            DisplayOrder = 20
        }
    ];
}
