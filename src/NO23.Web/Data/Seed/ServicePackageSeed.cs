using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Data.Seed;

public static class ServicePackageSeed
{
    public static IReadOnlyList<ServicePackage> Defaults { get; } =
    [
        Membership("hybrid", "HYBRID", "Dengeli başlangıç", MembershipPackageCode.Plus, 1, false,
            4, 8, [("Esnek", null, 13000m), ("3 Aylık", 3, 12500m), ("6 Aylık", 6, 12000m), ("12 Aylık", 12, 11500m)]),
        Membership("pro-membership", "PRO", "En çok tercih edilen", MembershipPackageCode.Pro, 2, true,
            8, 8, [("Esnek", null, 20000m), ("3 Aylık", 3, 19000m), ("6 Aylık", 6, 18000m), ("12 Aylık", 12, 17000m)]),
        Membership("black", "BLACK", "Sınırsız performans", MembershipPackageCode.Elite, 3, false,
            12, 20, [("Esnek", null, 27500m), ("3 Aylık", 3, 25000m), ("6 Aylık", 6, 23500m), ("12 Aylık", 12, 22000m)]),

        OneTime("pt-flex", ServicePackageCategory.PersonalTraining, "FLEX", "Başla, harekete geç!", 1, false,
            "Kişiye özel program ve birebir koçlukla güçlü bir başlangıç.", 8, 0, 0, 0, 0, 14000m),
        OneTime("pt-routine", ServicePackageCategory.PersonalTraining, "ROUTINE", "Düzenli ol, sonucu al!", 2, true,
            "Düzenli takip ve gelişim ölçümleriyle hedef odaklı PT programı.", 24, 0, 0, 0, 0, 36000m),
        OneTime("pt-commit", ServicePackageCategory.PersonalTraining, "COMMIT", "Kendine yatırım yap", 3, false,
            "Uzun dönem birebir çalışma, öncelikli randevu ve gelişim takibi.", 50, 0, 0, 0, 0, 70000m),

        OneTime("group-reformer", ServicePackageCategory.GroupClasses, "REFORMER", "4 kişilik butik grup", 1, false,
            "Core, denge, esneklik ve postür gelişimine odaklanan küçük grup deneyimi.", 0, 8, 0, 0, 0, 5000m),
        OneTime("group-reformer-plus", ServicePackageCategory.GroupClasses, "REFORMER PLUS", "Reformer + Performance", 2, true,
            "Reformer ve performans derslerini birleştiren dengeli paket.", 0, 8, 4, 0, 0, 7500m),
        PerformanceGroup(),

        KidsClub()
    ];

    private static ServicePackage Membership(string slug, string name, string subtitle,
        MembershipPackageCode membershipCode, int order, bool featured, int pt, int performance,
        IReadOnlyList<(string Name, int? Months, decimal MonthlyPrice)> prices)
    {
        var package = Base(slug, ServicePackageCategory.Membership, name, subtitle,
            "Aylık PT, performans grup dersleri, ölçüm ve gelişim takibini bir araya getiren üyelik.", order, featured);
        package.MembershipPackage = new MembershipPackage { Code = membershipCode };
        package.Features = Features("Aylık vücut analizi ve ölçüm", "Kişisel antrenman programı", "Gelişim takibi", "Üyelere özel ayrıcalıklar");
        var variantOrder = 1;
        foreach (var price in prices)
            package.Variants.Add(new ServicePackageVariant
            {
                Name = price.Name, BillingType = ServicePackageBillingType.MonthlySubscription,
                DurationMonths = price.Months, MonthlyPrice = price.MonthlyPrice,
                TotalPrice = price.Months.HasValue ? price.MonthlyPrice * price.Months.Value : price.MonthlyPrice,
                PersonalTrainingSessionCount = pt, PerformanceClassCreditCount = performance,
                IncludesGymAccess = true, IsRecommended = price.Months == 6,
                DisplayOrder = variantOrder++
            });
        return package;
    }

    private static ServicePackage OneTime(string slug, ServicePackageCategory category, string name,
        string subtitle, int order, bool featured, string description, int pt, int reformer,
        int performance, int group, int kids, decimal price)
    {
        var package = Base(slug, category, name, subtitle, description, order, featured);
        package.Features = Features("Kişiye özel planlama", "Düzenli gelişim takibi", "Uzman eğitmen desteği");
        package.Variants.Add(new ServicePackageVariant
        {
            Name = pt > 0 ? $"{pt} Ders" : reformer + performance + group + kids > 0 ? $"{reformer + performance + group + kids} Ders" : "Standart",
            BillingType = ServicePackageBillingType.OneTime, TotalPrice = price,
            PersonalTrainingSessionCount = pt, ReformerClassCreditCount = reformer,
            PerformanceClassCreditCount = performance, GroupClassCreditCount = group,
            KidsClassCreditCount = kids, DisplayOrder = 1, IsRecommended = featured
        });
        return package;
    }

    private static ServicePackage PerformanceGroup()
    {
        var package = Base("group-performance", ServicePackageCategory.GroupClasses,
            "PERFORMANCE GRUP DERSLERİ", "Enerjik grup", "Kondisyon, dayanıklılık ve kuvvet için yüksek enerjili grup dersleri.", 3, false);
        package.Features = Features("Uzman eğitmenler", "Esnek rezervasyon", "Kondisyon ve kuvvet gelişimi");
        AddLessonVariants(package, [(8, 6000m), (12, 8400m), (24, 15600m)], performance: true);
        return package;
    }

    private static ServicePackage KidsClub()
    {
        var package = Base("kids-club", ServicePackageCategory.KidsClub, "KIDS CLUB",
            "Postürü güçlendir, hareketi doğru temelle başlat", "Yaşa uygun hareket, koordinasyon ve güvenli gelişim programı.", 1, true);
        package.Features = Features("Postür analizi ile başlangıç", "Duruş ve koordinasyon gelişimi", "Yaşa uygun egzersiz planı", "Güvenli ve eğlenceli ortam");
        AddLessonVariants(package, [(8, 5000m), (12, 7200m), (24, 13800m)], performance: false);
        return package;
    }

    private static void AddLessonVariants(ServicePackage package, IReadOnlyList<(int Lessons, decimal Price)> variants, bool performance)
    {
        var order = 1;
        foreach (var variant in variants)
            package.Variants.Add(new ServicePackageVariant
            {
                Name = $"{variant.Lessons} Ders", BillingType = ServicePackageBillingType.OneTime,
                TotalPrice = variant.Price,
                PerformanceClassCreditCount = performance ? variant.Lessons : 0,
                KidsClassCreditCount = performance ? 0 : variant.Lessons,
                IsRecommended = variant.Lessons == 12, DisplayOrder = order++
            });
    }

    private static ServicePackage Base(string slug, ServicePackageCategory category, string name,
        string subtitle, string description, int order, bool featured) => new()
        { Slug = slug, Category = category, Name = name, Subtitle = subtitle, Description = description,
          DisplayOrder = order, IsFeatured = featured, IsActive = true };

    private static List<ServicePackageFeature> Features(params string[] texts) => texts
        .Select((text, index) => new ServicePackageFeature { Text = text, DisplayOrder = index + 1 }).ToList();
}
