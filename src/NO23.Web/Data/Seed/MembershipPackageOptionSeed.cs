using NO23.Web.Domain.Enums;

namespace NO23.Web.Data.Seed;

public static class MembershipPackageOptionSeed
{
    public static IReadOnlyList<DefaultOption> Defaults { get; } =
        Enum.GetValues<MembershipPackageCode>()
            .SelectMany(code => BuildForPackage(code))
            .ToList();

    private static IEnumerable<DefaultOption> BuildForPackage(MembershipPackageCode code)
    {
        yield return new(code, "20 Gün Personal Training",
            "Birebir eğitmen takibiyle hedef odaklı 20 günlük çalışma düzeni.",
            20, 8, 0, true, 1);
        yield return new(code, "Sadece Grup Dersleri",
            "NO23 grup derslerine odaklanan, birebir PT içermeyen program.",
            30, 0, 12, false, 2);
        yield return new(code, "PT + Grup Dersleri",
            "Birebir antrenman ve grup derslerini aynı programda birleştiren karma seçenek.",
            30, 4, 8, true, 3);
    }

    public sealed record DefaultOption(
        MembershipPackageCode PackageCode,
        string Name,
        string Description,
        int DurationDays,
        int PersonalTrainingSessionCount,
        int GroupClassCreditCount,
        bool IncludesGymAccess,
        int DisplayOrder);
}
