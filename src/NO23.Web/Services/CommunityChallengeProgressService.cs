using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public class CommunityChallengeProgressService(ApplicationDbContext dbContext)
{
    public async Task<CommunityChallengeActionResult> JoinAsync(string userId, string challengeSlug)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return CommunityChallengeActionResult.Fail("Oturum bilgisi bulunamadı.");
        }

        if (string.IsNullOrWhiteSpace(challengeSlug))
        {
            return CommunityChallengeActionResult.Fail("Challenge bulunamadı.");
        }

        var challenge = await dbContext.CommunityChallenges
            .FirstOrDefaultAsync(item =>
                item.Slug == challengeSlug.Trim() &&
                item.Status != CommunityChallengeStatus.Cancelled);

        if (challenge is null)
        {
            return CommunityChallengeActionResult.Fail("Challenge katılıma açık değil.");
        }

        if (!CommunityChallengeLifecycle.IsJoinOpen(
                CommunityChallengeLifecycle.GetEffectiveStatus(
                    challenge.Status,
                    challenge.StartsOn,
                    challenge.EndsOn,
                    DateOnly.FromDateTime(DateTime.Today))))
        {
            return CommunityChallengeActionResult.Fail("Challenge katilima acik degil.");
        }

        var profile = await GetCommunityMemberProfileAsync(userId);

        if (profile is null)
        {
            return CommunityChallengeActionResult.Fail(
                "Bu challenge'a katılmak için Community üyeliği gerekir.");
        }

        var existingParticipation = await dbContext.CommunityChallengeParticipations
            .FirstOrDefaultAsync(item =>
                item.CommunityChallengeId == challenge.Id &&
                item.MemberProfileId == profile.Id);

        if (existingParticipation is not null)
        {
            return CommunityChallengeActionResult.Ok(
                existingParticipation.Id,
                "Bu challenge'a zaten katıldın.");
        }

        var participation = new CommunityChallengeParticipation
        {
            CommunityChallengeId = challenge.Id,
            MemberProfileId = profile.Id
        };

        dbContext.CommunityChallengeParticipations.Add(participation);
        await dbContext.SaveChangesAsync();

        return CommunityChallengeActionResult.Ok(
            participation.Id,
            "Challenge katılımın oluşturuldu.");
    }

    public async Task<CommunityChallengeActionResult> UpsertDailyCaloriesAsync(
        string userId,
        ChallengeCalorieLogRequest request)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return CommunityChallengeActionResult.Fail("Oturum bilgisi bulunamadı.");
        }

        if (request.CaloriesConsumed <= 0)
        {
            return CommunityChallengeActionResult.Fail(
                "Alınan kalori sıfırdan büyük olmalıdır.");
        }

        if (request.CaloriesConsumed > 10000)
        {
            return CommunityChallengeActionResult.Fail(
                "Alınan kalori 10000 kcal değerini geçemez.");
        }

        var profile = await GetCommunityMemberProfileAsync(userId);

        if (profile is null)
        {
            return CommunityChallengeActionResult.Fail(
                "Bu işlem için Community üyeliği gerekir.");
        }

        var participation = await dbContext.CommunityChallengeParticipations
            .Include(item => item.CommunityChallenge)
            .Include(item => item.ProgressEntries)
            .FirstOrDefaultAsync(item =>
                item.Id == request.ParticipationId &&
                item.MemberProfileId == profile.Id);

        if (participation is null)
        {
            return CommunityChallengeActionResult.Fail("Challenge katılımı bulunamadı.");
        }

        if (participation.Status == CommunityChallengeParticipationStatus.Withdrawn)
        {
            return CommunityChallengeActionResult.Fail(
                "Ayrıldığın challenge için kalori girişi yapılamaz.");
        }

        var challenge = participation.CommunityChallenge;

        var effectiveStatus = CommunityChallengeLifecycle.GetEffectiveStatus(
            challenge.Status,
            challenge.StartsOn,
            challenge.EndsOn,
            DateOnly.FromDateTime(DateTime.Today));

        if (!CommunityChallengeLifecycle.CanLogCalories(effectiveStatus))
        {
            return CommunityChallengeActionResult.Fail(
                "Bu challenge için kalori girişi kapalı.");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);

        if (request.EntryDate > today)
        {
            return CommunityChallengeActionResult.Fail(
                "Gelecek tarih için kalori girişi yapılamaz.");
        }

        if (request.EntryDate < challenge.StartsOn || request.EntryDate > challenge.EndsOn)
        {
            return CommunityChallengeActionResult.Fail(
                "Seçilen tarih challenge tarih aralığında değil.");
        }

        var range = CommunityChallengeProgressCalculator.GetCalorieRange(
            challenge.TargetDailyCalories,
            challenge.CalorieTolerancePercent);
        var isCompliant = CommunityChallengeProgressCalculator.IsCalorieCompliant(
            request.CaloriesConsumed,
            range);

        var entry = participation.ProgressEntries
            .FirstOrDefault(item => item.EntryDate == request.EntryDate);

        if (entry is null)
        {
            entry = new ChallengeProgressEntry
            {
                EntryDate = request.EntryDate
            };

            participation.ProgressEntries.Add(entry);
        }
        else
        {
            entry.UpdatedAtUtc = DateTime.UtcNow;
        }

        entry.CaloriesConsumed = request.CaloriesConsumed;
        entry.TargetDailyCaloriesSnapshot = challenge.TargetDailyCalories;
        entry.CalorieTolerancePercentSnapshot = challenge.CalorieTolerancePercent;
        entry.MinCaloriesSnapshot = range.MinCalories;
        entry.MaxCaloriesSnapshot = range.MaxCalories;
        entry.IsCompliant = isCompliant;

        var stats = CommunityChallengeProgressCalculator.GetProgressStats(
            challenge.StartsOn,
            challenge.EndsOn,
            challenge.RequiredCompletionPercent,
            participation.ProgressEntries);

        participation.Status = stats.IsCompleted
            ? CommunityChallengeParticipationStatus.Completed
            : CommunityChallengeParticipationStatus.Active;
        participation.CompletedAtUtc = stats.IsCompleted
            ? participation.CompletedAtUtc ?? DateTime.UtcNow
            : null;
        participation.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return CommunityChallengeActionResult.Ok(
            participation.Id,
            isCompliant
                ? "Kalori girişin hedef aralığında."
                : "Kalori girişin kaydedildi; hedef aralığının dışında kaldı.");
    }

    private async Task<MemberProfile?> GetCommunityMemberProfileAsync(string userId)
    {
        return await dbContext.MemberProfiles
            .Include(member => member.MembershipPackage)
            .FirstOrDefaultAsync(member =>
                member.ApplicationUserId == userId &&
                member.MembershipPackage.IncludesCommunityMembership);
    }
}

