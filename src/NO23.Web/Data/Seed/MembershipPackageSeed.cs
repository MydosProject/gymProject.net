using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Data.Seed;

public static class MembershipPackageSeed
{
    public static IReadOnlyList<MembershipPackage> Defaults { get; } =
    [
        new()
        {
            Code = MembershipPackageCode.Start,
            Name = "START",
            Audience = "Yeni başlayanlar için",
            Description = "Haftada 2 ders, başlangıç ölçümü ve kişisel antrenman programı.",
            WeeklyClassLimit = 2,
            IncludesMeasurement = true,
            DisplayOrder = 1
        },
        new()
        {
            Code = MembershipPackageCode.Plus,
            Name = "PLUS",
            Audience = "Düzenli spor yapanlar için",
            Description = "Haftada 3 ders, vücut analizi ve beslenme önerisi.",
            WeeklyClassLimit = 3,
            IncludesBodyAnalysis = true,
            IncludesNutritionSupport = true,
            DisplayOrder = 2
        },
        new()
        {
            Code = MembershipPackageCode.Pro,
            Name = "PRO",
            Audience = "Net hedefleri olanlar için",
            Description = "Haftada 4 ders, detaylı takip, aylık analiz ve öncelikli rezervasyon.",
            WeeklyClassLimit = 4,
            IncludesDetailedTracking = true,
            IncludesMonthlyAnalysis = true,
            IncludesPriorityReservation = true,
            DisplayOrder = 3
        },
        new()
        {
            Code = MembershipPackageCode.Elite,
            Name = "ELITE",
            Audience = "En kapsamlı NO23 deneyimi",
            Description = "Sınırsız grup dersleri, Personal Training desteği, NO23 Kitchen avantajları ve özel etkinlik davetleri.",
            WeeklyClassLimit = null,
            IncludesPersonalTrainingSupport = true,
            IncludesKitchenBenefits = true,
            IncludesPrivateEvents = true,
            IncludesCommunityMembership = true,
            DisplayOrder = 4
        }
    ];
}
