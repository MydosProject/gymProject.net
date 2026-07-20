using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Data.Seed;

public static class CommunityContentSeed
{
    public static IReadOnlyList<CommunityEvent> Events =>
    [
        new()
        {
            Title = "NO23 Run Club Morning Session",
            Slug = "no23-run-club-morning-session",
            Summary = "Tempo kontrollu sabah kosusu ve mobilite calismasi.",
            Description = "NO23 community uyeleri icin haftalik sabah kosusu, isinma ve cooldown rutini.",
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
            Title = "Performance Nutrition Workshop",
            Slug = "performance-nutrition-workshop",
            Summary = "Antrenman gunlerinde makro planlama ve pratik ogun secimi.",
            Description = "NO23 Kitchen ekibiyle performans beslenmesi, kalori hedefleri ve pratik menuler uzerine workshop.",
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
            Title = "21 Day Consistency Challenge",
            Slug = "21-day-consistency-challenge",
            Summary = "21 gun boyunca antrenman, su ve beslenme takibi.",
            Description = "Uyeler her gun temel hedeflerini tamamlar, haftalik kontrolle ilerleme takip edilir.",
            Goal = "21 gun icinde en az 12 antrenman ve gunluk su hedefini tamamlamak.",
            Reward = "NO23 Shop hediye cekleri ve community panosunda rozet.",
            StartsOn = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            EndsOn = DateOnly.FromDateTime(DateTime.Today.AddDays(24)),
            Status = CommunityChallengeStatus.Upcoming,
            DisplayOrder = 10
        },
        new()
        {
            Title = "Core Strength Month",
            Slug = "core-strength-month",
            Summary = "Core stabilizasyonu ve teknik gelisim odakli aylik challenge.",
            Description = "Mat pilates, fonksiyonel core ve mobility calismalarini birlestiren aylik takip.",
            Goal = "Ay boyunca 16 core odakli mini gorevi tamamlamak.",
            Reward = "Ozel community dersi daveti.",
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
            Title = "Yeni Baslayanlar Icin Haftalik Antrenman Ritmi",
            Slug = "yeni-baslayanlar-icin-haftalik-antrenman-ritmi",
            Summary = "START ve PLUS uyeleri icin surdurulebilir haftalik antrenman planlama.",
            Content = "Yeni baslayanlar icin en iyi plan, surdurulebilir olan plandir. Haftada iki veya uc kaliteli seans, uyku ve beslenme ile desteklendiginde guclu bir temel olusturur.",
            Category = "Training",
            Tags = "training, beginner, planning",
            Status = ContentStatus.Published,
            PublishedAtUtc = DateTime.UtcNow.AddDays(-2)
        },
        new()
        {
            Title = "Protein Hedefini Gun Icine Yaymak",
            Slug = "protein-hedefini-gun-icine-yaymak",
            Summary = "Gunluk protein hedefini daha pratik ogunlerle tamamlamak icin basit yaklasim.",
            Content = "Protein hedefini tek ogune yikmak yerine kahvalti, ana ogun ve ara ogunlere bolmek uyumu kolaylastirir. NO23 Kitchen menuleri bu takibi sade hale getirmek icin tasarlanir.",
            Category = "Nutrition",
            Tags = "nutrition, protein, kitchen",
            Status = ContentStatus.Published,
            PublishedAtUtc = DateTime.UtcNow.AddDays(-1)
        }
    ];

    public static IReadOnlyList<SuccessStory> SuccessStories =>
    [
        new()
        {
            MemberName = "Ayse K.",
            Title = "12 Haftada Daha Guclu Bir Rutin",
            Slug = "12-haftada-daha-guclu-bir-rutin",
            Summary = "Duzenli grup dersleri ve Kitchen destegiyle surdurulebilir degisim.",
            Story = "Ayse, haftada uc grup dersi ve dengeli beslenme planiyla 12 haftada hem kuvvet hem enerji seviyesinde belirgin ilerleme kaydetti.",
            AchievementMetric = "12 hafta, 36 ders, 6 kg yag kaybi",
            Status = ContentStatus.Published,
            PublishedAtUtc = DateTime.UtcNow.AddDays(-5)
        },
        new()
        {
            MemberName = "Mert T.",
            Title = "Performans Odakli Donusum",
            Slug = "performans-odakli-donusum",
            Summary = "Atletik performans programi ile hiz ve kuvvet kazanimi.",
            Story = "Mert, bransa ozel kuvvet ve patlayici guc programiyla saha performansini daha takip edilebilir hale getirdi.",
            AchievementMetric = "8 hafta, sprint suresinde yuzde 7 iyilesme",
            Status = ContentStatus.Published,
            PublishedAtUtc = DateTime.UtcNow.AddDays(-8)
        }
    ];
}