public static class CommunityChallengeProgressCalculator
{
    public static ChallengeCalorieRange GetCalorieRange(
        int targetDailyCalories,
        decimal tolerancePercent)
    {
        var normalizedTarget = Math.Max(0, targetDailyCalories);
        var normalizedTolerance = Math.Clamp(tolerancePercent, 0, 100);
        var toleranceCalories = normalizedTarget * normalizedTolerance / 100m;

        return new ChallengeCalorieRange(
            (int)Math.Round(normalizedTarget - toleranceCalories, MidpointRounding.AwayFromZero),
            (int)Math.Round(normalizedTarget + toleranceCalories, MidpointRounding.AwayFromZero));
    }

    public static bool IsCalorieCompliant(
        int caloriesConsumed,
        ChallengeCalorieRange range)
    {
        return caloriesConsumed >= range.MinCalories &&
               caloriesConsumed <= range.MaxCalories;
    }

    public static ChallengeProgressStats GetProgressStats(
        DateOnly startsOn,
        DateOnly endsOn,
        int requiredCompletionPercent,
        IEnumerable<ChallengeProgressEntry> entries)
    {
        var totalDays = Math.Max(1, endsOn.DayNumber - startsOn.DayNumber + 1);
        var normalizedRequiredPercent = Math.Clamp(requiredCompletionPercent, 1, 100);
        var loggedDays = entries
            .Select(entry => entry.EntryDate)
            .Distinct()
            .Count();
        var compliantDays = entries
            .Where(entry => entry.IsCompliant)
            .Select(entry => entry.EntryDate)
            .Distinct()
            .Count();
        var progressPercent = decimal.Round(
            compliantDays * 100m / totalDays,
            1,
            MidpointRounding.AwayFromZero);
        var requiredCompliantDays = (int)Math.Ceiling(totalDays * normalizedRequiredPercent / 100m);

        return new ChallengeProgressStats(
            totalDays,
            loggedDays,
            compliantDays,
            progressPercent,
            requiredCompliantDays,
            progressPercent >= normalizedRequiredPercent);
    }
}

public record ChallengeCalorieLogRequest(
    int ParticipationId,
    DateOnly EntryDate,
    int CaloriesConsumed);

public record ChallengeCalorieRange(
    int MinCalories,
    int MaxCalories);

public record ChallengeProgressStats(
    int TotalDays,
    int LoggedDays,
    int CompliantDays,
    decimal ProgressPercent,
    int RequiredCompliantDays,
    bool IsCompleted);

public record CommunityChallengeActionResult(
    bool Succeeded,
    int? ParticipationId,
    string Message)
{
    public static CommunityChallengeActionResult Ok(int participationId, string message)
    {
        return new CommunityChallengeActionResult(true, participationId, message);
    }

    public static CommunityChallengeActionResult Fail(string message)
    {
        return new CommunityChallengeActionResult(false, null, message);
    }
}
