using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NO23.Web.Data;

#nullable disable

namespace NO23.Web.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260911120000_SynchronizePublishedPackagePrices")]
public partial class SynchronizePublishedPackagePrices : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
INSERT INTO "ServicePackages"
    ("Category", "Slug", "Name", "Subtitle", "Description", "IsFeatured", "KitchenDiscountPercent",
     "CoffeeDiscountPercent", "ShopDiscountPercent", "IncludesRecoveryRoom", "IsActive", "DisplayOrder",
     "MembershipPackageId", "CreatedAtUtc", "UpdatedAtUtc")
VALUES
    ('PersonalTraining', 'duet-pt', 'DÜET PERSONAL TRAINING', 'İki kişi birlikte, eğitmen eşliğinde',
     'İki kişinin birlikte katıldığı birebir formatta, kişi başı fiyatlandırılan Personal Training paketleri.',
     FALSE, 0, 0, 0, FALSE, TRUE, 4, NULL, NOW(), NOW())
ON CONFLICT ("Slug") DO NOTHING;

WITH desired("Slug", "Name", "Subtitle", "Description", "IsFeatured", "Kitchen", "Coffee", "Shop", "Recovery", "DisplayOrder") AS (
    VALUES
    ('hybrid', 'HYBRID', 'Antrenman ve beslenmede dengeli başlangıç', 'Ayda 4 birebir PT ve 8 Performance grup dersiyle düzenini kur. Kişisel program, aylık ölçüm ve gelişim takibini Kitchen avantajıyla tamamla.', FALSE, 5, 0, 0, FALSE, 1),
    ('pro-membership', 'PRO', 'Düzenli çalışma, daha yakın takip', 'Ayda 8 birebir PT ve 8 Performance grup dersiyle hedeflerine düzenli çalış. Aylık vücut analizi ve kişisel program takibine Kitchen, Coffee ve Supplement indirimlerini ekle.', TRUE, 10, 10, 5, FALSE, 2),
    ('black', 'BLACK', 'Yoğun antrenman ve toparlanma desteği', 'Ayda 12 birebir PT, geniş Performance erişimi ve premium ayrıcalıklarla yoğun bir çalışma düzeni oluştur. Gelişim takibini Recovery Room ve özel etkinliklerle destekle.', FALSE, 15, 15, 5, TRUE, 3),
    ('pt-flex', 'FLEX', 'Başla, harekete geç!', '8 veya 12 birebir dersle antrenmana başla. Eğitmeninle hedefini belirle, hareket tekniğini geliştir ve sana uygun kişisel programla ilerle.', FALSE, 0, 0, 0, FALSE, 1),
    ('pt-routine', 'ROUTINE', 'Düzenli ol, sonucu takip et', '24 veya 36 birebir dersle düzenli bir antrenman alışkanlığı oluştur. Kişisel programını gelişimine göre güncelle, ölçüm ve eğitmen takibiyle hedeflerine ilerle.', TRUE, 0, 0, 0, FALSE, 2),
    ('pt-commit', 'COMMIT', 'Uzun vadeli hedeflerine yatırım yap', '50, 70 veya 100 birebir dersle uzun dönem gelişimine odaklan. Kişisel programa ücretsiz beslenme danışmanlığı ve Tanita ölçümü eşlik eder.', FALSE, 0, 0, 0, FALSE, 3),
    ('duet-pt', 'DÜET PERSONAL TRAINING', 'İki kişi birlikte, eğitmen eşliğinde', 'İki kişinin birlikte katıldığı birebir formatta, kişi başı fiyatlandırılan Personal Training paketleri.', FALSE, 0, 0, 0, FALSE, 4),
    ('group-reformer', 'REFORMER', 'Kontrollü hareket, güçlü duruş', '4 kişilik butik grupta core, denge, esneklik ve postür üzerine çalış.', FALSE, 0, 0, 0, FALSE, 1),
    ('group-reformer-plus', 'REFORMER PLUS', 'Reformer ve Performance bir arada', 'Ayda 8 Reformer ve 4 Performance dersiyle toplam 12 derslik dengeli bir program oluştur.', TRUE, 0, 0, 0, FALSE, 2),
    ('group-performance', 'PERFORMANCE GRUP DERSLERİ', 'Kuvvet, kondisyon ve hareket', 'Bootcamp, Metcon, Kalça & Crunch ve Mat Pilates dersleri arasından programını oluştur.', FALSE, 0, 0, 0, FALSE, 3),
    ('kids-club', 'KIDS CLUB', '6–14 yaş için hareket ve gelişim', '6–14 yaş arası çocuklar için postür analizi dahil 8, 12 ve 24 ders seçenekleri.', TRUE, 0, 0, 0, FALSE, 1)
)
UPDATE "ServicePackages" p
SET "Name" = d."Name", "Subtitle" = d."Subtitle", "Description" = d."Description",
    "IsFeatured" = d."IsFeatured", "KitchenDiscountPercent" = d."Kitchen",
    "CoffeeDiscountPercent" = d."Coffee", "ShopDiscountPercent" = d."Shop",
    "IncludesRecoveryRoom" = d."Recovery", "IsActive" = TRUE, "DisplayOrder" = d."DisplayOrder",
    "UpdatedAtUtc" = NOW()
