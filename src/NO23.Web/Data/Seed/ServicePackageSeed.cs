using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Data.Seed;

public static class ServicePackageSeed
{
    // Return fresh entities; the database seeder attaches and assigns IDs to them.
    public static IReadOnlyList<ServicePackage> Defaults =>
    [
        Membership("hybrid", "HYBRID", "Antrenman ve beslenmede dengeli başlangıç", MembershipPackageCode.Plus, 1, false,
            "Ayda 4 birebir PT ve 8 Performance grup dersiyle düzenini kur. Kişisel program, aylık ölçüm ve gelişim takibini Kitchen avantajıyla tamamla.",
            4, 8, 5, 0, 0, [("Esnek", null, 13000m), ("3 Aylık", 3, 12500m), ("6 Aylık", 6, 12000m), ("12 Aylık", 12, 11500m)]),
        Membership("pro-membership", "PRO", "Düzenli çalışma, daha yakın takip", MembershipPackageCode.Pro, 2, true,
            "Ayda 8 birebir PT ve 8 Performance grup dersiyle hedeflerine düzenli çalış. Aylık vücut analizi ve kişisel program takibine Kitchen, Coffee ve Supplement indirimlerini ekle.",
            8, 8, 10, 10, 5, [("Esnek", null, 20000m), ("3 Aylık", 3, 19000m), ("6 Aylık", 6, 18000m), ("12 Aylık", 12, 17000m)]),
        Membership("black", "BLACK", "Yoğun antrenman ve toparlanma desteği", MembershipPackageCode.Elite, 3, false,
            "Ayda 12 birebir PT, geniş Performance erişimi ve premium ayrıcalıklarla yoğun bir çalışma düzeni oluştur. Gelişim takibini Recovery Room ve özel etkinliklerle destekle.",
            12, 0, 15, 15, 5, [("Esnek", null, 27500m), ("3 Aylık", 3, 25000m), ("6 Aylık", 6, 23500m), ("12 Aylık", 12, 22000m)]),

        Pt("pt-flex", "FLEX", "Başla, harekete geç!", 1, false,
            "8 veya 12 birebir dersle antrenmana başla. Eğitmeninle hedefini belirle, hareket tekniğini geliştir ve sana uygun kişisel programla ilerle.", [(8, 14000m), (12, 19500m)]),
        Pt("pt-routine", "ROUTINE", "Düzenli ol, sonucu takip et", 2, true,
            "24 veya 36 birebir dersle düzenli bir antrenman alışkanlığı oluştur. Kişisel programını gelişimine göre güncelle, ölçüm ve eğitmen takibiyle hedeflerine ilerle.", [(24, 36000m), (36, 54000m)]),
        Pt("pt-commit", "COMMIT", "Uzun vadeli hedeflerine yatırım yap", 3, false,
            "50, 70 veya 100 birebir dersle uzun dönem gelişimine odaklan. Kişisel programa ücretsiz beslenme danışmanlığı ve Tanita ölçümü eşlik eder.", [(50, 70000m), (70, 98000m), (100, 130000m)]),
        DuetPt(),

        Group("group-reformer", "REFORMER", "Kontrollü hareket, güçlü duruş", 1, false,
            "4 kişilik butik grupta core, denge, esneklik ve postür üzerine çalış.",
            Features("4 kişilik butik grup", "Eğitmen eşliğinde reformer", "Core, denge, esneklik ve postür çalışması", "6 aylık paketin 1 ayı hediye", "Yıllık paketin 2 ayı hediye"),
            [
                new("8 Ders", 1, 0, 5000m, 8, 0, false, false),
                new("6 Aylık (Ayda 8 Ders)", 6, 1, 25000m, 8, 0, true, false),
                new("Yıllık (Ayda 8 Ders)", 12, 2, 50000m, 8, 0, true, false)
            ]),
        Group("group-reformer-plus", "REFORMER PLUS", "Reformer ve Performance bir arada", 2, true,
            "Ayda 8 Reformer ve 4 Performance dersiyle toplam 12 derslik dengeli bir program oluştur.",
            Features("Ayda 8 Reformer + 4 Performance", "Toplam 12 ders", "6 aylık paketin 1 ayı hediye", "Yıllık paketin 2 ayı hediye"),
            [
                new("8 Reformer + 4 Performance", 1, 0, 7500m, 8, 4, false, true),
                new("6 Aylık", 6, 1, 37500m, 8, 4, true, false),
                new("Yıllık", 12, 2, 75000m, 8, 4, true, false)
            ]),
        Group("group-performance", "PERFORMANCE GRUP DERSLERİ", "Kuvvet, kondisyon ve hareket", 3, false,
            "Bootcamp, Metcon, Kalça & Crunch ve Mat Pilates dersleri arasından programını oluştur.",
            Features("Bootcamp", "Metcon", "Kalça & Crunch", "Mat Pilates", "6 aylık paketin 1 ayı hediye", "Yıllık paketin 2 ayı hediye"),
            [
                new("8 Ders", 1, 0, 6000m, 0, 8, false, false),
                new("8 Ders · 6 Aylık", 6, 1, 30000m, 0, 8, true, false),
                new("8 Ders · Yıllık", 12, 2, 60000m, 0, 8, true, false),
                new("12 Ders", 1, 0, 8400m, 0, 12, false, true),
                new("12 Ders · 6 Aylık", 6, 1, 42000m, 0, 12, true, true),
                new("12 Ders · Yıllık", 12, 2, 84000m, 0, 12, true, true),
                new("24 Ders · 3 Aylık", 3, 0, 15600m, 0, 24, false, false)
            ]),
        Kids()
    ];

