using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using NO23.Web.Services;

namespace NO23.Tests;

public class MeasurementDecimalBinderTests
{
    [Theory]
    [InlineData("tr-TR", "82,5")]
    [InlineData("tr-TR", "82.5")]
    [InlineData("en-US", "82,5")]
    [InlineData("en-US", "82.5")]
    public async Task DecimalSeparator_IsIndependentOfServerCulture(string culture, string input)
    {
        var context = Context(culture, input);
        await new MeasurementDecimalBinder().BindModelAsync(context);
        Assert.Equal(0, context.ModelState.ErrorCount);
        Assert.Equal(82.5m, context.Result.Model);
    }

    [Theory]
    [InlineData("82,5.0")]
    [InlineData("invalid")]
    public async Task InvalidInput_IsRejected(string input)
    {
        var context = Context("en-US", input);
        await new MeasurementDecimalBinder().BindModelAsync(context);
        Assert.Equal(1, context.ModelState.ErrorCount);
    }

    private static ModelBindingContext Context(string culture, string input) =>
        DefaultModelBindingContext.CreateBindingContext(new ActionContext { HttpContext = new DefaultHttpContext(),
            RouteData = new(), ActionDescriptor = new() },
            new FormValueProvider(BindingSource.Form, new FormCollection(new Dictionary<string, StringValues> { ["Weight"] = input }), CultureInfo.GetCultureInfo(culture)),
            new EmptyModelMetadataProvider().GetMetadataForType(typeof(decimal?)), null, "Weight");
}
