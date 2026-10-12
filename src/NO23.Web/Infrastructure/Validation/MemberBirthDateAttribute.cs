using System.ComponentModel.DataAnnotations;
using NO23.Web.Services;

namespace NO23.Web.Infrastructure.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MemberBirthDateAttribute : ValidationAttribute
{
    public MemberBirthDateAttribute() => ErrorMessage = BirthDateValidation.InvalidDateMessage;

    public override bool IsValid(object? value) => value is null ||
        value is DateOnly date && BirthDateValidation.GetError(date) is null;
}

public static class BirthDateValidation
{
    public const string RequiredMessage = "Doğum tarihi alanı zorunludur.";
    public const string InvalidDateMessage = "Doğum tarihi bugünden ileri veya 120 yıldan eski olamaz.";

    public static string? GetError(DateOnly? value, bool required = false, DateOnly? today = null)
    {
        if (value is null)
            return required ? RequiredMessage : null;

        var clubToday = today ?? DateOnly.FromDateTime(ClubTime.Now);
        return value > clubToday || value < clubToday.AddYears(-120)
            ? InvalidDateMessage
            : null;
    }
}
