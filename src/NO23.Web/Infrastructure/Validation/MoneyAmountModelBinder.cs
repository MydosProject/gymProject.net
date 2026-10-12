using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace NO23.Web.Infrastructure.Validation;

public sealed class MoneyAmountModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value = context.ValueProvider.GetValue(context.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, value);
        var text = value.FirstValue?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            context.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }
        var formatIsValid = Regex.IsMatch(text, @"^[+-]?\d+(?:[.,]\d{1,2})?$") ||
            Regex.IsMatch(text, @"^[+-]?\d{1,3}(?:\.\d{3})+,\d{1,2}$");
        var culture = text.Contains(',') ? CultureInfo.GetCultureInfo("tr-TR") : CultureInfo.InvariantCulture;
        if (formatIsValid && decimal.TryParse(text, NumberStyles.Number, culture, out var amount))
            context.Result = ModelBindingResult.Success(amount);
        else
            context.ModelState.TryAddModelError(context.ModelName, "Geçerli bir tutar girin; en fazla iki ondalık kullanın.");
        return Task.CompletedTask;
    }
}
