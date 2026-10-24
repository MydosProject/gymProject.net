using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Member;

public sealed class KitchenWeeklyPlanViewModel
{
    public DateOnly Week { get; init; }
    public DateOnly WeekEnd => Week.AddDays(6);
    public DateOnly Today { get; init; }
    public KitchenWeekSubscription? Subscription { get; init; }
    public IReadOnlyList<KitchenWeekSubscription> Subscriptions { get; init; } = [];
    public IReadOnlyList<KitchenMealSlot> MealSlots { get; init; } = [];
    public IReadOnlyList<KitchenWeekDay> Days { get; init; } = [];
    public int PlannedDays => Days.Count(x => x.State == KitchenWeekDayState.Planned);
    public int Portions => Days.Where(x => x.State == KitchenWeekDayState.Planned)
        .SelectMany(x => x.Meals).Where(x => !x.IsSkipped).Sum(x => x.Quantity);
}

public sealed record KitchenWeekSubscription(int Id, string Name, KitchenSubscriptionStatus Status,
    DateOnly StartsOn, DateOnly EndsOn, int SelectedMealSlotsMask);

public sealed class KitchenWeekDay
{
    public DateOnly Date { get; init; }
    public KitchenWeekDayState State { get; init; }
    public KitchenDeliveryMethod DeliveryMethod { get; init; }
    public string? DeliveryArea { get; init; }
    public IReadOnlyList<KitchenWeekMeal> Meals { get; init; } = [];
    public int Calories => State == KitchenWeekDayState.Planned ? Meals.Where(x => !x.IsSkipped).Sum(x => x.Calories) : 0;
    public string StatusLabel => State switch
    {
        KitchenWeekDayState.OutsidePackage => "Paket kapsamı dışında",
        KitchenWeekDayState.AwaitingPayment => "Ödeme onayı bekleniyor",
        KitchenWeekDayState.PaymentFailed => "Ödeme tamamlanmadı",
        KitchenWeekDayState.Cancelled => "Abonelik iptal edildi",
        KitchenWeekDayState.Paused => "Abonelik duraklatıldı",
        KitchenWeekDayState.Preparing => "Plan hazırlanıyor",
        KitchenWeekDayState.Skipped => "Gün pas geçildi",
        _ => "Planlandı"
    };
}

public sealed record KitchenWeekMeal(KitchenMealSlot Slot, string ProductName, int Quantity, int Calories, bool IsSkipped);

public enum KitchenWeekDayState
{
    OutsidePackage, AwaitingPayment, PaymentFailed, Cancelled, Paused, Preparing, Skipped, Planned
}
