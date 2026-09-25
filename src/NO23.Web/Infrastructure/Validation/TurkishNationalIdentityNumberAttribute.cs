using System.ComponentModel.DataAnnotations;

namespace NO23.Web.Infrastructure.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class TurkishNationalIdentityNumberAttribute : ValidationAttribute
{
    public TurkishNationalIdentityNumberAttribute()
    {
        ErrorMessage = "Geçerli bir TC Kimlik No girmelisin.";
    }

    public override bool IsValid(object? value)
    {
        return value is null ||
               value is string text &&
               TurkishNationalIdentityNumber.IsValid(text);
    }
}

public static class TurkishNationalIdentityNumber
{
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var text = value.Trim();
        if (text.Length != 11 || text[0] == '0' || text.Any(character => !char.IsAsciiDigit(character)))
        {
            return false;
        }

        Span<int> digits = stackalloc int[11];
        for (var index = 0; index < text.Length; index++)
        {
            digits[index] = text[index] - '0';
        }

        var oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        var evenSum = digits[1] + digits[3] + digits[5] + digits[7];
        var tenthDigit = ((oddSum * 7) - evenSum) % 10;
        if (tenthDigit < 0)
        {
            tenthDigit += 10;
        }

        var firstTenSum = 0;
        for (var index = 0; index < 10; index++)
        {
            firstTenSum += digits[index];
        }

        return digits[9] == tenthDigit && digits[10] == firstTenSum % 10;
    }

    public static string Mask(string? value)
    {
        var text = value?.Trim();
        return string.IsNullOrWhiteSpace(text) || text.Length < 4
            ? "Tanımlanmamış"
            : $"*******{text[^4..]}";
    }
}
