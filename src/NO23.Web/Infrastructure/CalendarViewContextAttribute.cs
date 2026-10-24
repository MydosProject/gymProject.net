using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NO23.Web.Infrastructure;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CalendarViewContextAttribute : ActionFilterAttribute
{
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is not RedirectToActionResult redirect || !context.HttpContext.Request.HasFormContentType) return;
        if (!string.Equals(redirect.ActionName, "Index", StringComparison.OrdinalIgnoreCase) ||
            !(string.Equals(redirect.ControllerName, "Calendar", StringComparison.OrdinalIgnoreCase) ||
              (redirect.ControllerName is null && string.Equals(context.RouteData.Values["controller"]?.ToString(), "Calendar", StringComparison.OrdinalIgnoreCase)))) return;
        var form = context.HttpContext.Request.Form;
        var mode = form["CalendarView"].ToString();
        if (mode is not ("day" or "week") || !DateTime.TryParseExact(form["CalendarDate"], "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date) || date.Year is < 1900 or > 2100) return;
        redirect.RouteValues ??= new();
        redirect.RouteValues["view"] = mode;
        redirect.RouteValues["date"] = date.ToString("yyyy-MM-dd");
    }
}
