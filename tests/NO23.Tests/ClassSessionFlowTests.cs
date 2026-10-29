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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RepeatedCancellationRestoresReservedCreditOnlyOnce(bool byAdmin)
    {
        await using var db = CreateDbContext();
        var member = await SeedMemberAsync(db);
        var session = await SeedClassSessionAsync(db, true, DateTime.UtcNow.AddDays(3), ClassSessionStatus.Scheduled);
        var service = new ClassReservationService(db);
        Assert.True((await service.ReserveAsync(member.ApplicationUserId, session.Id)).Succeeded);
        var reservation = await db.ClassReservations.SingleAsync();
        var first = byAdmin ? await service.CancelByAdminAsync(session.Id, reservation.Id)
            : await service.CancelAsync(member.ApplicationUserId, reservation.Id);
        var second = byAdmin ? await service.CancelByAdminAsync(session.Id, reservation.Id)
            : await service.CancelAsync(member.ApplicationUserId, reservation.Id);
        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Equal(4, member.RemainingClassCredits);
        Assert.Equal(ClassReservationStatus.Cancelled, reservation.Status);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(9, true)]
    [InlineData(10, false)]
    [InlineData(11, false)]
    public async Task ReserveAsync_RequiresLessonInsidePackagePeriod(int lessonOffset, bool allowed)
    {
        await using var db = CreateDbContext();
        var member = await SeedMemberAsync(db);
        var starts = DateTime.UtcNow.AddDays(5);
        member.MembershipStartsAtUtc = starts;
        member.MembershipEndsAtUtc = starts.AddDays(10);
        await db.SaveChangesAsync();
        var session = await SeedClassSessionAsync(db, true, starts.AddDays(lessonOffset), ClassSessionStatus.Scheduled);

        var result = await new ClassReservationService(db).ReserveAsync(member.ApplicationUserId, session.Id);

        Assert.Equal(allowed, result.Succeeded);
        Assert.Equal(allowed ? 3 : 4, member.RemainingClassCredits);
        Assert.Equal(allowed ? 1 : 0, await db.ClassReservations.CountAsync());
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ReserveAsync_UnlimitedCreditsStillRespectKnownStartAndSupportLegacyUnknownPeriod(bool knownStart, bool allowed)
    {
        await using var db = CreateDbContext();
        var member = await SeedMemberAsync(db);
        member.MembershipPackage.WeeklyClassLimit = null;
        member.MembershipStartsAtUtc = knownStart ? DateTime.UtcNow.AddDays(5) : null;
        await db.SaveChangesAsync();
        var session = await SeedClassSessionAsync(db, true, DateTime.UtcNow.AddDays(2), ClassSessionStatus.Scheduled);

        var result = await new ClassReservationService(db).ReserveAsync(member.ApplicationUserId, session.Id);

        Assert.Equal(allowed, result.Succeeded);
        Assert.Equal(4, member.RemainingClassCredits);
        Assert.Equal(allowed ? 1 : 0, await db.ClassReservations.CountAsync());
    }

    [Fact]
    public async Task ReserveAsync_KitchenAccountCannotBookSportsEvenWithinDates()
    {
        await using var db = CreateDbContext();
        var member = await SeedMemberAsync(db);
        member.MembershipPackage.Code = MembershipPackageCode.KitchenOnly;
        member.MembershipStartsAtUtc = DateTime.UtcNow.AddDays(-1);
        member.MembershipEndsAtUtc = DateTime.UtcNow.AddDays(10);
        await db.SaveChangesAsync();
        var session = await SeedClassSessionAsync(db, true, DateTime.UtcNow.AddDays(2), ClassSessionStatus.Scheduled);

        Assert.False((await new ClassReservationService(db).ReserveAsync(member.ApplicationUserId, session.Id)).Succeeded);
        Assert.Empty(db.ClassReservations);
        Assert.Equal(4, member.RemainingClassCredits);
    }

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
    public async Task CancelByAdminAsync_CancelsReservationAndRestoresCredit()
    {
        await using var dbContext = CreateDbContext();
        var profile = await SeedMemberAsync(dbContext);
        var session = await SeedClassSessionAsync(
            dbContext,
            isGroupClassActive: true,
            startsAtUtc: DateTime.UtcNow.AddDays(1),
            status: ClassSessionStatus.Scheduled);
        var service = new ClassReservationService(dbContext);
        var reserveResult = await service.ReserveAsync(
            profile.ApplicationUserId,
            session.Id);
        var reservation = await dbContext.ClassReservations.SingleAsync();

        var result = await service.CancelByAdminAsync(
            session.Id,
            reservation.Id);

        Assert.True(reserveResult.Succeeded);
        Assert.True(result.Succeeded);
        Assert.Equal(ClassReservationStatus.Cancelled, reservation.Status);
        Assert.Equal(4, profile.RemainingClassCredits);
        Assert.NotNull(reservation.CancelledAtUtc);
        Assert.Equal(
            "Yönetici tarafından ders listesinden çıkarıldı.",
            reservation.CancellationReason);
    }

    [Fact]
    public async Task GroupClassEdit_DeactivatesClassAndCancelsFutureSessions()
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
        Assert.Equal(ClassSessionStatus.Cancelled, session.Status);
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
