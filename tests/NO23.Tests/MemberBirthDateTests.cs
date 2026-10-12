using System.ComponentModel.DataAnnotations;
using NO23.Web.Areas.Identity.Pages.Account;
using NO23.Web.Infrastructure.Validation;
using NO23.Web.Services;
using NO23.Web.ViewModels.Admin;

namespace NO23.Tests;

public class MemberBirthDateTests
{
    [Theory]
    [InlineData(2028, 2, 29, true)]
    [InlineData(2028, 2, 28, true)]
    [InlineData(1908, 2, 29, true)]
    [InlineData(1908, 2, 28, false)]
    [InlineData(2028, 3, 1, false)]
    public void DateValidation_UsesInclusiveCurrentDayAnd120YearBoundaries(int year, int month, int day, bool valid)
    {
        var error = BirthDateValidation.GetError(new DateOnly(year, month, day), today: new DateOnly(2028, 2, 29));

        Assert.Equal(valid, error is null);
        if (!valid) Assert.Equal(BirthDateValidation.InvalidDateMessage, error);
    }

    [Fact]
    public void DateValidation_RequiresNewMemberDate_AndAllowsMissingLegacyDate()
    {
        Assert.Equal(BirthDateValidation.RequiredMessage, BirthDateValidation.GetError(null, required: true));
        Assert.Null(BirthDateValidation.GetError(null));
    }

    [Fact]
    public void NewRegistrationAndManualCreateModels_RequireBirthDate_WhileLegacyEditAllowsMissingDate()
    {
        Assert.Contains(Validate(new RegisterModel.InputModel()), result => result.MemberNames.Contains("BirthDate"));
        Assert.Contains(Validate(new MemberCreateViewModel()), result => result.MemberNames.Contains("BirthDate"));
        Assert.DoesNotContain(Validate(new MemberEditViewModel()), result => result.MemberNames.Contains("BirthDate"));
    }

    [Fact]
    public void BirthDateAttribute_RejectsFutureAndImplausibleDatesAcrossAllEditableModels()
    {
        var today = DateOnly.FromDateTime(ClubTime.Now);
        foreach (var date in new[] { today.AddDays(1), today.AddYears(-120).AddDays(-1) })
        {
            Assert.Contains(Validate(new RegisterModel.InputModel { BirthDate = date }), result => result.MemberNames.Contains("BirthDate"));
            Assert.Contains(Validate(new MemberCreateViewModel { BirthDate = date }), result => result.MemberNames.Contains("BirthDate"));
            Assert.Contains(Validate(new MemberEditViewModel { BirthDate = date }), result => result.MemberNames.Contains("BirthDate"));
        }
    }

    private static IReadOnlyList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
