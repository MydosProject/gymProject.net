using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Tests;

public class CommunityChallengeProgressServiceTests
{
    [Theory]
    [InlineData(-1, 7, true)]
    [InlineData(1, 7, true)]
    [InlineData(-10, -1, false)]
    public async Task JoinAsync_AllowsOnlyUpcomingOrActiveEffectiveChallenges(
        int startsOffset,
        int endsOffset,
        bool expectedSucceeded)
    {
        await using var dbContext = CreateDbContext();
        var profile = await SeedCommunityMemberAsync(dbContext);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var challenge = await SeedChallengeAsync(
            dbContext,
            startsOn: today.AddDays(startsOffset),
            endsOn: today.AddDays(endsOffset),
            storedStatus: CommunityChallengeStatus.Completed);
        var service = new CommunityChallengeProgressService(dbContext);

        var result = await service.JoinAsync(profile.ApplicationUserId, challenge.Slug);

        Assert.Equal(expectedSucceeded, result.Succeeded);
    }

    [Fact]
    public async Task UpsertDailyCalories_AcceptsOnlyEffectiveActiveChallenge()
    {
        await using var dbContext = CreateDbContext();
        var profile = await SeedCommunityMemberAsync(dbContext);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var challenge = await SeedChallengeAsync(
            dbContext,
            startsOn: today.AddDays(1),
            endsOn: today.AddDays(5),
            storedStatus: CommunityChallengeStatus.Active);
        var participation = new CommunityChallengeParticipation
        {
            CommunityChallengeId = challenge.Id,
            MemberProfileId = profile.Id
        };
        dbContext.CommunityChallengeParticipations.Add(participation);
        await dbContext.SaveChangesAsync();
        var service = new CommunityChallengeProgressService(dbContext);

        var result = await service.UpsertDailyCaloriesAsync(
            profile.ApplicationUserId,
            new ChallengeCalorieLogRequest(participation.Id, today, 2000));

        Assert.False(result.Succeeded);
        Assert.Empty(dbContext.ChallengeProgressEntries);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<MemberProfile> SeedCommunityMemberAsync(ApplicationDbContext dbContext)
    {
        var package = new MembershipPackage
        {
            Code = MembershipPackageCode.Pro,
            Name = "Pro",
            Audience = "Aktif uyeler",
            Description = "Test paketi",
            IncludesCommunityMembership = true
        };
        var user = new ApplicationUser
        {
            UserName = $"member-{Guid.NewGuid()}@no23.test",
            Email = $"member-{Guid.NewGuid()}@no23.test"
        };
        var profile = new MemberProfile
        {
            ApplicationUser = user,
            MembershipPackage = package,
            RemainingClassCredits = 4
        };

        dbContext.MemberProfiles.Add(profile);
        await dbContext.SaveChangesAsync();
        return profile;
    }

    private static async Task<CommunityChallenge> SeedChallengeAsync(
        ApplicationDbContext dbContext,
        DateOnly startsOn,
        DateOnly endsOn,
        CommunityChallengeStatus storedStatus)
    {
        var challenge = new CommunityChallenge
        {
            Title = "Lifecycle Challenge",
            Slug = $"lifecycle-challenge-{Guid.NewGuid()}",
            Summary = "Summary",
            Description = "Description",
            Goal = "Goal",
            TargetDailyCalories = 2000,
            CalorieTolerancePercent = 10,
            RequiredCompletionPercent = 80,
            StartsOn = startsOn,
            EndsOn = endsOn,
            Status = storedStatus
        };

        dbContext.CommunityChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();
        return challenge;
    }
}
