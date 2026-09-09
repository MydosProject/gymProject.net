using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace NO23.Web.Services;

// Measurements accept either decimal separator, never thousands separators.
public sealed class MeasurementDecimalBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value = context.ValueProvider.GetValue(context.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, value);
        var raw = value.FirstValue?.Trim();
        if (string.IsNullOrEmpty(raw)) context.Result = ModelBindingResult.Success(null);
        else if (decimal.TryParse(raw.Replace(',', '.'), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                     CultureInfo.InvariantCulture, out var number))
            context.Result = ModelBindingResult.Success(number);
        else context.ModelState.TryAddModelError(context.ModelName, "Geçerli bir ölçüm gir (örnek: 82,5).");
        return Task.CompletedTask;
    }
}
