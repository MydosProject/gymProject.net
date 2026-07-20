using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Seed;

public static class ShopProductSeed
{
    public static IReadOnlyList<ShopProduct> Defaults =>
    [
        new()
        {
            Name = "NO23 Oversize Hoodie",
            Sku = "NO23-HOODIE-001",
            Description = "Premium pamuk karisimli oversize antrenman ve gunluk kullanim hoodie.",
            Category = "Apparel",
            UnitPrice = 1850,
            StockQuantity = 24,
            Tags = "hoodie, lifestyle, apparel",
            DisplayOrder = 10
        },
        new()
        {
            Name = "NO23 Training T-Shirt",
            Sku = "NO23-TSHIRT-001",
            Description = "Nefes alan kumasli, antrenman odakli NO23 t-shirt.",
            Category = "Apparel",
            UnitPrice = 750,
            StockQuantity = 40,
            Tags = "t-shirt, training, apparel",
            DisplayOrder = 20
        },
        new()
        {
            Name = "NO23 Shaker",
            Sku = "NO23-SHAKER-001",
            Description = "Protein ve supplement karisimlari icin sizdirmaz shaker.",
            Category = "Accessories",
            UnitPrice = 320,
            StockQuantity = 60,
            Tags = "shaker, accessory",
            DisplayOrder = 30
        },
        new()
        {
            Name = "Resistance Band Set",
            Sku = "NO23-BAND-001",
            Description = "Isinma, mobilite ve kuvvet destek egzersizleri icin direnc bandi seti.",
            Category = "Equipment",
            UnitPrice = 540,
            StockQuantity = 35,
            Tags = "equipment, mobility, strength",
            DisplayOrder = 40
        }
    ];
}
