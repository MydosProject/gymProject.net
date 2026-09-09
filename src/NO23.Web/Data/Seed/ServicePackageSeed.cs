using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Data.Seed;

public static class ServicePackageSeed
{
    // Return fresh entities; the database seeder attaches and assigns IDs to them.
    public static IReadOnlyList<ServicePackage> Defaults =>
    [
        Membership("hybrid", "HYBRID", "Antrenman ve beslenmede dengeli başlangıç", MembershipPackageCode.Plus, 1, false,
            "Ayda 4 birebir PT ve 8 Performance grup dersiyle düzenini kur. Kişisel program, aylık ölçüm ve gelişim takibini Kitchen avantajlarıyla tamamla.",
            4, 8, 5, 5, 0, [("Esnek", null, 13000m), ("3 Aylık", 3, 12500m), ("6 Aylık", 6, 12000m), ("12 Aylık", 12, 11500m)]),
        Membership("pro-membership", "PRO", "Düzenli çalışma, daha yakın takip", MembershipPackageCode.Pro, 2, true,
            "Ayda 8 birebir PT ve 8 Performance grup dersiyle hedeflerine düzenli çalış. Aylık vücut analizi ve kişisel program takibine Kitchen, Coffee ve Shop indirimlerini ekle.",
            8, 8, 10, 10, 5, [("Esnek", null, 20000m), ("3 Aylık", 3, 19000m), ("6 Aylık", 6, 18000m), ("12 Aylık", 12, 17000m)]),
        Membership("black", "BLACK", "Yoğun antrenman ve toparlanma desteği", MembershipPackageCode.Elite, 3, false,
            "Ayda 12 birebir PT ve 20 Performance grup dersiyle yoğun bir çalışma düzeni oluştur. Gelişim takibini ücretsiz buz odası kullanımı ve en yüksek Kitchen abonelik indirimiyle destekle.",
            12, 20, 15, 10, 5, [("Esnek", null, 27500m), ("3 Aylık", 3, 25000m), ("6 Aylık", 6, 23500m), ("12 Aylık", 12, 22000m)]),
        Pt("pt-flex", "FLEX", "Başla, harekete geç!", 1, false,
            "8 veya 12 birebir dersle antrenmana başla. Eğitmeninle hedefini belirle, hareket tekniğini geliştir ve sana uygun kişisel programla ilerle.", [(8, 14000m), (12, null)]),
        Pt("pt-routine", "ROUTINE", "Düzenli ol, sonucu takip et", 2, true,
            "24 veya 36 birebir dersle düzenli bir antrenman alışkanlığı oluştur. Kişisel programını gelişimine göre güncelle, ölçüm ve eğitmen takibiyle hedeflerine ilerle.", [(24, 36000m), (36, null)]),
        Pt("pt-commit", "COMMIT", "Uzun vadeli hedeflerine yatırım yap", 3, false,
            "50, 70 veya 100 birebir dersle uzun dönem gelişimine odaklan. Öncelikli randevu, kişisel program ve gelişim takibine ücretsiz beslenme desteği eşlik eder.", [(50, 70000m), (70, null), (100, null)]),
        Group("group-reformer", "REFORMER", "Kontrollü hareket, güçlü duruş", 1, false,
            "Ayda 8 reformer dersiyle core, denge, esneklik ve postür üzerine çalış. Küçük grupta eğitmen eşliğinde ilerle; yıllık pakette 2 ay hediye kazan.", [(8, 5000m)], false, false),
        Group("group-reformer-plus", "REFORMER PLUS", "Daha sık reformer, düzenli gelişim", 2, true,
            "Ayda 12 reformer dersiyle daha sık ve düzenli çalış. 6 aylık pakette 1 ay, 12 aylık pakette 2 ay hediye ile aynı ders haklarını kullanmaya devam et.", [(12, null)], false, true),
        Group("group-performance", "PERFORMANCE GRUP DERSLERİ", "Kuvvet, kondisyon ve hareket", 3, false,
            "Ayda 8, 12 veya 24 dersle kuvvet ve kondisyonunu geliştir. Bootcamp, Metcon, Kalça & Crunch ve Mat Pilates dersleri arasından programını oluştur. Yıllık pakette 2 ay hediye.", [(8, 6000m), (12, 8400m), (24, 15600m)], true, false),
        Kids()
    ];

    private static ServicePackage Membership(string slug, string name, string subtitle, MembershipPackageCode code,
        int order, bool featured, string description, int pt, int performance, int kitchen, int coffee, int shop,
        IReadOnlyList<(string Name, int? Months, decimal Price)> prices)
    {
        var package = Base(slug, ServicePackageCategory.Membership, name, subtitle, description, order, featured);
        package.MembershipPackage = new MembershipPackage { Code = code };
        package.Features = Features("Aylık vücut analizi ve ölçüm", "Kişisel antrenman programı ve gelişim takibi");
        package.KitchenDiscountPercent = kitchen;
        package.CoffeeDiscountPercent = coffee;
        package.ShopDiscountPercent = shop;
        package.IncludesRecoveryRoom = slug == "black";
        foreach (var price in prices)
            package.Variants.Add(new ServicePackageVariant
            {
                Name = price.Name, BillingType = ServicePackageBillingType.MonthlySubscription,
                DurationMonths = price.Months, MonthlyPrice = price.Price, TotalPrice = price.Price * (price.Months ?? 1),
                PersonalTrainingSessionCount = pt, PerformanceClassCreditCount = performance, LessonsRenewMonthly = true,
                DisplayOrder = package.Variants.Count + 1, IsRecommended = price.Months == 6
            });
        return package;
    }

