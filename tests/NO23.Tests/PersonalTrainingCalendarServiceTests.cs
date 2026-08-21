using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Tests;

public class PersonalTrainingCalendarServiceTests
{
    [Fact]
    public async Task CreateAsync_RejectsMemberAssignedToAnotherTrainer()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var otherTrainer = new Trainer { FirstName = "Other", LastName = "Trainer", Specialty = "PT" };
        db.Trainers.Add(otherTrainer);
        await db.SaveChangesAsync();

        var result = await new PersonalTrainingCalendarService(db).CreateAsync(
            otherTrainer.Id, member.Id, DateTime.UtcNow.AddDays(1), 60, null);

        Assert.False(result.Succeeded);
        Assert.Empty(db.PersonalTrainingSessions);
    }

    [Fact]
    public async Task CancelledSession_ConsumesExactlyOneCredit()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var service = new PersonalTrainingCalendarService(db);
        await service.CreateAsync(trainer.Id, member.Id, DateTime.UtcNow.AddDays(1), 60, null);
        var session = await db.PersonalTrainingSessions.SingleAsync();

        var result = await service.ChangeStatusAsync(trainer.Id, session.Id,
            PersonalTrainingSessionStatus.Cancelled, null, "trainer-user", "Müşteri iptal etti");
        var secondResult = await service.ChangeStatusAsync(trainer.Id, session.Id,
            PersonalTrainingSessionStatus.Cancelled, null, "trainer-user", null);

        Assert.True(result.Succeeded);
        Assert.False(secondResult.Succeeded);
        Assert.Equal(3, member.RemainingClassCredits);
        Assert.True(session.CreditConsumed);
        Assert.Single(db.PersonalTrainingSessionHistories);
    }

    [Fact]
    public async Task PostponedSession_KeepsCreditAndUpdatesDateWithHistory()
    {
        await using var db = CreateDbContext();
        var (trainer, member) = await SeedAsync(db);
        var service = new PersonalTrainingCalendarService(db);
        var original = DateTime.UtcNow.AddDays(1);
        var postponed = original.AddDays(2);
        await service.CreateAsync(trainer.Id, member.Id, original, 60, "İlk ders detayı");
        var session = await db.PersonalTrainingSessions.SingleAsync();

        var result = await service.ChangeStatusAsync(trainer.Id, session.Id,
            PersonalTrainingSessionStatus.Postponed, postponed, "trainer-user", "Üye erteledi");

        Assert.True(result.Succeeded);
        Assert.Equal(4, member.RemainingClassCredits);
        Assert.False(session.CreditConsumed);
        Assert.Equal(PersonalTrainingSessionStatus.Scheduled, session.Status);
        Assert.Equal(postponed, session.StartsAtUtc);
        Assert.Equal("İlk ders detayı", session.Note);
        Assert.Equal(PersonalTrainingSessionStatus.Postponed,
            (await db.PersonalTrainingSessionHistories.SingleAsync()).NewStatus);
        Assert.Equal("Üye erteledi",
            (await db.PersonalTrainingSessionHistories.SingleAsync()).Note);
    }

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<(Trainer Trainer, MemberProfile Member)> SeedAsync(ApplicationDbContext db)
    {
        var trainer = new Trainer { FirstName = "Test", LastName = "Trainer", Specialty = "PT" };
        var member = new MemberProfile
        {
            ApplicationUser = new ApplicationUser { UserName = "member@test.local", Email = "member@test.local" },
            MembershipPackage = new MembershipPackage
            {
                Code = MembershipPackageCode.Pro, Name = "Pro", Audience = "Test", Description = "Test",
                WeeklyClassLimit = 4
            },
            AssignedTrainer = trainer,
            RemainingClassCredits = 4
        };
        db.Add(member);
        await db.SaveChangesAsync();
        return (trainer, member);
    }
}
