using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Tests;

public class LifecycleStatusTests
{
    [Fact]
    public void CommunityChallengeLifecycle_ReturnsUpcomingBeforeStart()
    {
        var today = new DateOnly(2026, 8, 5);

        var status = CommunityChallengeLifecycle.GetEffectiveStatus(
            CommunityChallengeStatus.Completed,
            today.AddDays(1),
            today.AddDays(10),
            today);

        Assert.Equal(CommunityChallengeStatus.Upcoming, status);
    }

    [Fact]
    public void CommunityChallengeLifecycle_ReturnsActiveInsideDateRange()
    {
        var today = new DateOnly(2026, 8, 5);

        var status = CommunityChallengeLifecycle.GetEffectiveStatus(
            CommunityChallengeStatus.Upcoming,
            today.AddDays(-1),
            today.AddDays(1),
            today);

        Assert.Equal(CommunityChallengeStatus.Active, status);
    }

    [Fact]
    public void CommunityChallengeLifecycle_ReturnsCompletedAfterEnd()
    {
        var today = new DateOnly(2026, 8, 5);

        var status = CommunityChallengeLifecycle.GetEffectiveStatus(
            CommunityChallengeStatus.Active,
            today.AddDays(-10),
            today.AddDays(-1),
            today);

        Assert.Equal(CommunityChallengeStatus.Completed, status);
    }

    [Fact]
    public void CommunityChallengeLifecycle_KeepsCancelledStatus()
    {
        var today = new DateOnly(2026, 8, 5);

        var status = CommunityChallengeLifecycle.GetEffectiveStatus(
            CommunityChallengeStatus.Cancelled,
            today.AddDays(-1),
            today.AddDays(1),
            today);

        Assert.Equal(CommunityChallengeStatus.Cancelled, status);
    }

    [Theory]
    [InlineData(CommunityChallengeStatus.Upcoming, true)]
    [InlineData(CommunityChallengeStatus.Active, true)]
    [InlineData(CommunityChallengeStatus.Completed, false)]
    [InlineData(CommunityChallengeStatus.Cancelled, false)]
    public void CommunityChallengeLifecycle_ChecksJoinOpenStatus(
        CommunityChallengeStatus status,
        bool expected)
    {
        Assert.Equal(expected, CommunityChallengeLifecycle.IsJoinOpen(status));
    }

    [Fact]
    public void ClassSessionLifecycle_ReturnsScheduledForFutureScheduledSession()
    {
        var nowUtc = new DateTime(2026, 8, 5, 9, 0, 0, DateTimeKind.Utc);

        var status = ClassSessionLifecycle.GetEffectiveStatus(
            ClassSessionStatus.Scheduled,
            nowUtc.AddHours(1),
            nowUtc);

        Assert.Equal(ClassSessionStatus.Scheduled, status);
    }

    [Fact]
    public void ClassSessionLifecycle_ReturnsCompletedForPastScheduledSession()
    {
        var nowUtc = new DateTime(2026, 8, 5, 9, 0, 0, DateTimeKind.Utc);

        var status = ClassSessionLifecycle.GetEffectiveStatus(
            ClassSessionStatus.Scheduled,
            nowUtc.AddMinutes(-1),
            nowUtc);

        Assert.Equal(ClassSessionStatus.Completed, status);
    }

    [Theory]
    [InlineData(ClassSessionStatus.Cancelled)]
    [InlineData(ClassSessionStatus.Completed)]
    public void ClassSessionLifecycle_KeepsTerminalStatus(ClassSessionStatus storedStatus)
    {
        var nowUtc = new DateTime(2026, 8, 5, 9, 0, 0, DateTimeKind.Utc);

        var status = ClassSessionLifecycle.GetEffectiveStatus(
            storedStatus,
            nowUtc.AddHours(1),
            nowUtc);

        Assert.Equal(storedStatus, status);
    }

    [Fact]
    public void CommunityEventLifecycle_ReturnsScheduledBeforeStartWhenEndIsMissing()
    {
        var nowUtc = new DateTime(2026, 8, 5, 9, 0, 0, DateTimeKind.Utc);

        var status = CommunityEventLifecycle.GetEffectiveStatus(
            CommunityEventStatus.Completed,
            nowUtc.AddHours(1),
            null,
            nowUtc);

        Assert.Equal(CommunityEventStatus.Scheduled, status);
    }

    [Fact]
    public void CommunityEventLifecycle_ReturnsCompletedAfterStartWhenEndIsMissing()
    {
        var nowUtc = new DateTime(2026, 8, 5, 9, 0, 0, DateTimeKind.Utc);

        var status = CommunityEventLifecycle.GetEffectiveStatus(
            CommunityEventStatus.Scheduled,
            nowUtc.AddMinutes(-1),
            null,
            nowUtc);

        Assert.Equal(CommunityEventStatus.Completed, status);
    }

    [Fact]
    public void CommunityEventLifecycle_KeepsScheduledUntilEndWhenEndExists()
    {
        var nowUtc = new DateTime(2026, 8, 5, 9, 0, 0, DateTimeKind.Utc);

        var status = CommunityEventLifecycle.GetEffectiveStatus(
            CommunityEventStatus.Completed,
            nowUtc.AddHours(-1),
            nowUtc.AddHours(1),
            nowUtc);

        Assert.Equal(CommunityEventStatus.Scheduled, status);
    }

    [Fact]
    public void CommunityEventLifecycle_ReturnsCompletedAfterEnd()
    {
        var nowUtc = new DateTime(2026, 8, 5, 9, 0, 0, DateTimeKind.Utc);

        var status = CommunityEventLifecycle.GetEffectiveStatus(
            CommunityEventStatus.Scheduled,
            nowUtc.AddHours(-2),
            nowUtc.AddHours(-1),
            nowUtc);

        Assert.Equal(CommunityEventStatus.Completed, status);
    }

    [Fact]
    public void CommunityEventLifecycle_KeepsCancelledStatus()
    {
        var nowUtc = new DateTime(2026, 8, 5, 9, 0, 0, DateTimeKind.Utc);

        var status = CommunityEventLifecycle.GetEffectiveStatus(
            CommunityEventStatus.Cancelled,
            nowUtc.AddHours(-1),
            nowUtc.AddHours(1),
            nowUtc);

        Assert.Equal(CommunityEventStatus.Cancelled, status);
    }
}
