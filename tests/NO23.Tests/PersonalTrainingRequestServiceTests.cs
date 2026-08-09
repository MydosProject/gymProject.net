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

        var conversation =
        await dbContext.TrainerConversations
        .SingleAsync();

        Assert.Equal(
            profile.Id,
            conversation.MemberProfileId);

        Assert.Equal(
            trainer.Id,
            conversation.TrainerId);
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
    public async Task CreateAsync_Fails_WhenPendingRequestExistsForSameTrainer()
    {
        await using var dbContext = CreateDbContext();

        var profile = await SeedMemberAsync(
            dbContext,
            includesPersonalTrainingSupport: true);

        var trainer = await SeedTrainerAsync(dbContext);

        var service =
            new PersonalTrainingRequestService(dbContext);

        var firstResult =
            await service.CreateAsync(
                profile.ApplicationUserId,
                BuildInput(
                    trainer.Id,
                    DateOnly.FromDateTime(
                        DateTime.Today.AddDays(1))));

        var secondResult =
            await service.CreateAsync(
                profile.ApplicationUserId,
                BuildInput(
                    trainer.Id,
                    DateOnly.FromDateTime(
                        DateTime.Today.AddDays(5))));

        Assert.True(firstResult.Succeeded);
        Assert.False(secondResult.Succeeded);

        Assert.Equal(
            1,
            await dbContext.PersonalTrainingRequests
                .CountAsync());
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenScheduledRequestExistsForSameTrainer()
    {
        await using var dbContext = CreateDbContext();

        var profile = await SeedMemberAsync(
            dbContext,
            includesPersonalTrainingSupport: true);

        var trainer = await SeedTrainerAsync(dbContext);

        var request =
            await SeedPersonalTrainingRequestAsync(
                dbContext,
                profile,
                trainer);

        request.Status =
            PersonalTrainingRequestStatus.Scheduled;

        request.ScheduledAtUtc =
            DateTime.UtcNow.AddDays(2);

        await dbContext.SaveChangesAsync();

        var service =
            new PersonalTrainingRequestService(dbContext);

        var result =
            await service.CreateAsync(
                profile.ApplicationUserId,
                BuildInput(
                    trainer.Id,
                    DateOnly.FromDateTime(
                        DateTime.Today.AddDays(5))));

        Assert.False(result.Succeeded);

        Assert.Equal(
            1,
            await dbContext.PersonalTrainingRequests
                .CountAsync());
    }

    [Fact]
    public async Task CreateAsync_AllowsNewRequest_WhenPreviousRequestIsCancelled()
    {
        await using var dbContext = CreateDbContext();

        var profile = await SeedMemberAsync(
            dbContext,
            includesPersonalTrainingSupport: true);

        var trainer = await SeedTrainerAsync(dbContext);

        var previousRequest =
            await SeedPersonalTrainingRequestAsync(
                dbContext,
                profile,
                trainer);

        previousRequest.Status =
            PersonalTrainingRequestStatus.Cancelled;

        previousRequest.CancelledAtUtc =
            DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        var service =
            new PersonalTrainingRequestService(dbContext);

        var result =
            await service.CreateAsync(
                profile.ApplicationUserId,
                BuildInput(
                    trainer.Id,
                    DateOnly.FromDateTime(
                        DateTime.Today.AddDays(4))));

        Assert.True(result.Succeeded);

        Assert.Equal(
            2,
            await dbContext.PersonalTrainingRequests
                .CountAsync());

        Assert.Equal(
        1,
        await dbContext.TrainerConversations
        .CountAsync());
    }

    [Fact]
    public async Task CancelByMemberAsync_CancelsFutureScheduledRequest()
    {
        await using var dbContext = CreateDbContext();

        var profile = await SeedMemberAsync(
            dbContext,
            includesPersonalTrainingSupport: true);

        var trainer = await SeedTrainerAsync(dbContext);

        var request =
            await SeedPersonalTrainingRequestAsync(
                dbContext,
                profile,
                trainer);

        request.Status =
            PersonalTrainingRequestStatus.Scheduled;

        request.ScheduledAtUtc =
            DateTime.UtcNow.AddDays(1);

        await dbContext.SaveChangesAsync();

        var service =
            new PersonalTrainingRequestService(dbContext);

        var result =
            await service.CancelByMemberAsync(
                profile.ApplicationUserId,
                request.Id);

        Assert.True(result.Succeeded);

        Assert.Equal(
            PersonalTrainingRequestStatus.Cancelled,
            request.Status);

        Assert.NotNull(request.CancelledAtUtc);
    }

    [Fact]
    public async Task CancelByMemberAsync_Fails_WhenScheduledRequestHasStarted()
    {
        await using var dbContext = CreateDbContext();

        var profile = await SeedMemberAsync(
            dbContext,
            includesPersonalTrainingSupport: true);

        var trainer = await SeedTrainerAsync(dbContext);

        var request =
            await SeedPersonalTrainingRequestAsync(
                dbContext,
                profile,
                trainer);

        request.Status =
            PersonalTrainingRequestStatus.Scheduled;

        request.ScheduledAtUtc =
            DateTime.UtcNow.AddMinutes(-10);

        await dbContext.SaveChangesAsync();

        var service =
            new PersonalTrainingRequestService(dbContext);

        var result =
            await service.CancelByMemberAsync(
                profile.ApplicationUserId,
                request.Id);

        Assert.False(result.Succeeded);

        Assert.Equal(
            PersonalTrainingRequestStatus.Scheduled,
            request.Status);
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
        Assert.NotNull(updatedRequest.CompletedAtUtc);
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

    [Fact]
    public async Task UpdateByTrainerAsync_SchedulesOwnPendingRequest()
    {
        await using var dbContext = CreateDbContext();

        var profile = await SeedMemberAsync(
            dbContext,
            includesPersonalTrainingSupport: true);

        var trainer = await SeedTrainerAsync(dbContext);

        var request =
            await SeedPersonalTrainingRequestAsync(
                dbContext,
                profile,
                trainer);

        var service =
            new PersonalTrainingRequestService(dbContext);

        var scheduledAtLocal =
            DateTime.Now.AddDays(2)
                .Date
                .AddHours(15);

        var result =
            await service.UpdateByTrainerAsync(
                trainer.ApplicationUserId!,
                request.Id,
                PersonalTrainingRequestStatus.Scheduled,
                scheduledAtLocal,
                "15.00 benim için uygundur.");

        Assert.True(result.Succeeded);

        var updatedRequest =
            await dbContext.PersonalTrainingRequests
                .SingleAsync();

        Assert.Equal(
            PersonalTrainingRequestStatus.Scheduled,
            updatedRequest.Status);

        Assert.NotNull(
            updatedRequest.ScheduledAtUtc);

        Assert.Equal(
            "15.00 benim için uygundur.",
            updatedRequest.TrainerNote);
    }

    [Fact]
    public async Task UpdateByTrainerAsync_RejectsOwnPendingRequest()
    {
        await using var dbContext = CreateDbContext();

        var profile = await SeedMemberAsync(
            dbContext,
            includesPersonalTrainingSupport: true);

        var trainer = await SeedTrainerAsync(dbContext);

        var request =
            await SeedPersonalTrainingRequestAsync(
                dbContext,
                profile,
                trainer);

        var service =
            new PersonalTrainingRequestService(dbContext);

        var result =
            await service.UpdateByTrainerAsync(
                trainer.ApplicationUserId!,
                request.Id,
                PersonalTrainingRequestStatus.Rejected,
                null,
                "Bu gün müsait değilim.");

        Assert.True(result.Succeeded);

        var updatedRequest =
            await dbContext.PersonalTrainingRequests
                .SingleAsync();

        Assert.Equal(
            PersonalTrainingRequestStatus.Rejected,
            updatedRequest.Status);

        Assert.Equal(
            "Bu gün müsait değilim.",
            updatedRequest.TrainerNote);
    }

    [Fact]
    public async Task UpdateByTrainerAsync_Fails_WhenRequestBelongsToAnotherTrainer()
    {
        await using var dbContext = CreateDbContext();

        var profile = await SeedMemberAsync(
            dbContext,
            includesPersonalTrainingSupport: true);

        var requestTrainer =
            await SeedTrainerAsync(dbContext);

        var otherTrainer =
            await SeedTrainerAsync(dbContext);

        var request =
            await SeedPersonalTrainingRequestAsync(
                dbContext,
                profile,
                requestTrainer);

        var service =
            new PersonalTrainingRequestService(dbContext);

        var result =
            await service.UpdateByTrainerAsync(
                otherTrainer.ApplicationUserId!,
                request.Id,
                PersonalTrainingRequestStatus.Rejected,
                null,
                "Test");

        Assert.False(result.Succeeded);

        var unchangedRequest =
            await dbContext.PersonalTrainingRequests
                .SingleAsync();

        Assert.Equal(
            PersonalTrainingRequestStatus.Pending,
            unchangedRequest.Status);
    }

    [Fact]
    public async Task UpdateByTrainerAsync_Fails_WhenRequestIsNotPending()
    {
        await using var dbContext = CreateDbContext();

        var profile = await SeedMemberAsync(
            dbContext,
            includesPersonalTrainingSupport: true);

        var trainer =
            await SeedTrainerAsync(dbContext);

        var request =
            await SeedPersonalTrainingRequestAsync(
                dbContext,
                profile,
                trainer);

        request.Status =
            PersonalTrainingRequestStatus.Scheduled;

        request.ScheduledAtUtc =
            DateTime.UtcNow.AddDays(1);

        await dbContext.SaveChangesAsync();

        var service =
            new PersonalTrainingRequestService(dbContext);

        var result =
            await service.UpdateByTrainerAsync(
                trainer.ApplicationUserId!,
                request.Id,
                PersonalTrainingRequestStatus.Rejected,
                null,
                "Sonradan reddetmeye çalışma.");

        Assert.False(result.Succeeded);

        Assert.Equal(
            PersonalTrainingRequestStatus.Scheduled,
            request.Status);
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
        var email =
            $"trainer-{Guid.NewGuid():N}@no23.test";

        var applicationUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "Trainer"
        };

        var trainer = new Trainer
        {
            FirstName = "Test",
            LastName = "Trainer",
            Specialty = "Strength",
            IsActive = isActive,
            ApplicationUser = applicationUser
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
