using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class KitchenMenuItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public MenuItemCategory Category { get; set; }

    public int Calories { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal ProteinGrams { get; set; }

    public decimal CarbohydrateGrams { get; set; }

    public decimal FatGrams { get; set; }

    public string Ingredients { get; set; } = string.Empty;

    public string? Allergens { get; set; }

    public string? Tags { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsPlanEligible { get; set; } = true;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<CartItem> CartItems { get; set; } = [];

    public ICollection<OrderItem> OrderItems { get; set; } = [];

    public ICollection<KitchenMealPlanItem> MealPlanItems { get; set; } = [];
}