FROM desired d
WHERE p."Slug" = d."Slug";

UPDATE "ServicePackageVariants" v
SET "IsActive" = FALSE, "UpdatedAtUtc" = NOW()
FROM "ServicePackages" p
WHERE v."ServicePackageId" = p."Id"
  AND p."Slug" IN ('hybrid', 'pro-membership', 'black', 'pt-flex', 'pt-routine', 'pt-commit', 'duet-pt',
                   'group-reformer', 'group-reformer-plus', 'group-performance', 'kids-club');

WITH desired("Slug", "Name", "BillingType", "DurationMonths", "BonusMonths", "LessonsRenewMonthly",
             "MonthlyPrice", "TotalPrice", "Pt", "Reformer", "Performance", "Kids", "Recommended", "DisplayOrder") AS (
    VALUES
    ('hybrid', 'Esnek', 'MonthlySubscription', NULL::integer, 0, TRUE, 13000::numeric, 13000::numeric, 4, 0, 8, 0, FALSE, 1),
    ('hybrid', '3 Aylık', 'MonthlySubscription', 3, 0, TRUE, 12500, 37500, 4, 0, 8, 0, FALSE, 2),
    ('hybrid', '6 Aylık', 'MonthlySubscription', 6, 0, TRUE, 12000, 72000, 4, 0, 8, 0, FALSE, 3),
    ('hybrid', '12 Aylık', 'MonthlySubscription', 12, 0, TRUE, 11500, 138000, 4, 0, 8, 0, FALSE, 4),
    ('pro-membership', 'Esnek', 'MonthlySubscription', NULL, 0, TRUE, 20000, 20000, 8, 0, 8, 0, FALSE, 1),
    ('pro-membership', '3 Aylık', 'MonthlySubscription', 3, 0, TRUE, 19000, 57000, 8, 0, 8, 0, FALSE, 2),
    ('pro-membership', '6 Aylık', 'MonthlySubscription', 6, 0, TRUE, 18000, 108000, 8, 0, 8, 0, FALSE, 3),
    ('pro-membership', '12 Aylık', 'MonthlySubscription', 12, 0, TRUE, 17000, 204000, 8, 0, 8, 0, FALSE, 4),
    ('black', 'Esnek', 'MonthlySubscription', NULL, 0, TRUE, 27500, 27500, 12, 0, 0, 0, FALSE, 1),
    ('black', '3 Aylık', 'MonthlySubscription', 3, 0, TRUE, 25000, 75000, 12, 0, 0, 0, FALSE, 2),
    ('black', '6 Aylık', 'MonthlySubscription', 6, 0, TRUE, 23500, 141000, 12, 0, 0, 0, FALSE, 3),
    ('black', '12 Aylık', 'MonthlySubscription', 12, 0, TRUE, 22000, 264000, 12, 0, 0, 0, FALSE, 4),
    ('pt-flex', '8 Ders', 'OneTime', NULL, 0, FALSE, NULL, 14000, 8, 0, 0, 0, FALSE, 1),
    ('pt-flex', '12 Ders', 'OneTime', NULL, 0, FALSE, NULL, 19500, 12, 0, 0, 0, FALSE, 2),
    ('pt-routine', '24 Ders', 'OneTime', NULL, 0, FALSE, NULL, 36000, 24, 0, 0, 0, TRUE, 1),
    ('pt-routine', '36 Ders', 'OneTime', NULL, 0, FALSE, NULL, 54000, 36, 0, 0, 0, FALSE, 2),
    ('pt-commit', '50 Ders', 'OneTime', NULL, 0, FALSE, NULL, 70000, 50, 0, 0, 0, FALSE, 1),
    ('pt-commit', '70 Ders', 'OneTime', NULL, 0, FALSE, NULL, 98000, 70, 0, 0, 0, FALSE, 2),
    ('pt-commit', '100 Ders', 'OneTime', NULL, 0, FALSE, NULL, 130000, 100, 0, 0, 0, FALSE, 3),
    ('duet-pt', '8 Ders · Kişi Başı', 'OneTime', NULL, 0, FALSE, NULL, 10000, 8, 0, 0, 0, FALSE, 1),
    ('duet-pt', '12 Ders · Kişi Başı', 'OneTime', NULL, 0, FALSE, NULL, 14400, 12, 0, 0, 0, FALSE, 2),
    ('duet-pt', '24 Ders · Kişi Başı', 'OneTime', NULL, 0, FALSE, NULL, 27600, 24, 0, 0, 0, TRUE, 3),
    ('duet-pt', '36 Ders · Kişi Başı', 'OneTime', NULL, 0, FALSE, NULL, 41400, 36, 0, 0, 0, FALSE, 4),
    ('duet-pt', '50 Ders · Kişi Başı', 'OneTime', NULL, 0, FALSE, NULL, 55000, 50, 0, 0, 0, FALSE, 5),
    ('duet-pt', '70 Ders · Kişi Başı', 'OneTime', NULL, 0, FALSE, NULL, 77000, 70, 0, 0, 0, FALSE, 6),
    ('duet-pt', '100 Ders · Kişi Başı', 'OneTime', NULL, 0, FALSE, NULL, 100000, 100, 0, 0, 0, FALSE, 7),
    ('group-reformer', '8 Ders', 'OneTime', 1, 0, FALSE, NULL, 5000, 0, 8, 0, 0, FALSE, 1),
    ('group-reformer', '6 Aylık (Ayda 8 Ders)', 'OneTime', 6, 1, TRUE, NULL, 25000, 0, 8, 0, 0, FALSE, 2),
    ('group-reformer', 'Yıllık (Ayda 8 Ders)', 'OneTime', 12, 2, TRUE, NULL, 50000, 0, 8, 0, 0, FALSE, 3),
    ('group-reformer-plus', '8 Reformer + 4 Performance', 'OneTime', 1, 0, FALSE, NULL, 7500, 0, 8, 4, 0, TRUE, 1),
    ('group-reformer-plus', '6 Aylık', 'OneTime', 6, 1, TRUE, NULL, 37500, 0, 8, 4, 0, FALSE, 2),
    ('group-reformer-plus', 'Yıllık', 'OneTime', 12, 2, TRUE, NULL, 75000, 0, 8, 4, 0, FALSE, 3),
    ('group-performance', '8 Ders', 'OneTime', 1, 0, FALSE, NULL, 6000, 0, 0, 8, 0, FALSE, 1),
    ('group-performance', '8 Ders · 6 Aylık', 'OneTime', 6, 1, TRUE, NULL, 30000, 0, 0, 8, 0, FALSE, 2),
    ('group-performance', '8 Ders · Yıllık', 'OneTime', 12, 2, TRUE, NULL, 60000, 0, 0, 8, 0, FALSE, 3),
    ('group-performance', '12 Ders', 'OneTime', 1, 0, FALSE, NULL, 8400, 0, 0, 12, 0, TRUE, 4),
    ('group-performance', '12 Ders · 6 Aylık', 'OneTime', 6, 1, TRUE, NULL, 42000, 0, 0, 12, 0, TRUE, 5),
    ('group-performance', '12 Ders · Yıllık', 'OneTime', 12, 2, TRUE, NULL, 84000, 0, 0, 12, 0, TRUE, 6),
    ('group-performance', '24 Ders · 3 Aylık', 'OneTime', 3, 0, FALSE, NULL, 15600, 0, 0, 24, 0, FALSE, 7),
    ('kids-club', '8 Ders', 'OneTime', 1, 0, FALSE, NULL, 5000, 0, 0, 0, 8, FALSE, 1),
    ('kids-club', '8 Ders · 6 Aylık', 'OneTime', 6, 1, TRUE, NULL, 25000, 0, 0, 0, 8, FALSE, 2),
    ('kids-club', '8 Ders · Yıllık', 'OneTime', 12, 2, TRUE, NULL, 50000, 0, 0, 0, 8, FALSE, 3),
    ('kids-club', '12 Ders', 'OneTime', 1, 0, FALSE, NULL, 7200, 0, 0, 0, 12, FALSE, 4),
    ('kids-club', '12 Ders · 6 Aylık', 'OneTime', 6, 1, TRUE, NULL, 36000, 0, 0, 0, 12, FALSE, 5),
    ('kids-club', '12 Ders · Yıllık', 'OneTime', 12, 2, TRUE, NULL, 72000, 0, 0, 0, 12, FALSE, 6),
    ('kids-club', '24 Ders · 3 Aylık', 'OneTime', 3, 0, FALSE, NULL, 13800, 0, 0, 0, 24, TRUE, 7)
)
INSERT INTO "ServicePackageVariants"
    ("ServicePackageId", "Name", "BillingType", "DurationMonths", "DurationDays", "BonusMonths",
     "LessonsRenewMonthly", "MonthlyPrice", "TotalPrice", "PriceOnRequest", "PersonalTrainingSessionCount",
     "ReformerClassCreditCount", "PerformanceClassCreditCount", "GroupClassCreditCount", "KidsClassCreditCount",
     "IncludesGymAccess", "IsRecommended", "IsActive", "DisplayOrder", "CreatedAtUtc", "UpdatedAtUtc")
