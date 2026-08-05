using Microsoft.EntityFrameworkCore;
using NO23.Web.Areas.Admin.Controllers;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Admin;

namespace NO23.Tests;

public class ClassSessionFlowTests
{
    [Fact]
    public async Task ReserveAsync_RejectsInactiveGroupClassSession()
    {
        await using var dbContext = CreateDbContext();
        var profile = await SeedMemberAsync(dbContext);
        var session = await SeedClassSessionAsync(
            dbContext,
            isGroupClassActive: false,
            startsAtUtc: DateTime.UtcNow.AddDays(1),
            status: ClassSessionStatus.Scheduled);
        var service = new ClassReservationService(dbContext);

        var result = await service.ReserveAsync(profile.ApplicationUserId, session.Id);

        Assert.False(result.Succeeded);
        Assert.Empty(dbContext.ClassReservations);
    }

    [Fact]
    public async Task ReserveAsync_RejectsPastScheduledSession()
    {
        await using var dbContext = CreateDbContext();
        var profile = await SeedMemberAsync(dbContext);
        var session = await SeedClassSessionAsync(
            dbContext,
            isGroupClassActive: true,
            startsAtUtc: DateTime.UtcNow.AddMinutes(-1),
            status: ClassSessionStatus.Scheduled);
        var service = new ClassReservationService(dbContext);

        var result = await service.ReserveAsync(profile.ApplicationUserId, session.Id);

        Assert.False(result.Succeeded);
        Assert.Empty(dbContext.ClassReservations);
    }

    [Fact]
    public async Task GroupClassEdit_DeactivatesClassWithoutCancellingFutureSessions()
    {
        await using var dbContext = CreateDbContext();
        var trainer = new Trainer
        {
            FirstName = "Test",
            LastName = "Trainer",
            Specialty = "Strength",
            Bio = "Bio",
            IsActive = true
        };
        var groupClass = new GroupClass
        {
            Trainer = trainer,
            Name = "HIIT",
            DurationMinutes = 45,
            DifficultyLevel = ClassDifficultyLevel.AllLevels,
            AverageCaloriesBurned = 350,
            Capacity = 10,
            IsActive = true
        };
        groupClass.Sessions.Add(new ClassSession
        {
            StartsAtUtc = DateTime.UtcNow.AddDays(1),
            Status = ClassSessionStatus.Scheduled
        });
        dbContext.GroupClasses.Add(groupClass);
        await dbContext.SaveChangesAsync();
        var controller = new GroupClassesController(dbContext);

        await controller.Edit(groupClass.Id, new GroupClassFormViewModel
        {
            Id = groupClass.Id,
            TrainerId = trainer.Id,
            Name = groupClass.Name,
            DurationMinutes = groupClass.DurationMinutes,
            DifficultyLevel = groupClass.DifficultyLevel,
            AverageCaloriesBurned = groupClass.AverageCaloriesBurned,
            Capacity = groupClass.Capacity,
            IsActive = false
        });

        var session = await dbContext.ClassSessions.SingleAsync();
        Assert.False(groupClass.IsActive);
        Assert.Equal(ClassSessionStatus.Scheduled, session.Status);
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
            Audience = "Aktif uyeler",
            Description = "Test paketi",
            WeeklyClassLimit = 4
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

    private static async Task<ClassSession> SeedClassSessionAsync(
        ApplicationDbContext dbContext,
        bool isGroupClassActive,
        DateTime startsAtUtc,
        ClassSessionStatus status)
    {
        var trainer = new Trainer
        {
            FirstName = "Test",
            LastName = "Trainer",
            Specialty = "Strength",
            Bio = "Bio",
            IsActive = true
        };
        var groupClass = new GroupClass
        {
            Trainer = trainer,
            Name = $"Class {Guid.NewGuid()}",
            DurationMinutes = 45,
            DifficultyLevel = ClassDifficultyLevel.AllLevels,
            AverageCaloriesBurned = 350,
            Capacity = 10,
            IsActive = isGroupClassActive
        };
        var session = new ClassSession
        {
            GroupClass = groupClass,
            StartsAtUtc = startsAtUtc,
            Status = status
        };

        dbContext.ClassSessions.Add(session);
        await dbContext.SaveChangesAsync();
        return session;
    }
}
