using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public static class KitchenMealSelection
{
    public static bool TryCreateMask(IEnumerable<KitchenMealSlot>? slots, out int mask)
    {
        mask = 0;
        if (slots is null) return false;
        foreach (var slot in slots)
        {
            if (!Enum.IsDefined(slot)) return false;
            mask |= 1 << ((int)slot - 1);
        }
        return mask > 0;
    }

    public static bool Contains(int mask, KitchenMealSlot slot) => (mask & (1 << ((int)slot - 1))) != 0;
    public static string Name(KitchenMealSlot slot) => slot switch
    {
        KitchenMealSlot.Breakfast => "Kahvaltı", KitchenMealSlot.MorningSnack => "Sabah ara öğünü",
        KitchenMealSlot.Lunch => "Öğle yemeği", KitchenMealSlot.AfternoonSnack => "Öğleden sonra ara öğünü",
        KitchenMealSlot.Dinner => "Akşam yemeği", _ => "Öğün"
    };
}