    private static ServicePackage Membership(string slug, string name, string subtitle, MembershipPackageCode code,
        int order, bool featured, string description, int pt, int performance, int kitchen, int coffee, int shop,
        IReadOnlyList<(string Name, int? Months, decimal Price)> prices)
    {
        var package = Base(slug, ServicePackageCategory.Membership, name, subtitle, description, order, featured);
        package.MembershipPackage = new MembershipPackage { Code = code };
        package.Features = Features("Aylık vücut analizi ve ölçüm", "Kişisel antrenman programı ve gelişim takibi");
        if (slug == "pro-membership") AddFeature(package, "Öncelikli rezervasyon");
        if (slug == "black")
        {
            AddFeature(package, "Geniş Performance erişimi");
            AddFeature(package, "Özel etkinliklere erişim");
        }
        package.KitchenDiscountPercent = kitchen;
        package.CoffeeDiscountPercent = coffee;
        package.ShopDiscountPercent = shop;
        package.IncludesRecoveryRoom = slug == "black";
        foreach (var price in prices)
            package.Variants.Add(new ServicePackageVariant
            {
                Name = price.Name,
                BillingType = ServicePackageBillingType.MonthlySubscription,
                DurationMonths = price.Months,
                MonthlyPrice = price.Price,
                TotalPrice = price.Price * (price.Months ?? 1),
                PersonalTrainingSessionCount = pt,
                PerformanceClassCreditCount = performance,
                LessonsRenewMonthly = true,
                DisplayOrder = package.Variants.Count + 1
            });
        return package;
    }

    private static ServicePackage Pt(string slug, string name, string subtitle, int order, bool featured,
        string description, IReadOnlyList<(int Lessons, decimal Price)> variants)
    {
        var package = Base(slug, ServicePackageCategory.PersonalTraining, name, subtitle, description, order, featured);
        package.Features = Features("Birebir eğitmen eşliğinde antrenman", "Hedefine uygun kişisel program", "Hareket tekniği ve gelişim takibi");
        if (slug == "pt-routine") AddFeature(package, "Düzenli ölçüm ve program güncellemesi");
        if (slug == "pt-commit")
        {
            AddFeature(package, "Ücretsiz beslenme danışmanlığı");
            AddFeature(package, "Ücretsiz Tanita ölçümü");
        }
        foreach (var option in variants)
            AddPtVariant(package, option.Lessons, option.Price, option.Lessons == 24);
        return package;
    }

