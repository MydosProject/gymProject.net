using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.Services.Payments;
using NO23.Web.ViewModels.Member;

namespace NO23.Tests;

public class KitchenWeeklyPlanTests
{
    private static readonly DateOnly Today = new(2026, 10, 21);

    [Fact]
    public async Task Week_UsesMondayThroughSundayAndOnlyTheChosenMembersSnapshotProducts()
    {
        await using var db = Db();
        var own = AddSubscription(db, "own");
        var other = AddSubscription(db, "other");
        var plan = AddPlan(own);
        var monday = Today.AddDays(-2);
        AddDay(plan, monday.AddDays(-1), "Previous week");
        var day = AddDay(plan, Today, "Kayıtlı ürün", quantity: 2);
        day.DeliveryMethod = KitchenDeliveryMethod.HomeDelivery;
        day.DeliveryDistrict = "Kadıköy"; day.DeliveryCity = "İstanbul";
        AddDay(plan, monday.AddDays(7), "Next week");
        AddDay(AddPlan(other), Today, "Other members private product");
        await db.SaveChangesAsync();

        var result = (await new KitchenWeeklyPlanService(db).LoadAsync("own", monday.AddDays(6), todayOverride: Today))!;
        Assert.Equal(monday, result.Week);
        Assert.Equal(monday.AddDays(6), result.WeekEnd);
        Assert.Equal(7, result.Days.Count);
        var planned = Assert.Single(result.Days, x => x.State == KitchenWeekDayState.Planned);
        Assert.Equal(Today, planned.Date);
        Assert.Equal("Kayıtlı ürün", Assert.Single(planned.Meals).ProductName);
        Assert.Equal(2, planned.Meals[0].Quantity);
        Assert.Equal(1000, planned.Calories);
        Assert.Equal(KitchenDeliveryMethod.HomeDelivery, planned.DeliveryMethod);
        Assert.Equal("Kadıköy / İstanbul", planned.DeliveryArea);
        Assert.Equal(2, result.Portions);
        Assert.Equal(1, result.PlannedDays);
        Assert.Equal(own.Id, Assert.Single(result.Subscriptions).Id);
        Assert.Equal([KitchenMealSlot.Lunch, KitchenMealSlot.AfternoonSnack], result.MealSlots);
        Assert.Equal("Güncel katalog adı", day.Items.Single().KitchenMenuItem.Name);
    }

    [Fact]
    public async Task HistoricalWeeksAndSubscriptionSelection_DoNotChangeAnyStoredPlanOrBalance()
    {
        await using var db = Db();
        var active = AddSubscription(db, "own");
        var old = AddSubscription(db, "old", KitchenSubscriptionStatus.Completed);
        old.MemberProfile = active.MemberProfile;
        old.StartsOn = new DateOnly(2025, 12, 29); old.EndsOn = old.StartsOn.AddDays(4);
        AddDay(AddPlan(old), old.StartsOn, "Eski plan");
        await db.SaveChangesAsync();
        var result = (await new KitchenWeeklyPlanService(db).LoadAsync("own", new DateOnly(2026, 1, 1), old.Id, Today))!;
        Assert.Equal(new DateOnly(2025, 12, 29), result.Week);
        Assert.Equal(old.Id, result.Subscription!.Id);
        Assert.Contains(result.Days.SelectMany(x => x.Meals), x => x.ProductName == "Eski plan");
        Assert.Equal(0, active.MemberProfile.RemainingClassCredits);
        Assert.DoesNotContain(db.ChangeTracker.Entries(), x => x.State != EntityState.Unchanged);
    }

