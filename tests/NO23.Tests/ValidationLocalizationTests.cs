using System.ComponentModel.DataAnnotations;
using NO23.Web.Infrastructure.Validation;

namespace NO23.Tests;

public class ValidationLocalizationTests
{
    [Fact]
    public void ApplyDefaultMessage_LocalizesRangeAttribute()
    {
        var attribute = new RangeAttribute(120, 230);

        TurkishValidationMetadataProvider.ApplyDefaultMessage(attribute);

        Assert.Equal(
            "Boy 120 ile 230 arasında olmalıdır.",
            attribute.FormatErrorMessage("Boy"));
    }

    [Fact]
    public void ApplyDefaultMessage_LocalizesStringLengthAttribute()
    {
        var attribute = new StringLengthAttribute(80);

        TurkishValidationMetadataProvider.ApplyDefaultMessage(attribute);

        Assert.Equal(
            "Ad en fazla 80 karakter olabilir.",
            attribute.FormatErrorMessage("Ad"));
    }

    [Fact]
    public void ApplyDefaultMessage_DoesNotOverrideExplicitMessage()
    {
        var attribute = new RequiredAttribute
        {
            ErrorMessage = "Özel mesaj."
        };

        TurkishValidationMetadataProvider.ApplyDefaultMessage(attribute);

        Assert.Equal(
            "Özel mesaj.",
            attribute.FormatErrorMessage("Ad"));
    }

    [Fact]
    public void TurkishIdentityErrorDescriber_ReturnsTurkishPasswordMessages()
    {
        var describer = new TurkishIdentityErrorDescriber();

        Assert.Equal(
            "Şifre en az 6 karakter olmalıdır.",
            describer.PasswordTooShort(6).Description);
        Assert.Equal(
            "Şifrede en az bir rakam bulunmalıdır.",
            describer.PasswordRequiresDigit().Description);
    }

    [Fact]
    public void TurkishIdentityErrorDescriber_ReturnsTurkishDuplicateEmailMessage()
    {
        var describer = new TurkishIdentityErrorDescriber();

        Assert.Equal(
            "'uye@no23.test' e-posta adresi zaten kullanılıyor.",
            describer.DuplicateEmail("uye@no23.test").Description);
    }
}
