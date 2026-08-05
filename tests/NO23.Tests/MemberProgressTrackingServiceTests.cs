using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Member;

namespace NO23.Tests;

public class MemberProgressTrackingServiceTests
{
    [Fact]
    public async Task UpsertAsync_Fails_ForFutureDate()
    {
        await using var dbContext = CreateDbContext();
        var profile = await SeedMemberAsync(dbContext);
        var service = new MemberProgressTrackingService(dbContext);

        var result = await service.UpsertAsync(
            profile.ApplicationUserId,
            new MemberProgressEntryInputViewModel
            {
                EntryDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                CaloriesConsumed = 2000
            });

        Assert.False(result.Succeeded);
        Assert.Empty(dbContext.MemberProgressEntries);
    }

    [Fact]
    public async Task UpsertAsync_UpdatesExistingEntry_ForSameDate()
    {
        await using var dbContext = CreateDbContext();
        var profile = await SeedMemberAsync(dbContext);
        var service = new MemberProgressTrackingService(dbContext);
        var entryDate = DateOnly.FromDateTime(DateTime.Today);

        await service.UpsertAsync(
            profile.ApplicationUserId,
            new MemberProgressEntryInputViewModel
            {
                EntryDate = entryDate,
                CaloriesConsumed = 1800,
                BodyWeightKg = 82.4m
            });
        var result = await service.UpsertAsync(
            profile.ApplicationUserId,
            new MemberProgressEntryInputViewModel
            {
                EntryDate = entryDate,
                CaloriesConsumed = 2100,
                BodyWeightKg = 81.9m
            });

        Assert.True(result.Succeeded);
        var entry = await dbContext.MemberProgressEntries.SingleAsync();
        Assert.Equal(2100, entry.CaloriesConsumed);
        Assert.Equal(81.9m, entry.BodyWeightKg);
        Assert.NotNull(entry.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpsertAsync_ClearsMeasurementField_WhenValueIsRemoved()
    {
        await using var dbContext = CreateDbContext();
        var profile = await SeedMemberAsync(dbContext);
        var service = new MemberProgressTrackingService(dbContext);
        var entryDate = DateOnly.FromDateTime(DateTime.Today);

        await service.UpsertAsync(
            profile.ApplicationUserId,
            new MemberProgressEntryInputViewModel
            {
                EntryDate = entryDate,
                CaloriesConsumed = 1900,
                BodyWeightKg = 82.4m
            });
        var result = await service.UpsertAsync(
            profile.ApplicationUserId,
            new MemberProgressEntryInputViewModel
            {
                EntryDate = entryDate,
                CaloriesConsumed = 1900
            });

        Assert.True(result.Succeeded);
        var entry = await dbContext.MemberProgressEntries.SingleAsync();
        Assert.Equal(1900, entry.CaloriesConsumed);
        Assert.Null(entry.BodyWeightKg);
    }

    [Fact]
    public async Task UpsertAsync_RemovesChallengeProgress_WhenCaloriesAreCleared()
    {
        await using var dbContext = CreateDbContext();
        var profile = await SeedMemberAsync(dbContext);
        var entryDate = DateOnly.FromDateTime(DateTime.Today);
        await SeedChallengeParticipationAsync(dbContext, profile, entryDate);
        var service = new MemberProgressTrackingService(dbContext);

        await service.UpsertAsync(
            profile.ApplicationUserId,
            new MemberProgressEntryInputViewModel
            {
                EntryDate = entryDate,
                CaloriesConsumed = 2000,
                BodyWeightKg = 82.4m
            });
        var result = await service.UpsertAsync(
            profile.ApplicationUserId,
            new MemberProgressEntryInputViewModel
            {
                EntryDate = entryDate,
                BodyWeightKg = 82.4m
            });

        Assert.True(result.Succeeded);
        Assert.Empty(dbContext.ChallengeProgressEntries);
        var entry = await dbContext.MemberProgressEntries.SingleAsync();
        Assert.Null(entry.CaloriesConsumed);
        Assert.Equal(82.4m, entry.BodyWeightKg);
    }

    [Fact]
    public async Task UpsertAsync_DeletesExistingEntry_WhenAllValuesAreCleared()
    {
        await using var dbContext = CreateDbContext();
        var profile = await SeedMemberAsync(dbContext);
        var entryDate = DateOnly.FromDateTime(DateTime.Today);
        await SeedChallengeParticipationAsync(dbContext, profile, entryDate);
        var service = new MemberProgressTrackingService(dbContext);

        await service.UpsertAsync(
            profile.ApplicationUserId,
            new MemberProgressEntryInputViewModel
            {
                EntryDate = entryDate,
                CaloriesConsumed = 2000,
                BodyWeightKg = 82.4m
            });
        var result = await service.UpsertAsync(
            profile.ApplicationUserId,
            new MemberProgressEntryInputViewModel
            {
                EntryDate = entryDate
            });

        Assert.True(result.Succeeded);
        Assert.Empty(dbContext.MemberProgressEntries);
        Assert.Empty(dbContext.ChallengeProgressEntries);
    }

    [Fact]
    public async Task UpsertAsync_DoesNotCreateEntry_WhenNoValuesAreSubmittedForEmptyDate()
    {
        await using var dbContext = CreateDbContext();
        var profile = await SeedMemberAsync(dbContext);
        var service = new MemberProgressTrackingService(dbContext);

        var result = await service.UpsertAsync(
            profile.ApplicationUserId,
            new MemberProgressEntryInputViewModel
            {
                EntryDate = DateOnly.FromDateTime(DateTime.Today)
            });

        Assert.False(result.Succeeded);
        Assert.Empty(dbContext.MemberProgressEntries);
    }

    [Fact]
    public async Task UpsertAsync_SyncsCaloriesToJoinedChallengeProgress()
    {
        await using var dbContext = CreateDbContext();
        var profile = await SeedMemberAsync(dbContext);
        var entryDate = DateOnly.FromDateTime(DateTime.Today);
        var participation = await SeedChallengeParticipationAsync(dbContext, profile, entryDate);
        var service = new MemberProgressTrackingService(dbContext);

        var result = await service.UpsertAsync(
            profile.ApplicationUserId,
            new MemberProgressEntryInputViewModel
            {
                EntryDate = entryDate,
                CaloriesConsumed = 2000
            });

        Assert.True(result.Succeeded);
        var progressEntry = await dbContext.ChallengeProgressEntries.SingleAsync();
        Assert.Equal(participation.Id, progressEntry.CommunityChallengeParticipationId);
        Assert.Equal(entryDate, progressEntry.EntryDate);
        Assert.Equal(2000, progressEntry.CaloriesConsumed);
        Assert.True(progressEntry.IsCompliant);
    }

    [Fact]
    public async Task UpsertAsync_DoesNotSyncCaloriesToCompletedChallenge()
    {
        await using var dbContext = CreateDbContext();
        var profile = await SeedMemberAsync(dbContext);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var entryDate = today.AddDays(-2);
        await SeedChallengeParticipationAsync(
            dbContext,
            profile,
            entryDate,
            startsOn: entryDate.AddDays(-3),
            endsOn: today.AddDays(-1),
            status: CommunityChallengeStatus.Active);
        var service = new MemberProgressTrackingService(dbContext);

        var result = await service.UpsertAsync(
            profile.ApplicationUserId,
            new MemberProgressEntryInputViewModel
            {
                EntryDate = entryDate,
                CaloriesConsumed = 2000
            });

        Assert.True(result.Succeeded);
        Assert.Empty(dbContext.ChallengeProgressEntries);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<MemberProfile> SeedMemberAsync(ApplicationDbContext dbContext)
    {
        var package = new MembershipPackage
        {
            Code = MembershipPackageCode.Pro,
            Name = "Pro",
            Audience = "Aktif üyeler",
            Description = "Test paketi",
            IncludesCommunityMembership = true
        };
        var user = new ApplicationUser
        {
            UserName = $"member-{Guid.NewGuid()}@no23.test",
            Email = $"member-{Guid.NewGuid()}@no23.test",
            FirstName = "Test",
            LastName = "Member"
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

    private static async Task<CommunityChallengeParticipation> SeedChallengeParticipationAsync(
        ApplicationDbContext dbContext,
        MemberProfile profile,
        DateOnly entryDate,
        DateOnly? startsOn = null,
        DateOnly? endsOn = null,
        CommunityChallengeStatus status = CommunityChallengeStatus.Active)
    {
        var challenge = new CommunityChallenge
        {
            Title = "Test Challenge",
            Slug = $"test-challenge-{Guid.NewGuid()}",
            Summary = "Test summary",
            Description = "Test description",
            Goal = "Kalori hedefini tuttur.",
            TargetDailyCalories = 2000,
            CalorieTolerancePercent = 10,
            RequiredCompletionPercent = 80,
            StartsOn = startsOn ?? entryDate.AddDays(-2),
            EndsOn = endsOn ?? entryDate.AddDays(2),
            Status = status
        };
        var participation = new CommunityChallengeParticipation
        {
            CommunityChallenge = challenge,
            MemberProfileId = profile.Id,
            Status = CommunityChallengeParticipationStatus.Active
        };

        dbContext.CommunityChallengeParticipations.Add(participation);
        await dbContext.SaveChangesAsync();
        return participation;
    }
}