    private static ServicePackage Pt(string slug, string name, string subtitle, int order, bool featured,
        string description, IReadOnlyList<(int Lessons, decimal? Price)> variants)
    {
        var package = Base(slug, ServicePackageCategory.PersonalTraining, name, subtitle, description, order, featured);
        package.Features = Features("Birebir eğitmen eşliğinde antrenman", "Hedefine uygun kişisel program", "Hareket tekniği ve gelişim takibi");
        if (slug == "pt-routine") AddFeature(package, "Düzenli ölçüm ve program güncellemesi");
        if (slug == "pt-commit")
        {
            AddFeature(package, "Öncelikli randevu");
            AddFeature(package, "Ücretsiz beslenme desteği");
        }
        foreach (var option in variants)
            package.Variants.Add(new ServicePackageVariant
            {
                Name = $"{option.Lessons} Ders", BillingType = ServicePackageBillingType.OneTime,
                PersonalTrainingSessionCount = option.Lessons, TotalPrice = option.Price ?? 0,
                PriceOnRequest = !option.Price.HasValue, DisplayOrder = package.Variants.Count + 1
            });
        return package;
    }

    private static ServicePackage Group(string slug, string name, string subtitle, int order, bool featured,
        string description, IReadOnlyList<(int Lessons, decimal? Price)> variants, bool performance, bool sixMonths)
    {
        var package = Base(slug, ServicePackageCategory.GroupClasses, name, subtitle, description, order, featured);
        package.Features = performance
            ? Features("Bootcamp", "Metcon", "Kalça & Crunch", "Mat Pilates", "Yıllık pakette +2 ay hediye")
            : Features("Eğitmen eşliğinde reformer", "Core, denge, esneklik ve postür çalışması", "Yıllık pakette +2 ay hediye");
        if (sixMonths) AddFeature(package, "6 aylık pakette +1 ay hediye");
        AddGroupVariants(package, variants, performance, false, sixMonths);
        return package;
    }

    private static ServicePackage Kids()
    {
        var package = Base("kids-club", ServicePackageCategory.KidsClub, "KIDS CLUB", "6–14 yaş için hareket ve gelişim",
            "6–14 yaş arası çocuklar için ayda 8, 12 veya 24 ders. Yaşa uygun egzersizlerle duruş, koordinasyon ve hareket becerilerini güvenli bir ortamda geliştir. Yıllık pakette 2 ay hediye.", 1, true);
        package.Features = Features("6–14 yaş arası çocuklara uygun", "Postür ve koordinasyon çalışmaları", "Yaşa uygun egzersiz planı", "Güvenli ve eğlenceli ortam", "Yıllık pakette +2 ay hediye");
        AddGroupVariants(package, [(8, 5000m), (12, 7200m), (24, 13800m)], false, true, false);
        return package;
    }

    private static void AddGroupVariants(ServicePackage package, IReadOnlyList<(int Lessons, decimal? Price)> options,
        bool performance, bool kids, bool sixMonths)
    {
        foreach (var option in options)
        {
            Add(1, 0, option.Price);
            if (sixMonths) Add(6, 1, null);
            Add(12, 2, null);
            void Add(int months, int bonus, decimal? price) => package.Variants.Add(new ServicePackageVariant
            {
                Name = months == 1 ? $"{option.Lessons} Ders" : $"Ayda {option.Lessons} Ders · {months} Ay + {bonus} Ay Hediye",
                BillingType = ServicePackageBillingType.OneTime, DurationMonths = months, BonusMonths = bonus,
                LessonsRenewMonthly = true, TotalPrice = price ?? 0, PriceOnRequest = !price.HasValue,
                ReformerClassCreditCount = !performance && !kids ? option.Lessons : 0,
                PerformanceClassCreditCount = performance ? option.Lessons : 0, KidsClassCreditCount = kids ? option.Lessons : 0,
                DisplayOrder = package.Variants.Count + 1
            });
        }
    }

    private static ServicePackage Base(string slug, ServicePackageCategory category, string name, string subtitle,
        string description, int order, bool featured) => new()
        { Slug = slug, Category = category, Name = name, Subtitle = subtitle, Description = description,
          DisplayOrder = order, IsFeatured = featured, IsActive = true };
    private static List<ServicePackageFeature> Features(params string[] texts) => texts
        .Select((text, index) => new ServicePackageFeature { Text = text, DisplayOrder = index + 1 }).ToList();
    private static void AddFeature(ServicePackage package, string text) => package.Features.Add(new ServicePackageFeature
        { Text = text, DisplayOrder = package.Features.Count + 1 });
}
