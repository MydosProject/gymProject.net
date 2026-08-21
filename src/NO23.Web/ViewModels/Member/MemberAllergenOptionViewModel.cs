namespace NO23.Web.ViewModels.Member;

public class MemberAllergenOptionViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsSelected { get; set; }
}
