using Microsoft.AspNetCore.Razor.TagHelpers;

namespace NO23.Web.Infrastructure;

// Explicit columns keep labels deterministic for conditional columns and action cells.
[HtmlTargetElement("table")]
public sealed class MobileTableTagHelper : TagHelper
{
    public string? MobileLabels { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        context.Items[typeof(MobileTableTagHelper)] = MobileLabels?.Split('|');
        context.Items[typeof(MobileTableRowTagHelper)] = null!;
        output.Attributes.RemoveAll("mobile-labels");
        if (MobileLabels is not null) output.Attributes.SetAttribute("data-mobile-table", "true");
        output.Content.SetHtmlContent(await output.GetChildContentAsync());
    }
}

[HtmlTargetElement("tr")]
public sealed class MobileTableRowTagHelper : TagHelper
{
    internal sealed class Columns(string[] labels)
    {
        public string[] Labels { get; } = labels;
        public int Index { get; set; }
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (context.Items.TryGetValue(typeof(MobileTableTagHelper), out var value) && value is string[] labels)
            context.Items[typeof(MobileTableRowTagHelper)] = new Columns(labels);
        output.Content.SetHtmlContent(await output.GetChildContentAsync());
    }
}

[HtmlTargetElement("td")]
public sealed class MobileTableCellTagHelper : TagHelper
{
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (!context.Items.TryGetValue(typeof(MobileTableRowTagHelper), out var value) ||
            value is not MobileTableRowTagHelper.Columns columns) return;

        var span = int.TryParse(output.Attributes["colspan"]?.Value?.ToString(), out var parsed) ? Math.Max(1, parsed) : 1;
        if (span == 1 && columns.Index < columns.Labels.Length)
        {
            if (!output.Attributes.ContainsName("data-label"))
                output.Attributes.SetAttribute("data-label", columns.Labels[columns.Index].Trim());
            var content = await output.GetChildContentAsync();
            output.Content.SetHtmlContent("<div class=\"mobile-table-value\">");
            output.Content.AppendHtml(content);
            output.Content.AppendHtml("</div>");
        }
        columns.Index += span;
    }
}
