using NO23.Web.Infrastructure.Validation;

namespace NO23.Tests;

public class TurkishNationalIdentityNumberTests
{
    [Theory]
    [InlineData("10000000146")]
    [InlineData(" 10000000146 ")]
    public void IsValid_AcceptsValidNumbers(string value)
    {
        Assert.True(TurkishNationalIdentityNumber.IsValid(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("01234567890")]
    [InlineData("10000000145")]
    [InlineData("1000000014A")]
    [InlineData("123456789")]
    public void IsValid_RejectsInvalidNumbers(string? value)
    {
        Assert.False(TurkishNationalIdentityNumber.IsValid(value));
    }

    [Fact]
    public void Mask_OnlyShowsLastFourDigits()
    {
        Assert.Equal("*******0146", TurkishNationalIdentityNumber.Mask("10000000146"));
    }
}
