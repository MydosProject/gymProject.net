using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace NO23.Web.Infrastructure;

public sealed class ConcurrentUpdateFilter(ITempDataDictionaryFactory tempDataFactory) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not DbUpdateConcurrencyException) return;
        tempDataFactory.GetTempData(context.HttpContext)["ErrorMessage"] =
            "Kayıt veya ders bakiyesi başka bir işlemle değişti. Güncel bilgileri kontrol edip tekrar deneyin.";
        context.Result = new RedirectToActionResult("Index", context.RouteData.Values["controller"]?.ToString(),
            new { area = context.RouteData.Values["area"] });
        context.ExceptionHandled = true;
    }
}
