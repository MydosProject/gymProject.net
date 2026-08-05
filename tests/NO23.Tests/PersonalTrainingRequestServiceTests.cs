using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Member;

namespace NO23.Tests;

public class PersonalTrainingRequestServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesRequest_WhenPackageIncludesPersonalTrainingSupport()
    {
        await using var dbContext = CreateDbContext();
        var profile = await SeedMemberAsync(dbContext, includesPersonalTrainingSupport: true);
        var trainer = await SeedTrainerAsync(dbContext);
        var service = new PersonalTrainingRequestService(dbContext);

        var result = await service.CreateAsync(profile.ApplicationUserId, BuildInput(trainer.Id));

        Assert.True(result.Succeeded);
        var request = await dbContext.PersonalTrainingRequests.SingleAsync();
        Assert.Equal(profile.Id, request.MemberProfileId);
        Assert.Equal(trainer.Id, request.TrainerId);
        Assert.Equal(PersonalTrainingRequestStatus.Pending, request.Status);
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenPackageDoesNotIncludePersonalTrainingSupport()
    {
        await using var dbContext = CreateDbContext();
        var profile = await SeedMemberAsync(dbContext, includesPersonalTrainingSupport: false);
        var trainer = await SeedTrainerAsync(dbContext);
        var service = new PersonalTrainingRequestService(dbContext);

        var result = await service.CreateAsync(profile.ApplicationUserId, BuildInput(trainer.Id));

        Assert.False(result.Succeeded);
        Assert.Empty(dbContext.PersonalTrainingRequests);
    }

    [Fact]
    public async Task CreateAsync_Fails_ForInactiveTrainer()
    {
        await using var dbContext = CreateDbContext();
        var profile = await SeedMemberAsync(dbContext, includesPersonalTrainingSupport: true);
        var trainer = await SeedTrainerAsync(dbContext, isActive: false);
        var service = new PersonalTrainingRequestService(dbContext);

        var result = await service.CreateAsync(profile.ApplicationUserId, BuildInput(trainer.Id));

        Assert.False(result.Succeeded);
        Assert.Empty(dbContext.PersonalTrainingRequests);
    }

    [Fact]
    public async Task CreateAsync_Fails_ForPastPreferredDate()
    {
        await using var dbContext = CreateDbContext();
        var profile = await SeedMemberAsync(dbContext, includesPersonalTrainingSupport: true);
        var trainer = await SeedTrainerAsync(dbContext);
        var service = new PersonalTrainingRequestService(dbContext);

        var result = await service.CreateAsync(
            profile.ApplicationUserId,
            BuildInput(trainer.Id, DateOnly.FromDateTime(DateTime.Today.AddDays(-1))));

        Assert.False(result.Succeeded);
        Assert.Empty(dbContext.PersonalTrainingRequests);
    }

    [Fact]
    public async Task CreateAsync_Fails_ForDuplicatePendingRequestOnSameTrainerAndDate()
    {
        await using var dbContext = CreateDbContext();
        var profile = await SeedMemberAsync(dbContext, includesPersonalTrainingSupport: true);
        var trainer = await SeedTrainerAsync(dbContext);
        var service = new PersonalTrainingRequestService(dbContext);
        var input = BuildInput(trainer.Id);

        var firstResult = await service.CreateAsync(profile.ApplicationUserId, input);
        var secondResult = await service.CreateAsync(profile.ApplicationUserId, input);

        Assert.True(firstResult.Succeeded);
        Assert.False(secondResult.Succeeded);
        Assert.Equal(1, await dbContext.PersonalTrainingRequests.CountAsync());
    }

    [Fact]
    public async Task UpdateByAdminAsync_SchedulesAndCompletesPendingRequest()
    {
        await using var dbContext = CreateDbContext();
        var profile = await SeedMemberAsync(dbContext, includesPersonalTrainingSupport: true);
        var trainer = await SeedTrainerAsync(dbContext);
        var request = await SeedPersonalTrainingRequestAsync(dbContext, profile, trainer);
        var service = new PersonalTrainingRequestService(dbContext);
        var scheduledAtLocal = DateTime.Now.AddDays(2).Date.AddHours(14);

        var scheduleResult = await service.UpdateByAdminAsync(
            request.Id,
            PersonalTrainingRequestStatus.Scheduled,
            scheduledAtLocal,
            "Planlandı.");
        var completeResult = await service.UpdateByAdminAsync(
            request.Id,
            PersonalTrainingRequestStatus.Completed,
            scheduledAtLocal,
            "Tamamlandı.");

        Assert.True(scheduleResult.Succeeded);
        Assert.True(completeResult.Succeeded);
        var updatedRequest = await dbContext.PersonalTrainingRequests.SingleAsync();
        Assert.Equal(PersonalTrainingRequestStatus.Completed, updatedRequest.Status);
        Assert.NotNull(updatedRequest.ScheduledAtUtc);
    }

    [Fact]
    public async Task UpdateByAdminAsync_RejectsPendingRequest()
    {
        await using var dbContext = CreateDbContext();
        var profile = await SeedMemberAsync(dbContext, includesPersonalTrainingSupport: true);
        var trainer = await SeedTrainerAsync(dbContext);
        var request = await SeedPersonalTrainingRequestAsync(dbContext, profile, trainer);
        var service = new PersonalTrainingRequestService(dbContext);

        var result = await service.UpdateByAdminAsync(
            request.Id,
            PersonalTrainingRequestStatus.Rejected,
            null,
            "Uygun saat bulunamadı.");

        Assert.True(result.Succeeded);
        var updatedRequest = await dbContext.PersonalTrainingRequests.SingleAsync();
        Assert.Equal(PersonalTrainingRequestStatus.Rejected, updatedRequest.Status);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<MemberProfile> SeedMemberAsync(
        ApplicationDbContext dbContext,
        bool includesPersonalTrainingSupport)
    {
        var package = new MembershipPackage
        {
            Code = MembershipPackageCode.Pro,
            Name = "Pro",
            Audience = "Aktif üyeler",
            Description = "Test paketi",
            IncludesPersonalTrainingSupport = includesPersonalTrainingSupport
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

    private static async Task<Trainer> SeedTrainerAsync(
        ApplicationDbContext dbContext,
        bool isActive = true)
    {
        var trainer = new Trainer
        {
            FirstName = "Test",
            LastName = "Trainer",
            Specialty = "Strength",
            IsActive = isActive
        };

        dbContext.Trainers.Add(trainer);
        await dbContext.SaveChangesAsync();
        return trainer;
    }

    private static async Task<PersonalTrainingRequest> SeedPersonalTrainingRequestAsync(
        ApplicationDbContext dbContext,
        MemberProfile profile,
        Trainer trainer)
    {
        var request = new PersonalTrainingRequest
        {
            MemberProfileId = profile.Id,
            TrainerId = trainer.Id,
            PreferredDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            PreferredTimeWindow = PersonalTrainingRequestService.PreferredTimeWindows[0]
        };

        dbContext.PersonalTrainingRequests.Add(request);
        await dbContext.SaveChangesAsync();
        return request;
    }

    private static PersonalTrainingRequestInputViewModel BuildInput(
        int trainerId,
        DateOnly? preferredDate = null)
    {
        return new PersonalTrainingRequestInputViewModel
        {
            TrainerId = trainerId,
            PreferredDate = preferredDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            PreferredTimeWindow = PersonalTrainingRequestService.PreferredTimeWindows[0],
            GoalNote = "Duruş ve kuvvet çalışmak istiyorum."
        };
    }
}
