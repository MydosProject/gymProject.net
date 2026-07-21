using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Data.Seed;

public static class CommunityContentSeed
{
    public static IReadOnlyList<CommunityEvent> Events =>
    [
        new()
        {
            Title = "NO23 Run Club Sabah Seansı",
            Slug = "no23-run-club-morning-session",
            Summary = "Tempo kontrollü sabah koşusu ve mobilite çalışması.",
            Description = "NO23 community üyeleri için haftalık sabah koşusu, ısınma ve cooldown rutini.",
            Type = CommunityEventType.RunningGroup,
            Status = CommunityEventStatus.Scheduled,
            StartsAtUtc = DateTime.UtcNow.Date.AddDays(7).AddHours(5),
            EndsAtUtc = DateTime.UtcNow.Date.AddDays(7).AddHours(6),
            Location = "NO23 Sports Club",
            Capacity = 24,
            IsMembersOnly = true,
            DisplayOrder = 10
        },
        new()
        {
            Title = "Performans Beslenmesi Workshop",
            Slug = "performance-nutrition-workshop",
            Summary = "Antrenman günlerinde makro planlama ve pratik öğün seçimi.",
            Description = "NO23 Kitchen ekibiyle performans beslenmesi, kalori hedefleri ve pratik menüler üzerine workshop.",
            Type = CommunityEventType.Workshop,
            Status = CommunityEventStatus.Scheduled,
            StartsAtUtc = DateTime.UtcNow.Date.AddDays(14).AddHours(16),
            EndsAtUtc = DateTime.UtcNow.Date.AddDays(14).AddHours(18),
            Location = "NO23 Kitchen Area",
            Capacity = 18,
            IsMembersOnly = true,
            DisplayOrder = 20
        }
    ];

    public static IReadOnlyList<CommunityChallenge> Challenges =>
    [
        new()
        {
            Title = "21 Günlük İstikrar Challenge",
            Slug = "21-day-consistency-challenge",
            Summary = "21 gün boyunca antrenman, su ve beslenme takibi.",
            Description = "Üyeler her gün temel hedeflerini tamamlar, haftalık kontrolle ilerleme takip edilir.",
            Goal = "21 gün içinde en az 12 antrenman ve günlük su hedefini tamamlamak.",
            Reward = "NO23 Shop hediye çekleri ve community panosunda rozet.",
            StartsOn = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            EndsOn = DateOnly.FromDateTime(DateTime.Today.AddDays(24)),
            Status = CommunityChallengeStatus.Upcoming,
            DisplayOrder = 10
        },
        new()
        {
            Title = "Core Güç Ayı",
            Slug = "core-strength-month",
            Summary = "Core stabilizasyonu ve teknik gelişim odaklı aylık challenge.",
            Description = "Mat pilates, fonksiyonel core ve mobility çalışmalarını birleştiren aylık takip.",
            Goal = "Ay boyunca 16 core odaklı mini görevi tamamlamak.",
            Reward = "Özel community dersi daveti.",
            StartsOn = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            EndsOn = DateOnly.FromDateTime(DateTime.Today.AddDays(40)),
            Status = CommunityChallengeStatus.Upcoming,
            DisplayOrder = 20
        }
    ];

    public static IReadOnlyList<BlogPost> BlogPosts =>
    [
        new()
        {
            Title = "Yeni Başlayanlar İçin Haftalık Antrenman Ritmi",
            Slug = "yeni-baslayanlar-icin-haftalik-antrenman-ritmi",
            Summary = "START ve PLUS üyeleri için sürdürülebilir haftalık antrenman planlama.",
            Content = "Yeni başlayanlar için en iyi plan, sürdürülebilir olan plandır. Haftada iki veya üç kaliteli seans, uyku ve beslenme ile desteklendiğinde güçlü bir temel oluşturur.",
            Category = "Antrenman",
            Tags = "antrenman, başlangıç, planlama",
            Status = ContentStatus.Published,
            PublishedAtUtc = DateTime.UtcNow.AddDays(-2)
        },
        new()
        {
            Title = "Protein Hedefini Gün İçine Yaymak",
            Slug = "protein-hedefini-gun-icine-yaymak",
            Summary = "Günlük protein hedefini daha pratik öğünlerle tamamlamak için basit yaklaşım.",
            Content = "Protein hedefini tek öğüne yıkmak yerine kahvaltı, ana öğün ve ara öğünlere bölmek uyumu kolaylaştırır. NO23 Kitchen menüleri bu takibi sade hale getirmek için tasarlanır.",
            Category = "Beslenme",
            Tags = "beslenme, protein, kitchen",
            Status = ContentStatus.Published,
            PublishedAtUtc = DateTime.UtcNow.AddDays(-1)
        }
    ];

    public static IReadOnlyList<SuccessStory> SuccessStories =>
    [
        new()
        {
            MemberName = "Ayşe K.",
            Title = "12 Haftada Daha Güçlü Bir Rutin",
            Slug = "12-haftada-daha-guclu-bir-rutin",
            Summary = "Düzenli grup dersleri ve Kitchen desteğiyle sürdürülebilir değişim.",
            Story = "Ayşe, haftada üç grup dersi ve dengeli beslenme planıyla 12 haftada hem kuvvet hem enerji seviyesinde belirgin ilerleme kaydetti.",
            AchievementMetric = "12 hafta, 36 ders, 6 kg yağ kaybı",
            Status = ContentStatus.Published,
            PublishedAtUtc = DateTime.UtcNow.AddDays(-5)
        },
        new()
        {
            MemberName = "Mert T.",
            Title = "Performans Odaklı Dönüşüm",
            Slug = "performans-odakli-donusum",
            Summary = "Atletik performans programı ile hız ve kuvvet kazanımı.",
            Story = "Mert, branşa özel kuvvet ve patlayıcı güç programıyla saha performansını daha takip edilebilir hale getirdi.",
            AchievementMetric = "8 hafta, sprint süresinde yüzde 7 iyileşme",
            Status = ContentStatus.Published,
            PublishedAtUtc = DateTime.UtcNow.AddDays(-8)
        }
    ];
}
