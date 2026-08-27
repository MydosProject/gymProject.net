using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Tests;

public class CommunityEventReservationTests
{
    [Fact]
    public async Task ReserveAsync_EnforcesCapacityAndAllowsPlaceAfterCancellation()
    {
        await using var dbContext = CreateDbContext();
        var firstMember = AddMember(dbContext, "first@no23.test");
        var secondMember = AddMember(dbContext, "second@no23.test");
        var eventItem = new CommunityEvent
        {
            Title = "NO23 Koşu Buluşması",
            Slug = "kosu-bulusmasi",
            Summary = "Test etkinliği",
            Description = "Test etkinliği açıklaması",
            Type = CommunityEventType.RunningGroup,
            Status = CommunityEventStatus.Scheduled,
            StartsAtUtc = DateTime.UtcNow.AddDays(2),
            Location = "NO23",
            Capacity = 1
        };
        dbContext.CommunityEvents.Add(eventItem);
        await dbContext.SaveChangesAsync();
        var service = new CommunityEventReservationService(dbContext);

        var firstResult = await service.ReserveAsync(
            firstMember.ApplicationUserId,
            eventItem.Slug);
        var fullResult = await service.ReserveAsync(
            secondMember.ApplicationUserId,
            eventItem.Slug);
        var cancelResult = await service.CancelAsync(
            firstMember.ApplicationUserId,
            eventItem.Slug);
        var secondResult = await service.ReserveAsync(
            secondMember.ApplicationUserId,
            eventItem.Slug);

        Assert.True(firstResult.Succeeded);
        Assert.False(fullResult.Succeeded);
        Assert.True(cancelResult.Succeeded);
        Assert.True(secondResult.Succeeded);
        Assert.Single(
            dbContext.CommunityEventReservations.Where(reservation =>
                reservation.Status == CommunityEventReservationStatus.Reserved));
    }

    [Fact]
    public async Task ReserveAsync_RejectsStartedEvent()
    {
        await using var dbContext = CreateDbContext();
        var member = AddMember(dbContext, "member@no23.test");
        dbContext.CommunityEvents.Add(new CommunityEvent
        {
            Title = "Başlayan Etkinlik",
            Slug = "baslayan-etkinlik",
            Summary = "Test etkinliği",
            Description = "Test etkinliği açıklaması",
            Type = CommunityEventType.Workshop,
            Status = CommunityEventStatus.Scheduled,
            StartsAtUtc = DateTime.UtcNow.AddMinutes(-5),
            EndsAtUtc = DateTime.UtcNow.AddHours(1),
            Location = "NO23"
        });
        await dbContext.SaveChangesAsync();
        var service = new CommunityEventReservationService(dbContext);

        var result = await service.ReserveAsync(
            member.ApplicationUserId,
            "baslayan-etkinlik");

        Assert.False(result.Succeeded);
        Assert.Empty(dbContext.CommunityEventReservations);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static MemberProfile AddMember(
        ApplicationDbContext dbContext,
        string email)
    {
        var profile = new MemberProfile
        {
            ApplicationUser = new ApplicationUser
            {
                UserName = email,
                Email = email
            },
            MembershipPackage = new MembershipPackage
            {
                Code = MembershipPackageCode.Pro,
                Name = $"Pro {email}",
                Audience = "Test",
                Description = "Test paketi"
            }
        };
        dbContext.MemberProfiles.Add(profile);
        return profile;
    }
}
