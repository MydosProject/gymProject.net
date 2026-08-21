using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Seed;

public static class KitchenAllergenSeed
{
    public static IReadOnlyList<KitchenAllergen> Defaults { get; } =
    [
        new() { Name = "Gluten", Description = "Buğday, arpa, çavdar ve bunların ürünleri.", DisplayOrder = 1 },
        new() { Name = "Süt", Description = "Süt ve süt ürünleri.", DisplayOrder = 2 },
        new() { Name = "Yumurta", DisplayOrder = 3 },
        new() { Name = "Yer Fıstığı", DisplayOrder = 4 },
        new() { Name = "Sert Kabuklu Yemişler", Description = "Badem, fındık, ceviz ve benzeri yemişler.", DisplayOrder = 5 },
        new() { Name = "Soya", DisplayOrder = 6 },
        new() { Name = "Balık", DisplayOrder = 7 },
        new() { Name = "Kabuklu Deniz Ürünleri", DisplayOrder = 8 },
        new() { Name = "Susam", DisplayOrder = 9 },
        new() { Name = "Kereviz", DisplayOrder = 10 }
    ];

    public static IReadOnlyList<string> ResolveNames(string? legacyText)
    {
        if (string.IsNullOrWhiteSpace(legacyText) || legacyText.Equals("Yok", StringComparison.OrdinalIgnoreCase))
            return [];

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawName in legacyText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var name = rawName.ToLowerInvariant() switch
            {
                "badem" or "fındık" or "ceviz" => "Sert Kabuklu Yemişler",
                "yer fıstığı" => "Yer Fıstığı",
                "süt" => "Süt",
                "yumurta" => "Yumurta",
                "gluten" => "Gluten",
                "soya" => "Soya",
                "balık" => "Balık",
                "susam" => "Susam",
                "kereviz" => "Kereviz",
                _ => string.Empty
            };
            if (name.Length > 0) names.Add(name);
        }
        return names.ToList();
    }
}
