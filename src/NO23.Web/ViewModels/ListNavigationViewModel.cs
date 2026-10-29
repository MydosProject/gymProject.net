using Microsoft.AspNetCore.Mvc.Rendering;

namespace NO23.Web.ViewModels;

public sealed record ListNavigationViewModel(int TotalCount, int RequestedPage, string? Query, string? State,
    IReadOnlyList<SelectListItem> States)
{
    public const int PageSize = 25;
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public int Page => Math.Clamp(RequestedPage, 1, TotalPages);
    public int Skip => (Page - 1) * PageSize;
    public Dictionary<string, string> Routes(int page) => new()
    {
        ["page"] = page.ToString(), ["query"] = Query ?? "", ["state"] = State ?? ""
    };
}
