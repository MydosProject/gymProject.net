namespace NO23.Web.ViewModels.Member;

public class KitchenMenuItemCardViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public int Calories { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal ProteinGrams { get; init; }

    public decimal CarbohydrateGrams { get; init; }

    public decimal FatGrams { get; init; }

    public string Ingredients { get; init; } = string.Empty;

    public IReadOnlyList<string> AllergenNames { get; init; } = [];

    public IReadOnlyList<int> AllergenIds { get; init; } = [];

    public IReadOnlyList<string> MatchingAllergenNames { get; init; } = [];

    public bool HasAllergenConflict => MatchingAllergenNames.Count > 0;

    public string? Tags { get; init; }

    public IReadOnlyList<string> TagList { get; init; } = [];
}
