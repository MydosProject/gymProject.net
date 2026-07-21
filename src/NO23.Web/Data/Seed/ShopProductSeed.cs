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
            Description = "Premium pamuk karışımlı oversize antrenman ve günlük kullanım hoodie.",
            Category = "Giyim",
            UnitPrice = 1850,
            StockQuantity = 24,
            Tags = "hoodie, lifestyle, giyim",
            DisplayOrder = 10
        },
        new()
        {
            Name = "NO23 Training T-Shirt",
            Sku = "NO23-TSHIRT-001",
            Description = "Nefes alan kumaşlı, antrenman odaklı NO23 t-shirt.",
            Category = "Giyim",
            UnitPrice = 750,
            StockQuantity = 40,
            Tags = "t-shirt, antrenman, giyim",
            DisplayOrder = 20
        },
        new()
        {
            Name = "NO23 Shaker",
            Sku = "NO23-SHAKER-001",
            Description = "Protein ve supplement karışımları için sızdırmaz shaker.",
            Category = "Aksesuar",
            UnitPrice = 320,
            StockQuantity = 60,
            Tags = "shaker, aksesuar",
            DisplayOrder = 30
        },
        new()
        {
            Name = "Direnç Bandı Seti",
            Sku = "NO23-BAND-001",
            Description = "Isınma, mobilite ve kuvvet destek egzersizleri için direnç bandı seti.",
            Category = "Ekipman",
            UnitPrice = 540,
            StockQuantity = 35,
            Tags = "ekipman, mobilite, kuvvet",
            DisplayOrder = 40
        }
    ];
}