SELECT p."Id", d."Name", d."BillingType", d."DurationMonths", NULL, d."BonusMonths",
       d."LessonsRenewMonthly", d."MonthlyPrice", d."TotalPrice", FALSE, d."Pt", d."Reformer", d."Performance",
       0, d."Kids", FALSE, d."Recommended", TRUE, d."DisplayOrder", NOW(), NOW()
FROM desired d
JOIN "ServicePackages" p ON p."Slug" = d."Slug"
ON CONFLICT ("ServicePackageId", "Name") DO UPDATE SET
    "BillingType" = EXCLUDED."BillingType", "DurationMonths" = EXCLUDED."DurationMonths",
    "DurationDays" = EXCLUDED."DurationDays", "BonusMonths" = EXCLUDED."BonusMonths",
    "LessonsRenewMonthly" = EXCLUDED."LessonsRenewMonthly", "MonthlyPrice" = EXCLUDED."MonthlyPrice",
    "TotalPrice" = EXCLUDED."TotalPrice", "PriceOnRequest" = FALSE,
    "PersonalTrainingSessionCount" = EXCLUDED."PersonalTrainingSessionCount",
    "ReformerClassCreditCount" = EXCLUDED."ReformerClassCreditCount",
    "PerformanceClassCreditCount" = EXCLUDED."PerformanceClassCreditCount",
    "GroupClassCreditCount" = 0, "KidsClassCreditCount" = EXCLUDED."KidsClassCreditCount",
    "IncludesGymAccess" = FALSE, "IsRecommended" = EXCLUDED."IsRecommended", "IsActive" = TRUE,
    "DisplayOrder" = EXCLUDED."DisplayOrder", "UpdatedAtUtc" = NOW();

