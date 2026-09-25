using NO23.Web.Infrastructure.Identity;

namespace NO23.Tests;

public class MemberLoginNameTests
{
    [Fact]
    public void BuildUserName_IsSameForEquivalentTurkishNames()
    {
        var first = MemberLoginName.BuildUserName("ışıl", "öz");
        var second = MemberLoginName.BuildUserName("  IŞIL ", " ÖZ  ");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Matches_IgnoresCaseAndExtraWhiteSpace()
    {
        Assert.True(MemberLoginName.Matches(
            "  Ahmet   Yılmaz ",
            "ahmet",
            "yılmaz"));
    }

    [Fact]
    public void BuildUserName_ProducesDifferentValuesForDifferentNames()
    {
        Assert.NotEqual(
            MemberLoginName.BuildUserName("Ahmet", "Yılmaz"),
            MemberLoginName.BuildUserName("Mehmet", "Yılmaz"));
    }
}
