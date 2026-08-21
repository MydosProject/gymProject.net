namespace NO23.Web.ViewModels.Admin;

public class KitchenAllergenListItemViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
    public int MenuItemCount { get; init; }
    public int MemberCount { get; init; }
}
