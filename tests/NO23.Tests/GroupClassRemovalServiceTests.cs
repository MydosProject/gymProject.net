using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Tests;

public class GroupClassRemovalServiceTests
{
    [Fact]
    public async Task RemoveAsync_DeletesUnusedGroupClass()
    {
        await using var db = Db();
        var group = new GroupClass
        {
            Name = "Test", DurationMinutes = 50, Capacity = 8,
            Trainer = new Trainer { FirstName = "Test", LastName = "Trainer", Specialty = "PT" }
        };
        db.GroupClasses.Add(group);
        await db.SaveChangesAsync();

        var result = await new GroupClassRemovalService(db).RemoveAsync(group.Id);

        Assert.True(result.Succeeded);
        Assert.True(result.Deleted);
        Assert.Empty(db.GroupClasses);
    }

    [Fact]
    public async Task RemoveAsync_ArchivesGroupAndRefundsFutureReservations()
    {
        await using var db = Db();
        var group = new GroupClass
        {
            Name = "Test", DurationMinutes = 50, Capacity = 8,
            Trainer = new Trainer { FirstName = "Test", LastName = "Trainer", Specialty = "PT" }
        };
        var member = new MemberProfile
        {
            ApplicationUser = new ApplicationUser { UserName = "member@test.local", Email = "member@test.local" },
            MembershipPackage = new MembershipPackage
            {
                Code = MembershipPackageCode.Pro, Name = "Pro", Audience = "Test", Description = "Test", WeeklyClassLimit = 4
            },
            RemainingClassCredits = 3
        };
        var future = new ClassSession { GroupClass = group, StartsAtUtc = DateTime.UtcNow.AddDays(1) };
        future.Reservations.Add(new ClassReservation { MemberProfile = member });
        var past = new ClassSession { GroupClass = group, StartsAtUtc = DateTime.UtcNow.AddDays(-2), Status = ClassSessionStatus.Completed };
        db.ClassSessions.AddRange(future, past);
        await db.SaveChangesAsync();

        var result = await new GroupClassRemovalService(db).RemoveAsync(group.Id);

        Assert.True(result.Succeeded);
        Assert.False(result.Deleted);
        Assert.False(group.IsActive);
        Assert.Equal(ClassSessionStatus.Cancelled, future.Status);
        Assert.Equal(ClassReservationStatus.Cancelled, future.Reservations.Single().Status);
        Assert.Equal(4, member.RemainingClassCredits);
        Assert.Equal(ClassSessionStatus.Completed, past.Status);
        Assert.Contains(member.ApplicationUserId, result.AffectedMemberUserIds);
    }

    private static ApplicationDbContext Db() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
