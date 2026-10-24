using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using NO23.Web.ViewModels;

namespace NO23.Web.Infrastructure;

[HtmlTargetElement("form")]
public sealed class CalendarContextTagHelper : TagHelper
{
    [ViewContext, HtmlAttributeNotBound] public ViewContext ViewContext { get; set; } = null!;
    public override int Order => 10000;
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (ViewContext.ViewData["CalendarNavigation"] is not CalendarNavigationViewModel navigation ||
            !string.Equals(output.Attributes["method"]?.Value?.ToString(), "post", StringComparison.OrdinalIgnoreCase)) return;
        foreach (var (name, value) in new[] { ("CalendarView", navigation.Mode), ("CalendarDate", navigation.Date.ToString("yyyy-MM-dd")) })
        {
            var input = new TagBuilder("input");
            input.Attributes["type"] = "hidden"; input.Attributes["name"] = name; input.Attributes["value"] = value;
            output.PostContent.AppendHtml(input);
        }
    }
}
