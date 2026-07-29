using NO23.Web.Domain.Entities;
using NO23.Web.Services;

namespace NO23.Tests;

public class CommunityChallengeProgressCalculatorTests
{
    [Fact]
    public void GetCalorieRange_UsesTargetAndTolerancePercent()
    {
        var range = CommunityChallengeProgressCalculator.GetCalorieRange(
            targetDailyCalories: 2000,
            tolerancePercent: 10);

        Assert.Equal(1800, range.MinCalories);
        Assert.Equal(2200, range.MaxCalories);
    }

    [Theory]
    [InlineData(1799, false)]
    [InlineData(1800, true)]
    [InlineData(2000, true)]
    [InlineData(2200, true)]
    [InlineData(2201, false)]
    public void IsCalorieCompliant_ChecksInclusiveRange(
        int caloriesConsumed,
        bool expected)
    {
        var range = new ChallengeCalorieRange(1800, 2200);

        var isCompliant = CommunityChallengeProgressCalculator.IsCalorieCompliant(
            caloriesConsumed,
            range);

        Assert.Equal(expected, isCompliant);
    }

    [Fact]
    public void GetProgressStats_CompletesWhenRequiredPercentIsReached()
    {
        var entries = Enumerable.Range(0, 8)
            .Select(day => new ChallengeProgressEntry
            {
                EntryDate = new DateOnly(2026, 7, 1).AddDays(day),
                IsCompliant = true
            });

        var stats = CommunityChallengeProgressCalculator.GetProgressStats(
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 10),
            requiredCompletionPercent: 80,
            entries);

        Assert.Equal(10, stats.TotalDays);
        Assert.Equal(8, stats.LoggedDays);
        Assert.Equal(8, stats.CompliantDays);
        Assert.Equal(80, stats.ProgressPercent);
        Assert.True(stats.IsCompleted);
    }

    [Fact]
    public void GetProgressStats_DoesNotCompleteBelowRequiredPercent()
    {
        var entries = Enumerable.Range(0, 7)
            .Select(day => new ChallengeProgressEntry
            {
                EntryDate = new DateOnly(2026, 7, 1).AddDays(day),
                IsCompliant = true
            });

        var stats = CommunityChallengeProgressCalculator.GetProgressStats(
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 10),
            requiredCompletionPercent: 80,
            entries);

        Assert.Equal(70, stats.ProgressPercent);
        Assert.False(stats.IsCompleted);
    }
}
