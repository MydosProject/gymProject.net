using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.Services;

public sealed class KitchenWeeklyPlanService(ApplicationDbContext db)
{
    public async Task<KitchenWeeklyPlanViewModel?> LoadAsync(string userId, DateOnly? requestedWeek = null,
        int? subscriptionId = null, DateOnly? todayOverride = null)
    {
        var today = todayOverride ?? DateOnly.FromDateTime(ClubTime.Now);
        var date = requestedWeek ?? today;
        if (date.Year is < 1900 or > 2100) throw new ArgumentOutOfRangeException(nameof(requestedWeek));
        var week = date.AddDays(-((int)date.DayOfWeek + 6) % 7);
        var end = week.AddDays(6);
        var subscriptions = await db.KitchenSubscriptions.AsNoTracking()
            .Where(x => x.MemberProfile.ApplicationUserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .Select(x => new KitchenWeekSubscription(x.Id, x.PackageNameSnapshot, x.Status,
                x.StartsOn, x.EndsOn, x.SelectedMealSlotsMask)).ToListAsync();
        var selected = subscriptionId.HasValue ? subscriptions.FirstOrDefault(x => x.Id == subscriptionId)
            : subscriptions.FirstOrDefault(x => x.Status == KitchenSubscriptionStatus.Active && x.EndsOn >= today)
                ?? subscriptions.FirstOrDefault(x => x.Status == KitchenSubscriptionStatus.PendingPayment)
                ?? subscriptions.FirstOrDefault(x => x.Status == KitchenSubscriptionStatus.Paused)
                ?? subscriptions.FirstOrDefault();
        if (subscriptionId.HasValue && selected is null) return null;

        KitchenMealPlan? plan = null;
        if (selected is not null)
            plan = await db.KitchenMealPlans.AsNoTracking()
                .Where(x => x.KitchenSubscriptionId == selected.Id)
                .Include(x => x.Days.Where(d => d.PlanDate >= week && d.PlanDate <= end))
                    .ThenInclude(x => x.Items)
                .AsSplitQuery().FirstOrDefaultAsync();

        var days = Enumerable.Range(0, 7).Select(offset =>
        {
            var dayDate = week.AddDays(offset);
            var day = plan?.Days.FirstOrDefault(x => x.PlanDate == dayDate);
            var state = StateFor(selected, plan, day, dayDate);
            // Pending/cancelled/paused subscriptions must never promise a delivery,
            // even if a previous meal plan exists in storage.
            var meals = state is KitchenWeekDayState.Planned or KitchenWeekDayState.Skipped
                ? day!.Items.OrderBy(x => x.MealSlot).ThenBy(x => x.Id)
                    .Select(x => new KitchenWeekMeal(x.MealSlot, x.ProductNameSnapshot, x.Quantity,
                        x.CaloriesSnapshot * x.Quantity, x.IsSkipped)).ToList() : [];
            return new KitchenWeekDay
            {
                Date = dayDate, State = state, Meals = meals,
                DeliveryMethod = state == KitchenWeekDayState.Planned ? day!.DeliveryMethod : KitchenDeliveryMethod.NotSelected,
                DeliveryArea = state == KitchenWeekDayState.Planned && day!.DeliveryMethod == KitchenDeliveryMethod.HomeDelivery
                    ? string.Join(" / ", new[] { day.DeliveryDistrict, day.DeliveryCity }.Where(x => !string.IsNullOrWhiteSpace(x))) : null
            };
        }).ToList();
        var slots = Enum.GetValues<KitchenMealSlot>().Where(slot => selected is not null &&
            (KitchenMealSelection.Contains(selected.SelectedMealSlotsMask, slot) || days.Any(d => d.Meals.Any(m => m.Slot == slot)))).ToList();
        return new KitchenWeeklyPlanViewModel
        {
            Week = week, Today = today, Subscription = selected, Subscriptions = subscriptions,
            MealSlots = slots, Days = days
        };
    }

    private static KitchenWeekDayState StateFor(KitchenWeekSubscription? subscription, KitchenMealPlan? plan,
        KitchenMealPlanDay? day, DateOnly date)
    {
        if (subscription is null || date < subscription.StartsOn || date > subscription.EndsOn) return KitchenWeekDayState.OutsidePackage;
        if (subscription.Status == KitchenSubscriptionStatus.PendingPayment) return KitchenWeekDayState.AwaitingPayment;
        if (subscription.Status == KitchenSubscriptionStatus.PaymentFailed) return KitchenWeekDayState.PaymentFailed;
        if (subscription.Status == KitchenSubscriptionStatus.Cancelled) return KitchenWeekDayState.Cancelled;
        if (subscription.Status == KitchenSubscriptionStatus.Paused) return KitchenWeekDayState.Paused;
        if (plan?.Status != KitchenMealPlanStatus.Generated || day is null || day.Items.Count == 0) return KitchenWeekDayState.Preparing;
        return day.Items.All(x => x.IsSkipped) ? KitchenWeekDayState.Skipped : KitchenWeekDayState.Planned;
    }
}
