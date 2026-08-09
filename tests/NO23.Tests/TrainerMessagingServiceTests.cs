using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Tests;

public class TrainerMessagingServiceTests
{
    [Fact]
    public async Task CanMemberWriteAsync_ReturnsTrue_ForPendingRequest()
    {
        await using var dbContext = CreateDbContext();

        var member =
            await SeedMemberAsync(dbContext);

        var trainer =
            await SeedTrainerAsync(dbContext);

        var conversation =
            await SeedConversationAsync(
                dbContext,
                member,
                trainer);

        await SeedRequestAsync(
            dbContext,
            member,
            trainer,
            PersonalTrainingRequestStatus.Pending);

        var service =
            new TrainerMessagingService(dbContext);

        var result =
            await service.CanMemberWriteAsync(
                member.ApplicationUserId,
                conversation.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task CanTrainerWriteAsync_ReturnsTrue_ForScheduledRequest()
    {
        await using var dbContext = CreateDbContext();

        var member =
            await SeedMemberAsync(dbContext);

        var trainer =
            await SeedTrainerAsync(dbContext);

        var conversation =
            await SeedConversationAsync(
                dbContext,
                member,
                trainer);

        await SeedRequestAsync(
            dbContext,
            member,
            trainer,
            PersonalTrainingRequestStatus.Scheduled);

        var service =
            new TrainerMessagingService(dbContext);

        var result =
            await service.CanTrainerWriteAsync(
                trainer.ApplicationUserId!,
                conversation.Id);

        Assert.True(result);
    }

    [Theory]
    [InlineData(
        PersonalTrainingRequestStatus.Rejected)]
    [InlineData(
        PersonalTrainingRequestStatus.Cancelled)]
    public async Task CanMemberWriteAsync_ReturnsFalse_ForClosedStatus(
        PersonalTrainingRequestStatus status)
    {
        await using var dbContext = CreateDbContext();

        var member =
            await SeedMemberAsync(dbContext);

        var trainer =
            await SeedTrainerAsync(dbContext);

        var conversation =
            await SeedConversationAsync(
                dbContext,
                member,
                trainer);

        await SeedRequestAsync(
            dbContext,
            member,
            trainer,
            status);

        var service =
            new TrainerMessagingService(dbContext);

        var result =
            await service.CanMemberWriteAsync(
                member.ApplicationUserId,
                conversation.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task CanMemberWriteAsync_ReturnsTrue_WhenCompletedWithin48Hours()
    {
        await using var dbContext = CreateDbContext();

        var member =
            await SeedMemberAsync(dbContext);

        var trainer =
            await SeedTrainerAsync(dbContext);

        var conversation =
            await SeedConversationAsync(
                dbContext,
                member,
                trainer);

        await SeedRequestAsync(
            dbContext,
            member,
            trainer,
            PersonalTrainingRequestStatus.Completed,
            DateTime.UtcNow.AddHours(-47));

        var service =
            new TrainerMessagingService(dbContext);

        var result =
            await service.CanMemberWriteAsync(
                member.ApplicationUserId,
                conversation.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task CanTrainerWriteAsync_ReturnsFalse_WhenCompletedMoreThan48HoursAgo()
    {
        await using var dbContext = CreateDbContext();

        var member =
            await SeedMemberAsync(dbContext);

        var trainer =
            await SeedTrainerAsync(dbContext);

        var conversation =
            await SeedConversationAsync(
                dbContext,
                member,
                trainer);

        await SeedRequestAsync(
            dbContext,
            member,
            trainer,
            PersonalTrainingRequestStatus.Completed,
            DateTime.UtcNow.AddHours(-49));

        var service =
            new TrainerMessagingService(dbContext);

        var result =
            await service.CanTrainerWriteAsync(
                trainer.ApplicationUserId!,
                conversation.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task SendByMemberAsync_Fails_ForAnotherMembersConversation()
    {
        await using var dbContext = CreateDbContext();

        var owner =
            await SeedMemberAsync(dbContext);

        var otherMember =
            await SeedMemberAsync(dbContext);

        var trainer =
            await SeedTrainerAsync(dbContext);

        var conversation =
            await SeedConversationAsync(
                dbContext,
                owner,
                trainer);

        await SeedRequestAsync(
            dbContext,
            owner,
            trainer,
            PersonalTrainingRequestStatus.Pending);

        var service =
            new TrainerMessagingService(dbContext);

        var result =
            await service.SendByMemberAsync(
                otherMember.ApplicationUserId,
                conversation.Id,
                "Bu mesaj gönderilmemeli.");

        Assert.False(result.Succeeded);

        Assert.Empty(
            dbContext.TrainerMessages);
    }

    [Fact]
    public async Task SendByTrainerAsync_Fails_ForAnotherTrainersConversation()
    {
        await using var dbContext = CreateDbContext();

        var member =
            await SeedMemberAsync(dbContext);

        var ownerTrainer =
            await SeedTrainerAsync(dbContext);

        var otherTrainer =
            await SeedTrainerAsync(dbContext);

        var conversation =
            await SeedConversationAsync(
                dbContext,
                member,
                ownerTrainer);

        await SeedRequestAsync(
            dbContext,
            member,
            ownerTrainer,
            PersonalTrainingRequestStatus.Pending);

        var service =
            new TrainerMessagingService(dbContext);

        var result =
            await service.SendByTrainerAsync(
                otherTrainer.ApplicationUserId!,
                conversation.Id,
                "Bu mesaj gönderilmemeli.");

        Assert.False(result.Succeeded);

        Assert.Empty(
            dbContext.TrainerMessages);
    }

    [Fact]
    public async Task MarkAsReadByTrainerAsync_MarksOnlyMemberMessages()
    {
        await using var dbContext = CreateDbContext();

        var member =
            await SeedMemberAsync(dbContext);

        var trainer =
            await SeedTrainerAsync(dbContext);

        var conversation =
            await SeedConversationAsync(
                dbContext,
                member,
                trainer);

        var memberMessage =
            new TrainerMessage
            {
                TrainerConversationId =
                    conversation.Id,

                SenderApplicationUserId =
                    member.ApplicationUserId,

                Body =
                    "Üyeden mesaj"
            };

        var trainerMessage =
            new TrainerMessage
            {
                TrainerConversationId =
                    conversation.Id,

                SenderApplicationUserId =
                    trainer.ApplicationUserId!,

                Body =
                    "Eğitmenden mesaj"
            };

        dbContext.TrainerMessages.AddRange(
            memberMessage,
            trainerMessage);

        await dbContext.SaveChangesAsync();

        var service =
            new TrainerMessagingService(dbContext);

        var result =
            await service.MarkAsReadByTrainerAsync(
                trainer.ApplicationUserId!,
                conversation.Id);

        Assert.True(result);

        Assert.NotNull(
            memberMessage.ReadAtUtc);

        Assert.Null(
            trainerMessage.ReadAtUtc);
    }

    [Fact]
    public async Task MarkAsReadByMemberAsync_MarksOnlyTrainerMessages()
    {
        await using var dbContext = CreateDbContext();

        var member =
            await SeedMemberAsync(dbContext);

        var trainer =
            await SeedTrainerAsync(dbContext);

        var conversation =
            await SeedConversationAsync(
                dbContext,
                member,
                trainer);

        var memberMessage =
            new TrainerMessage
            {
                TrainerConversationId =
                    conversation.Id,

                SenderApplicationUserId =
                    member.ApplicationUserId,

                Body =
                    "Üyeden mesaj"
            };

        var trainerMessage =
            new TrainerMessage
            {
                TrainerConversationId =
                    conversation.Id,

                SenderApplicationUserId =
                    trainer.ApplicationUserId!,

                Body =
                    "Eğitmenden mesaj"
            };

        dbContext.TrainerMessages.AddRange(
            memberMessage,
            trainerMessage);

        await dbContext.SaveChangesAsync();

        var service =
            new TrainerMessagingService(dbContext);

        var result =
            await service.MarkAsReadByMemberAsync(
                member.ApplicationUserId,
                conversation.Id);

        Assert.True(result);

        Assert.Null(
            memberMessage.ReadAtUtc);

        Assert.NotNull(
            trainerMessage.ReadAtUtc);
    }

    [Fact]
    public async Task CanAccessConversationAsync_ReturnsTrue_ForMemberAndTrainer()
    {
        await using var dbContext =
            CreateDbContext();

        var member =
            await SeedMemberAsync(
                dbContext);

        var trainer =
            await SeedTrainerAsync(
                dbContext);

        var conversation =
            await SeedConversationAsync(
                dbContext,
                member,
                trainer);

        var service =
            new TrainerMessagingService(
                dbContext);

        var memberCanAccess =
            await service
                .CanAccessConversationAsync(
                    member.ApplicationUserId,
                    conversation.Id);

        var trainerCanAccess =
            await service
                .CanAccessConversationAsync(
                    trainer.ApplicationUserId!,
                    conversation.Id);

        Assert.True(memberCanAccess);
        Assert.True(trainerCanAccess);
    }

    [Fact]
    public async Task CanAccessConversationAsync_ReturnsFalse_ForUnrelatedUser()
    {
        await using var dbContext =
            CreateDbContext();

        var owner =
            await SeedMemberAsync(
                dbContext);

        var unrelatedMember =
            await SeedMemberAsync(
                dbContext);

        var trainer =
            await SeedTrainerAsync(
                dbContext);

        var conversation =
            await SeedConversationAsync(
                dbContext,
                owner,
                trainer);

        var service =
            new TrainerMessagingService(
                dbContext);

        var result =
            await service
                .CanAccessConversationAsync(
                    unrelatedMember.ApplicationUserId,
                    conversation.Id);

        Assert.False(result);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid().ToString())
                .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<MemberProfile> SeedMemberAsync(
        ApplicationDbContext dbContext)
    {
        var email =
            $"member-{Guid.NewGuid():N}@no23.test";

        var user =
            new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = "Test",
                LastName = "Member"
            };

        var package =
            new MembershipPackage
            {
                Code = MembershipPackageCode.Pro,
                Name = "Pro",
                Audience = "Test",
                Description = "Test",
                IncludesPersonalTrainingSupport = true
            };

        var profile =
            new MemberProfile
            {
                ApplicationUser = user,
                MembershipPackage = package,
                RemainingClassCredits = 5
            };

        dbContext.MemberProfiles.Add(profile);

        await dbContext.SaveChangesAsync();

        return profile;
    }

    private static async Task<Trainer> SeedTrainerAsync(
        ApplicationDbContext dbContext)
    {
        var email =
            $"trainer-{Guid.NewGuid():N}@no23.test";

        var user =
            new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = "Test",
                LastName = "Trainer"
            };

        var trainer =
            new Trainer
            {
                FirstName = "Test",
                LastName = "Trainer",
                Specialty = "Strength",
                IsActive = true,
                ApplicationUser = user
            };

        dbContext.Trainers.Add(trainer);

        await dbContext.SaveChangesAsync();

        return trainer;
    }

    private static async Task<TrainerConversation>
        SeedConversationAsync(
            ApplicationDbContext dbContext,
            MemberProfile member,
            Trainer trainer)
    {
        var conversation =
            new TrainerConversation
            {
                MemberProfileId =
                    member.Id,

                TrainerId =
                    trainer.Id
            };

        dbContext.TrainerConversations.Add(
            conversation);

        await dbContext.SaveChangesAsync();

        return conversation;
    }

    private static async Task<PersonalTrainingRequest>
        SeedRequestAsync(
            ApplicationDbContext dbContext,
            MemberProfile member,
            Trainer trainer,
            PersonalTrainingRequestStatus status,
            DateTime? completedAtUtc = null)
    {
        var request =
            new PersonalTrainingRequest
            {
                MemberProfileId =
                    member.Id,

                TrainerId =
                    trainer.Id,

                PreferredDate =
                    DateOnly.FromDateTime(
                        DateTime.Today.AddDays(1)),

                PreferredTimeWindow =
                    PersonalTrainingRequestService
                        .PreferredTimeWindows[0],

                Status =
                    status,

                ScheduledAtUtc =
                    status is
                        PersonalTrainingRequestStatus.Scheduled
                        or
                        PersonalTrainingRequestStatus.Completed
                        ? DateTime.UtcNow.AddHours(-1)
                        : null,

                CompletedAtUtc =
                    completedAtUtc
            };

        dbContext.PersonalTrainingRequests.Add(
            request);

        await dbContext.SaveChangesAsync();

        return request;
    }
}