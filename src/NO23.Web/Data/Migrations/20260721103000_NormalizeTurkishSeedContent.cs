using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NO23.Web.Data;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260721103000_NormalizeTurkishSeedContent")]
    public partial class NormalizeTurkishSeedContent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "ShopProducts"
                SET "Description" = 'Premium pamuk karışımlı oversize antrenman ve günlük kullanım hoodie.',
                    "Category" = 'Giyim',
                    "Tags" = 'hoodie, lifestyle, giyim',
                    "UpdatedAtUtc" = NOW()
                WHERE "Sku" = 'NO23-HOODIE-001';

                UPDATE "ShopProducts"
                SET "Description" = 'Nefes alan kumaşlı, antrenman odaklı NO23 t-shirt.',
                    "Category" = 'Giyim',
                    "Tags" = 't-shirt, antrenman, giyim',
                    "UpdatedAtUtc" = NOW()
                WHERE "Sku" = 'NO23-TSHIRT-001';

                UPDATE "ShopProducts"
                SET "Description" = 'Protein ve supplement karışımları için sızdırmaz shaker.',
                    "Category" = 'Aksesuar',
                    "Tags" = 'shaker, aksesuar',
                    "UpdatedAtUtc" = NOW()
                WHERE "Sku" = 'NO23-SHAKER-001';

                UPDATE "ShopProducts"
                SET "Name" = 'Direnç Bandı Seti',
                    "Description" = 'Isınma, mobilite ve kuvvet destek egzersizleri için direnç bandı seti.',
                    "Category" = 'Ekipman',
                    "Tags" = 'ekipman, mobilite, kuvvet',
                    "UpdatedAtUtc" = NOW()
                WHERE "Sku" = 'NO23-BAND-001';

                UPDATE "CommunityEvents"
                SET "Title" = 'NO23 Run Club Sabah Seansı',
                    "Summary" = 'Tempo kontrollü sabah koşusu ve mobilite çalışması.',
                    "Description" = 'NO23 community üyeleri için haftalık sabah koşusu, ısınma ve cooldown rutini.',
                    "UpdatedAtUtc" = NOW()
                WHERE "Slug" = 'no23-run-club-morning-session';

                UPDATE "CommunityEvents"
                SET "Title" = 'Performans Beslenmesi Workshop',
                    "Summary" = 'Antrenman günlerinde makro planlama ve pratik öğün seçimi.',
                    "Description" = 'NO23 Kitchen ekibiyle performans beslenmesi, kalori hedefleri ve pratik menüler üzerine workshop.',
                    "UpdatedAtUtc" = NOW()
                WHERE "Slug" = 'performance-nutrition-workshop';

                UPDATE "CommunityChallenges"
                SET "Title" = '21 Günlük İstikrar Challenge',
                    "Summary" = '21 gün boyunca antrenman, su ve beslenme takibi.',
                    "Description" = 'Üyeler her gün temel hedeflerini tamamlar, haftalık kontrolle ilerleme takip edilir.',
                    "Goal" = '21 gün içinde en az 12 antrenman ve günlük su hedefini tamamlamak.',
                    "Reward" = 'NO23 Shop hediye çekleri ve community panosunda rozet.',
                    "UpdatedAtUtc" = NOW()
                WHERE "Slug" = '21-day-consistency-challenge';

                UPDATE "CommunityChallenges"
                SET "Title" = 'Core Güç Ayı',
                    "Summary" = 'Core stabilizasyonu ve teknik gelişim odaklı aylık challenge.',
                    "Description" = 'Mat pilates, fonksiyonel core ve mobility çalışmalarını birleştiren aylık takip.',
                    "Goal" = 'Ay boyunca 16 core odaklı mini görevi tamamlamak.',
                    "Reward" = 'Özel community dersi daveti.',
                    "UpdatedAtUtc" = NOW()
                WHERE "Slug" = 'core-strength-month';

                UPDATE "BlogPosts"
                SET "Title" = 'Yeni Başlayanlar İçin Haftalık Antrenman Ritmi',
                    "Summary" = 'START ve PLUS üyeleri için sürdürülebilir haftalık antrenman planlama.',
                    "Content" = 'Yeni başlayanlar için en iyi plan, sürdürülebilir olan plandır. Haftada iki veya üç kaliteli seans, uyku ve beslenme ile desteklendiğinde güçlü bir temel oluşturur.',
                    "Category" = 'Antrenman',
                    "Tags" = 'antrenman, başlangıç, planlama',
                    "UpdatedAtUtc" = NOW()
                WHERE "Slug" = 'yeni-baslayanlar-icin-haftalik-antrenman-ritmi';

                UPDATE "BlogPosts"
                SET "Title" = 'Protein Hedefini Gün İçine Yaymak',
                    "Summary" = 'Günlük protein hedefini daha pratik öğünlerle tamamlamak için basit yaklaşım.',
                    "Content" = 'Protein hedefini tek öğüne yıkmak yerine kahvaltı, ana öğün ve ara öğünlere bölmek uyumu kolaylaştırır. NO23 Kitchen menüleri bu takibi sade hale getirmek için tasarlanır.',
                    "Category" = 'Beslenme',
                    "Tags" = 'beslenme, protein, kitchen',
                    "UpdatedAtUtc" = NOW()
                WHERE "Slug" = 'protein-hedefini-gun-icine-yaymak';

                UPDATE "SuccessStories"
                SET "MemberName" = 'Ayşe K.',
                    "Title" = '12 Haftada Daha Güçlü Bir Rutin',
                    "Summary" = 'Düzenli grup dersleri ve Kitchen desteğiyle sürdürülebilir değişim.',
                    "Story" = 'Ayşe, haftada üç grup dersi ve dengeli beslenme planıyla 12 haftada hem kuvvet hem enerji seviyesinde belirgin ilerleme kaydetti.',
                    "AchievementMetric" = '12 hafta, 36 ders, 6 kg yağ kaybı',
                    "UpdatedAtUtc" = NOW()
                WHERE "Slug" = '12-haftada-daha-guclu-bir-rutin';

                UPDATE "SuccessStories"
                SET "Title" = 'Performans Odaklı Dönüşüm',
                    "Summary" = 'Atletik performans programı ile hız ve kuvvet kazanımı.',
                    "Story" = 'Mert, branşa özel kuvvet ve patlayıcı güç programıyla saha performansını daha takip edilebilir hale getirdi.',
                    "AchievementMetric" = '8 hafta, sprint süresinde yüzde 7 iyileşme',
                    "UpdatedAtUtc" = NOW()
                WHERE "Slug" = 'performans-odakli-donusum';

                UPDATE "KitchenMenuItems"
                SET "Tags" = 'yüksek protein, performans',
                    "UpdatedAtUtc" = NOW()
                WHERE "Name" = 'Protein Power Bowl';

                UPDATE "KitchenMenuItems"
                SET "Tags" = 'yüksek protein, düşük kalori',
                    "UpdatedAtUtc" = NOW()
                WHERE "Name" = 'Lean Breakfast Plate';

                UPDATE "KitchenMenuItems"
                SET "Tags" = 'toparlanma, içecek',
                    "UpdatedAtUtc" = NOW()
                WHERE "Name" = 'Green Recovery Smoothie';

                UPDATE "KitchenMenuItems"
                SET "Tags" = 'glutensiz, tatlı',
                    "UpdatedAtUtc" = NOW()
                WHERE "Name" = 'Gluten Free Fit Brownie';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "ShopProducts"
                SET "Description" = 'Premium pamuk karisimli oversize antrenman ve gunluk kullanim hoodie.',
                    "Category" = 'Apparel',
                    "Tags" = 'hoodie, lifestyle, apparel',
                    "UpdatedAtUtc" = NOW()
                WHERE "Sku" = 'NO23-HOODIE-001';

                UPDATE "ShopProducts"
                SET "Description" = 'Nefes alan kumasli, antrenman odakli NO23 t-shirt.',
                    "Category" = 'Apparel',
                    "Tags" = 't-shirt, training, apparel',
                    "UpdatedAtUtc" = NOW()
                WHERE "Sku" = 'NO23-TSHIRT-001';

                UPDATE "ShopProducts"
                SET "Description" = 'Protein ve supplement karisimlari icin sizdirmaz shaker.',
                    "Category" = 'Accessories',
                    "Tags" = 'shaker, accessory',
                    "UpdatedAtUtc" = NOW()
                WHERE "Sku" = 'NO23-SHAKER-001';

                UPDATE "ShopProducts"
                SET "Name" = 'Resistance Band Set',
                    "Description" = 'Isinma, mobilite ve kuvvet destek egzersizleri icin direnc bandi seti.',
                    "Category" = 'Equipment',
                    "Tags" = 'equipment, mobility, strength',
                    "UpdatedAtUtc" = NOW()
                WHERE "Sku" = 'NO23-BAND-001';

                UPDATE "CommunityEvents"
                SET "Title" = 'NO23 Run Club Morning Session',
                    "Summary" = 'Tempo kontrollu sabah kosusu ve mobilite calismasi.',
                    "Description" = 'NO23 community uyeleri icin haftalik sabah kosusu, isinma ve cooldown rutini.',
                    "UpdatedAtUtc" = NOW()
                WHERE "Slug" = 'no23-run-club-morning-session';

                UPDATE "CommunityEvents"
                SET "Title" = 'Performance Nutrition Workshop',
                    "Summary" = 'Antrenman gunlerinde makro planlama ve pratik ogun secimi.',
                    "Description" = 'NO23 Kitchen ekibiyle performans beslenmesi, kalori hedefleri ve pratik menuler uzerine workshop.',
                    "UpdatedAtUtc" = NOW()
                WHERE "Slug" = 'performance-nutrition-workshop';

                UPDATE "CommunityChallenges"
                SET "Title" = '21 Day Consistency Challenge',
                    "Summary" = '21 gun boyunca antrenman, su ve beslenme takibi.',
                    "Description" = 'Uyeler her gun temel hedeflerini tamamlar, haftalik kontrolle ilerleme takip edilir.',
                    "Goal" = '21 gun icinde en az 12 antrenman ve gunluk su hedefini tamamlamak.',
                    "Reward" = 'NO23 Shop hediye cekleri ve community panosunda rozet.',
                    "UpdatedAtUtc" = NOW()
                WHERE "Slug" = '21-day-consistency-challenge';

                UPDATE "CommunityChallenges"
                SET "Title" = 'Core Strength Month',
                    "Summary" = 'Core stabilizasyonu ve teknik gelisim odakli aylik challenge.',
                    "Description" = 'Mat pilates, fonksiyonel core ve mobility calismalarini birlestiren aylik takip.',
                    "Goal" = 'Ay boyunca 16 core odakli mini gorevi tamamlamak.',
                    "Reward" = 'Ozel community dersi daveti.',
                    "UpdatedAtUtc" = NOW()
                WHERE "Slug" = 'core-strength-month';

                UPDATE "BlogPosts"
                SET "Title" = 'Yeni Baslayanlar Icin Haftalik Antrenman Ritmi',
                    "Summary" = 'START ve PLUS uyeleri icin surdurulebilir haftalik antrenman planlama.',
                    "Content" = 'Yeni baslayanlar icin en iyi plan, surdurulebilir olan plandir. Haftada iki veya uc kaliteli seans, uyku ve beslenme ile desteklendiginde guclu bir temel olusturur.',
                    "Category" = 'Training',
                    "Tags" = 'training, beginner, planning',
                    "UpdatedAtUtc" = NOW()
                WHERE "Slug" = 'yeni-baslayanlar-icin-haftalik-antrenman-ritmi';

                UPDATE "BlogPosts"
                SET "Title" = 'Protein Hedefini Gun Icine Yaymak',
                    "Summary" = 'Gunluk protein hedefini daha pratik ogunlerle tamamlamak icin basit yaklasim.',
                    "Content" = 'Protein hedefini tek ogune yikmak yerine kahvalti, ana ogun ve ara ogunlere bolmek uyumu kolaylastirir. NO23 Kitchen menuleri bu takibi sade hale getirmek icin tasarlanir.',
                    "Category" = 'Nutrition',
                    "Tags" = 'nutrition, protein, kitchen',
                    "UpdatedAtUtc" = NOW()
                WHERE "Slug" = 'protein-hedefini-gun-icine-yaymak';

                UPDATE "SuccessStories"
                SET "MemberName" = 'Ayse K.',
                    "Title" = '12 Haftada Daha Guclu Bir Rutin',
                    "Summary" = 'Duzenli grup dersleri ve Kitchen destegiyle surdurulebilir degisim.',
                    "Story" = 'Ayse, haftada uc grup dersi ve dengeli beslenme planiyla 12 haftada hem kuvvet hem enerji seviyesinde belirgin ilerleme kaydetti.',
                    "AchievementMetric" = '12 hafta, 36 ders, 6 kg yag kaybi',
                    "UpdatedAtUtc" = NOW()
                WHERE "Slug" = '12-haftada-daha-guclu-bir-rutin';

                UPDATE "SuccessStories"
                SET "Title" = 'Performans Odakli Donusum',
                    "Summary" = 'Atletik performans programi ile hiz ve kuvvet kazanimi.',
                    "Story" = 'Mert, bransa ozel kuvvet ve patlayici guc programiyla saha performansini daha takip edilebilir hale getirdi.',
                    "AchievementMetric" = '8 hafta, sprint suresinde yuzde 7 iyilesme',
                    "UpdatedAtUtc" = NOW()
                WHERE "Slug" = 'performans-odakli-donusum';

                UPDATE "KitchenMenuItems"
                SET "Tags" = 'High Protein, Performance',
                    "UpdatedAtUtc" = NOW()
                WHERE "Name" = 'Protein Power Bowl';

                UPDATE "KitchenMenuItems"
                SET "Tags" = 'High Protein, Low Calorie',
                    "UpdatedAtUtc" = NOW()
                WHERE "Name" = 'Lean Breakfast Plate';

                UPDATE "KitchenMenuItems"
                SET "Tags" = 'Recovery, Beverage',
                    "UpdatedAtUtc" = NOW()
                WHERE "Name" = 'Green Recovery Smoothie';

                UPDATE "KitchenMenuItems"
                SET "Tags" = 'Gluten Free, Dessert',
                    "UpdatedAtUtc" = NOW()
                WHERE "Name" = 'Gluten Free Fit Brownie';
                """);
        }
    }
}