    [Theory]
    [InlineData(KitchenSubscriptionStatus.PendingPayment, KitchenWeekDayState.AwaitingPayment)]
    [InlineData(KitchenSubscriptionStatus.PaymentFailed, KitchenWeekDayState.PaymentFailed)]
    [InlineData(KitchenSubscriptionStatus.Paused, KitchenWeekDayState.Paused)]
    [InlineData(KitchenSubscriptionStatus.Cancelled, KitchenWeekDayState.Cancelled)]
    public async Task InactiveSubscriptions_DoNotPromiseProductsOrDeliveryEvenIfAPlanExists(
        KitchenSubscriptionStatus status, KitchenWeekDayState expected)
    {
        await using var db = Db();
        var subscription = AddSubscription(db, "own", status);
        var day = AddDay(AddPlan(subscription), Today, "Must not be promised");
        day.DeliveryMethod = KitchenDeliveryMethod.GymPickup;
        await db.SaveChangesAsync();
        var result = (await new KitchenWeeklyPlanService(db).LoadAsync("own", todayOverride: Today))!;
        var row = result.Days.Single(x => x.Date == Today);
        Assert.Equal(expected, row.State);
        Assert.Empty(row.Meals);
        Assert.Equal(KitchenDeliveryMethod.NotSelected, row.DeliveryMethod);
        Assert.Equal(0, result.PlannedDays);
        Assert.Equal(0, result.Portions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingOrFailedPlan_IsPreparingAndPackageBoundariesStayVisible(bool failedPlan)
    {
        await using var db = Db();
        var subscription = AddSubscription(db, "own");
        subscription.StartsOn = Today; subscription.EndsOn = Today.AddDays(1);
        if (failedPlan) AddPlan(subscription).Status = KitchenMealPlanStatus.Failed;
        await db.SaveChangesAsync();
        var result = (await new KitchenWeeklyPlanService(db).LoadAsync("own", todayOverride: Today))!;
        Assert.Equal(2, result.Days.Count(x => x.State == KitchenWeekDayState.Preparing));
        Assert.Equal(5, result.Days.Count(x => x.State == KitchenWeekDayState.OutsidePackage));
    }

    [Fact]
    public async Task SkippedDaysAndMeals_AreMarkedAndExcludedFromPortionsAndCalories()
    {
        await using var db = Db();
        var subscription = AddSubscription(db, "own");
        var plan = AddPlan(subscription);
        var partial = AddDay(plan, Today, "Öğle", 2);
        partial.Items.Add(new KitchenMealPlanItem
        {
            KitchenMenuItem = new KitchenMenuItem { Name = "Snack" }, ProductNameSnapshot = "Pas geçilen ara öğün",
            MealSlot = KitchenMealSlot.AfternoonSnack, Quantity = 3, CaloriesSnapshot = 100, IsSkipped = true
        });
        var skipped = AddDay(plan, Today.AddDays(1), "Pas geçilen gün", 4);
        skipped.Items.Single().IsSkipped = true;
        skipped.DeliveryMethod = KitchenDeliveryMethod.HomeDelivery;
        await db.SaveChangesAsync();
        var result = (await new KitchenWeeklyPlanService(db).LoadAsync("own", todayOverride: Today))!;
        Assert.Equal(2, result.Portions);
        Assert.Equal(1000, result.Days.Single(x => x.Date == Today).Calories);
        var row = result.Days.Single(x => x.Date == Today.AddDays(1));
        Assert.Equal(KitchenWeekDayState.Skipped, row.State);
        Assert.True(Assert.Single(row.Meals).IsSkipped);
        Assert.Equal(KitchenDeliveryMethod.NotSelected, row.DeliveryMethod);
        Assert.Equal(0, row.Calories);
    }

    [Fact]
    public async Task DefaultSelection_PrefersActivePackageOverANewerPendingPayment()
    {
        await using var db = Db();
        var active = AddSubscription(db, "own");
        active.CreatedAtUtc = DateTime.UtcNow.AddDays(-3);
        var pending = AddSubscription(db, "pending", KitchenSubscriptionStatus.PendingPayment);
        pending.MemberProfile = active.MemberProfile;
        await db.SaveChangesAsync();
        var result = (await new KitchenWeeklyPlanService(db).LoadAsync("own", todayOverride: Today))!;
        Assert.Equal(active.Id, result.Subscription!.Id);
        Assert.Equal(2, result.Subscriptions.Count);
    }

    [Fact]
    public async Task OwnershipAndDateValidation_AreEnforcedAtTheControllerBoundary()
    {
        await using var db = Db();
        var other = AddSubscription(db, "other");
        await db.SaveChangesAsync();
        var controller = new NO23.Web.Areas.Member.Controllers.KitchenController(db, new(), new(db), null!,
            Options.Create(new IyzicoOptions()), Options.Create(new ClubPickupOptions()))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        Assert.IsType<ChallengeResult>(await controller.Index());
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "own")], "test"));
        Assert.IsType<NotFoundResult>(await controller.Index(subscriptionId: other.Id));
        Assert.IsType<NotFoundResult>(await controller.Index(subscriptionId: int.MaxValue));
        Assert.IsType<BadRequestResult>(await controller.Index(new DateOnly(9999, 12, 31)));
        controller.ModelState.AddModelError("week", "invalid");
        Assert.IsType<BadRequestResult>(await controller.Index());
    }

    [Fact]
    public async Task NoSubscription_IsAnEmptyWeekWithoutInventedProducts()
    {
        await using var db = Db();
        var result = (await new KitchenWeeklyPlanService(db).LoadAsync("new-user", todayOverride: Today))!;
        Assert.Null(result.Subscription);
        Assert.Empty(result.MealSlots);
        Assert.All(result.Days, x => Assert.Equal(KitchenWeekDayState.OutsidePackage, x.State));
    }

    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static KitchenSubscription AddSubscription(ApplicationDbContext db, string userId,
        KitchenSubscriptionStatus status = KitchenSubscriptionStatus.Active)
    {
        KitchenMealSelection.TryCreateMask([KitchenMealSlot.Lunch, KitchenMealSlot.AfternoonSnack], out var mask);
        var subscription = new KitchenSubscription
        {
            MemberProfile = new MemberProfile { ApplicationUser = new ApplicationUser { Id = userId },
                MembershipPackage = new MembershipPackage { Code = MembershipPackageCode.KitchenOnly } },
            KitchenSubscriptionPackage = new KitchenSubscriptionPackage { Name = "Test" },
            PackageNameSnapshot = "Kitchen Test Paketi", StartsOn = Today.AddDays(-10), EndsOn = Today.AddDays(20),
            SelectedMealSlotsMask = mask, Status = status
        };
        db.KitchenSubscriptions.Add(subscription);
        return subscription;
    }

    private static KitchenMealPlan AddPlan(KitchenSubscription subscription)
    {
        var plan = new KitchenMealPlan { KitchenSubscription = subscription };
        subscription.MealPlan = plan;
        return plan;
    }

    private static KitchenMealPlanDay AddDay(KitchenMealPlan plan, DateOnly date, string name, int quantity = 1)
    {
        var day = new KitchenMealPlanDay { PlanDate = date, DayNumber = date.DayNumber - plan.KitchenSubscription.StartsOn.DayNumber + 1 };
        day.Items.Add(new KitchenMealPlanItem { KitchenMenuItem = new KitchenMenuItem { Name = "Güncel katalog adı" },
            ProductNameSnapshot = name, MealSlot = KitchenMealSlot.Lunch, Quantity = quantity, CaloriesSnapshot = 500 });
        plan.Days.Add(day);
        return day;
    }
}
