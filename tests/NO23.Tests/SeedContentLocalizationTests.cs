using NO23.Web.Data.Seed;

namespace NO23.Tests;

public class SeedContentLocalizationTests
{
    private static readonly string[] MojibakeMarkers =
    [
        "Ã",
        "Ä",
        "Å",
        "�"
    ];

    [Fact]
    public void ShopProductSeed_DoesNotContainMojibakeOrAsciiOnlyTurkishPlaceholders()
    {
        var values = ShopProductSeed.Defaults.SelectMany(product =>
            new[]
            {
                product.Name,
                product.Description,
                product.Category,
                product.Tags
            });

        AssertCleanContent(values);
        Assert.Contains(ShopProductSeed.Defaults, product => product.Name == "Direnç Bandı Seti");
        Assert.Contains(ShopProductSeed.Defaults, product => product.Category == "Aksesuar");
    }

    [Fact]
    public void CommunityContentSeed_DoesNotContainMojibakeOrAsciiOnlyTurkishPlaceholders()
    {
        var eventValues = CommunityContentSeed.Events.SelectMany(item =>
            new[]
            {
                item.Title,
                item.Summary,
                item.Description,
                item.Location
            });

        var challengeValues = CommunityContentSeed.Challenges.SelectMany(item =>
            new[]
            {
                item.Title,
                item.Summary,
                item.Description,
                item.Goal,
                item.Reward
            });

        var blogValues = CommunityContentSeed.BlogPosts.SelectMany(item =>
            new[]
            {
                item.Title,
                item.Summary,
                item.Content,
                item.Category,
                item.Tags
            });

        var storyValues = CommunityContentSeed.SuccessStories.SelectMany(item =>
            new[]
            {
                item.MemberName,
                item.Title,
                item.Summary,
                item.Story,
                item.AchievementMetric
            });

        AssertCleanContent(eventValues
            .Concat(challengeValues)
            .Concat(blogValues)
            .Concat(storyValues));
        Assert.Contains(CommunityContentSeed.BlogPosts, post => post.Title == "Protein Hedefini Gün İçine Yaymak");
        Assert.Contains(CommunityContentSeed.SuccessStories, story => story.MemberName == "Ayşe K.");
    }

    [Fact]
    public void KitchenMenuItemSeed_DoesNotContainMojibake()
    {
        var values = KitchenMenuItemSeed.Defaults.SelectMany(item =>
            new[]
            {
                item.Name,
                item.Description,
                item.Ingredients,
                item.Allergens,
                item.Tags
            });

        AssertCleanContent(values);
    }

    private static void AssertCleanContent(IEnumerable<string?> values)
    {
        foreach (var value in values.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            foreach (var marker in MojibakeMarkers)
            {
                Assert.DoesNotContain(marker, value, StringComparison.Ordinal);
            }
        }
    }
}
