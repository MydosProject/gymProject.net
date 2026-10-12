using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using NO23.Web.Infrastructure.Validation;

namespace NO23.Tests;

public class MoneyAmountModelBinderTests
{
    [Theory]
    [InlineData("tr-TR", "1234,56")]
    [InlineData("tr-TR", "1234.56")]
    [InlineData("tr-TR", "1.234,56")]
    [InlineData("en-US", "1234,56")]
    [InlineData("en-US", "1234.56")]
    [InlineData("en-US", "1.234,56")]
    public async Task BindsCommaDotAndTurkishGroupedMoneyWithoutInflation(string serverCulture, string input)
    {
        var context = BindingContext(serverCulture, input);

        await new MoneyAmountModelBinder().BindModelAsync(context);

        Assert.Equal(0, context.ModelState.ErrorCount);
        Assert.True(context.Result.IsModelSet);
        Assert.Equal(1234.56m, context.Result.Model);
    }

    [Theory]
    [InlineData("12.345")]
    [InlineData("1.2345")]
    [InlineData("12,345")]
    [InlineData("1,234.56")]
    [InlineData("1.23.4,56")]
    [InlineData("1,23,4")]
    [InlineData("invalid")]
    public async Task RejectsAmbiguousOrMalformedAmountsInsteadOfTreatingDecimalsAsThousands(string input)
    {
        var context = BindingContext("tr-TR", input);

        await new MoneyAmountModelBinder().BindModelAsync(context);

        Assert.Equal(1, context.ModelState.ErrorCount);
        Assert.False(context.Result.IsModelSet);
    }

    [Fact]
    public async Task EmptyAmount_BindsNullForCatalogTotalFallback()
    {
        var context = BindingContext("tr-TR", " ");

        await new MoneyAmountModelBinder().BindModelAsync(context);

        Assert.Equal(0, context.ModelState.ErrorCount);
        Assert.True(context.Result.IsModelSet);
        Assert.Null(context.Result.Model);
    }

    private static ModelBindingContext BindingContext(string culture, string input) =>
        DefaultModelBindingContext.CreateBindingContext(new ActionContext
        {
            HttpContext = new DefaultHttpContext(), RouteData = new(), ActionDescriptor = new()
        }, new FormValueProvider(BindingSource.Form,
            new FormCollection(new Dictionary<string, StringValues> { ["Payment.Amount"] = input }), CultureInfo.GetCultureInfo(culture)),
            new EmptyModelMetadataProvider().GetMetadataForType(typeof(decimal?)), null, "Payment.Amount");
}