DELETE FROM "ServicePackageFeatures" f
USING "ServicePackages" p
WHERE f."ServicePackageId" = p."Id"
  AND p."Slug" IN ('hybrid', 'pro-membership', 'black', 'pt-flex', 'pt-routine', 'pt-commit', 'duet-pt',
                   'group-reformer', 'group-reformer-plus', 'group-performance', 'kids-club');

WITH desired("Slug", "Text", "DisplayOrder") AS (
    VALUES
    ('hybrid', 'Aylık vücut analizi ve ölçüm', 1), ('hybrid', 'Kişisel antrenman programı ve gelişim takibi', 2),
    ('pro-membership', 'Aylık vücut analizi ve ölçüm', 1), ('pro-membership', 'Kişisel antrenman programı ve gelişim takibi', 2), ('pro-membership', 'Öncelikli rezervasyon', 3),
    ('black', 'Aylık vücut analizi ve ölçüm', 1), ('black', 'Kişisel antrenman programı ve gelişim takibi', 2), ('black', 'Geniş Performance erişimi', 3), ('black', 'Özel etkinliklere erişim', 4),
    ('pt-flex', 'Birebir eğitmen eşliğinde antrenman', 1), ('pt-flex', 'Hedefine uygun kişisel program', 2), ('pt-flex', 'Hareket tekniği ve gelişim takibi', 3),
    ('pt-routine', 'Birebir eğitmen eşliğinde antrenman', 1), ('pt-routine', 'Hedefine uygun kişisel program', 2), ('pt-routine', 'Hareket tekniği ve gelişim takibi', 3), ('pt-routine', 'Düzenli ölçüm ve program güncellemesi', 4),
    ('pt-commit', 'Birebir eğitmen eşliğinde antrenman', 1), ('pt-commit', 'Hedefine uygun kişisel program', 2), ('pt-commit', 'Hareket tekniği ve gelişim takibi', 3), ('pt-commit', 'Ücretsiz beslenme danışmanlığı', 4), ('pt-commit', 'Ücretsiz Tanita ölçümü', 5),
    ('duet-pt', '2 kişi birlikte antrenman', 1), ('duet-pt', 'Fiyatlar kişi başıdır', 2), ('duet-pt', 'Hedefe uygun ortak program', 3), ('duet-pt', '50 ders ve üzeri paketlerde ücretsiz beslenme danışmanlığı ve Tanita ölçümü', 4),
    ('group-reformer', '4 kişilik butik grup', 1), ('group-reformer', 'Eğitmen eşliğinde reformer', 2), ('group-reformer', 'Core, denge, esneklik ve postür çalışması', 3), ('group-reformer', '6 aylık paketin 1 ayı hediye', 4), ('group-reformer', 'Yıllık paketin 2 ayı hediye', 5),
    ('group-reformer-plus', 'Ayda 8 Reformer + 4 Performance', 1), ('group-reformer-plus', 'Toplam 12 ders', 2), ('group-reformer-plus', '6 aylık paketin 1 ayı hediye', 3), ('group-reformer-plus', 'Yıllık paketin 2 ayı hediye', 4),
    ('group-performance', 'Bootcamp', 1), ('group-performance', 'Metcon', 2), ('group-performance', 'Kalça & Crunch', 3), ('group-performance', 'Mat Pilates', 4), ('group-performance', '6 aylık paketin 1 ayı hediye', 5), ('group-performance', 'Yıllık paketin 2 ayı hediye', 6),
    ('kids-club', '6–14 yaş arası çocuklara uygun', 1), ('kids-club', 'Postür analizi dahil', 2), ('kids-club', 'Yaşa uygun egzersiz planı', 3), ('kids-club', 'Aynı aileden ikinci çocuk için %25 kardeş indirimi', 4), ('kids-club', '6 aylık paketin 1 ayı hediye', 5), ('kids-club', 'Yıllık paketin 2 ayı hediye', 6)
)
INSERT INTO "ServicePackageFeatures" ("ServicePackageId", "Text", "DisplayOrder")
SELECT p."Id", d."Text", d."DisplayOrder"
FROM desired d
JOIN "ServicePackages" p ON p."Slug" = d."Slug";
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Commercial catalog corrections are intentionally not reversed. Existing applications may reference
        // variants introduced here, so keeping the corrected data is safer than deleting historical records.
    }
}