    private static ServicePackage DuetPt()
    {
        var package = Base("duet-pt", ServicePackageCategory.PersonalTraining, "DÜET PERSONAL TRAINING",
            "İki kişi birlikte, eğitmen eşliğinde", "İki kişinin birlikte katıldığı birebir formatta, kişi başı fiyatlandırılan Personal Training paketleri.", 4, false);
        package.Features = Features("2 kişi birlikte antrenman", "Fiyatlar kişi başıdır", "Hedefe uygun ortak program", "50 ders ve üzeri paketlerde ücretsiz beslenme danışmanlığı ve Tanita ölçümü");
        foreach (var option in new[] { (8, 10000m), (12, 14400m), (24, 27600m), (36, 41400m), (50, 55000m), (70, 77000m), (100, 100000m) })
            AddPtVariant(package, option.Item1, option.Item2, option.Item1 == 24, "Kişi Başı");
        return package;
    }

    private static void AddPtVariant(ServicePackage package, int lessons, decimal price, bool recommended, string? suffix = null) =>
        package.Variants.Add(new ServicePackageVariant
        {
            Name = suffix is null ? $"{lessons} Ders" : $"{lessons} Ders · {suffix}",
            BillingType = ServicePackageBillingType.OneTime,
            PersonalTrainingSessionCount = lessons,
            TotalPrice = price,
            IsRecommended = recommended,
            DisplayOrder = package.Variants.Count + 1
        });

    private static ServicePackage Group(string slug, string name, string subtitle, int order, bool featured,
        string description, List<ServicePackageFeature> features, IReadOnlyList<GroupVariantDefinition> variants)
    {
        var package = Base(slug, ServicePackageCategory.GroupClasses, name, subtitle, description, order, featured);
        package.Features = features;
        foreach (var option in variants) AddGroupVariant(package, option, false);
        return package;
    }

    private static ServicePackage Kids()
    {
        var package = Base("kids-club", ServicePackageCategory.KidsClub, "KIDS CLUB", "6–14 yaş için hareket ve gelişim",
            "6–14 yaş arası çocuklar için postür analizi dahil, ayda 8 derslik aylık, 6 aylık ve yıllık paketler.", 1, true);
        package.Features = Features("6–14 yaş arası çocuklara uygun", "Postür analizi dahil", "Yaşa uygun egzersiz planı", "Aynı aileden ikinci çocuk için %25 kardeş indirimi", "6 aylık paketin 1 ayı hediye", "Yıllık paketin 2 ayı hediye");
        foreach (var option in new GroupVariantDefinition[]
        {
            new("8 Ders", 1, 0, 5000m, 0, 8, false, false),
            new("8 Ders · 6 Aylık", 6, 1, 25000m, 0, 8, true, false),
            new("8 Ders · Yıllık", 12, 2, 50000m, 0, 8, true, false)
        }) AddGroupVariant(package, option, true);
        return package;
    }

    private static void AddGroupVariant(ServicePackage package, GroupVariantDefinition option, bool kids) =>
        package.Variants.Add(new ServicePackageVariant
        {
            Name = option.Name,
            BillingType = ServicePackageBillingType.OneTime,
            DurationMonths = option.DurationMonths,
            BonusMonths = option.BonusMonths,
            LessonsRenewMonthly = option.RenewsMonthly,
            TotalPrice = option.Price,
            ReformerClassCreditCount = kids ? 0 : option.ReformerLessons,
            PerformanceClassCreditCount = kids ? 0 : option.PerformanceOrKidsLessons,
            KidsClassCreditCount = kids ? option.PerformanceOrKidsLessons : 0,
            IsRecommended = option.IsRecommended,
            DisplayOrder = package.Variants.Count + 1
        });

    private static ServicePackage Base(string slug, ServicePackageCategory category, string name, string subtitle,
        string description, int order, bool featured) => new()
        {
            Slug = slug,
            Category = category,
            Name = name,
            Subtitle = subtitle,
            Description = description,
            DisplayOrder = order,
            IsFeatured = featured,
            IsActive = true
        };

    private static List<ServicePackageFeature> Features(params string[] texts) => texts
        .Select((text, index) => new ServicePackageFeature { Text = text, DisplayOrder = index + 1 }).ToList();

    private static void AddFeature(ServicePackage package, string text) => package.Features.Add(new ServicePackageFeature
        { Text = text, DisplayOrder = package.Features.Count + 1 });

    private sealed record GroupVariantDefinition(string Name, int DurationMonths, int BonusMonths, decimal Price,
        int ReformerLessons, int PerformanceOrKidsLessons, bool RenewsMonthly, bool IsRecommended);
}
