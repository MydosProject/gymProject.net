using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using NO23.Web.ViewModels.Admin;
using NO23.Web.Infrastructure.Validation;

namespace NO23.Tests;

public class KitchenSubscriptionPackageValidationTests
{
    public static TheoryData<string, string> PriceFields => new()
    {
        { "tr-TR", "UnitPrice" }, { "tr-TR", "TwoMainMealsPrice" }, { "tr-TR", "ThreeMainMealsPrice" }, { "tr-TR", "DailyDeliveryFee" },
        { "en-US", "UnitPrice" }, { "en-US", "TwoMainMealsPrice" }, { "en-US", "ThreeMainMealsPrice" }, { "en-US", "DailyDeliveryFee" },
        { "de-DE", "UnitPrice" }, { "de-DE", "TwoMainMealsPrice" }, { "de-DE", "ThreeMainMealsPrice" }, { "de-DE", "DailyDeliveryFee" }
    };

    [Theory]
    [MemberData(nameof(PriceFields))]
    public void FormValidation_RendersAndChecksMoneyBoundariesRegardlessOfServerCulture(string cultureName, string field)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            var attribute = typeof(KitchenSubscriptionPackageFormViewModel).GetProperty(field)!.GetCustomAttribute<RangeAttribute>()!;
            Assert.Equal(typeof(MoneyAmountModelBinder), typeof(KitchenSubscriptionPackageFormViewModel)
                .GetProperty(field)!.GetCustomAttribute<ModelBinderAttribute>()!.BinderType);
            // This is the same adapter factory used by InputTagHelper while rendering
            // both create and edit forms. It used to throw before any input was posted.
            var adapter = new ValidationAttributeAdapterProvider().GetAttributeAdapter(attribute, null)!;
            var metadataProvider = new EmptyModelMetadataProvider();
            var metadata = metadataProvider.GetMetadataForProperty(typeof(KitchenSubscriptionPackageFormViewModel), field);
            var html = new Dictionary<string, string>();
            adapter.AddValidation(new ClientModelValidationContext(new ActionContext(new DefaultHttpContext(), new RouteData(),
                new ActionDescriptor()), metadata, metadataProvider, html));
            var minimum = field == "DailyDeliveryFee" ? 0m : 0.01m;
            Assert.Equal(minimum.ToString(CultureInfo.InvariantCulture), html["data-val-range-min"]);
            Assert.Equal("1000000", html["data-val-range-max"]);
            foreach (var amount in new[] { -1m, 0m, 0.009m, 0.01m, 1234.56m, 1000000m, 1000000.01m })
                Assert.Equal(amount >= minimum && amount <= 1000000m, attribute.IsValid(amount));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }
}
