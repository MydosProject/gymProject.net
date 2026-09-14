using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.Services;

public class MemberProgressTrackingService(ApplicationDbContext dbContext)
{
    public async Task<MemberProgressTrackingResult> UpsertAsync(
        string userId,
        MemberProgressEntryInputViewModel input,
        bool preserveMeasurements = false,
        bool preserveTrainerMeasurements = false,
        bool preserveCaloriesWhenMissing = false)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return MemberProgressTrackingResult.Fail("Oturum bilgisi bulunamadı.");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);

        if (input.EntryDate > today)
        {
            return MemberProgressTrackingResult.Fail("İleri tarih için kayıt girilemez.");
        }

        var profile = await dbContext.MemberProfiles
            .FirstOrDefaultAsync(member => member.ApplicationUserId == userId);

        if (profile is null)
        {
            return MemberProgressTrackingResult.Fail("Üye profili bulunamadı.");
        }

        var entry = await dbContext.MemberProgressEntries
            .FirstOrDefaultAsync(item =>
                item.MemberProfileId == profile.Id &&
                item.EntryDate == input.EntryDate);
        var hasAnyValue = HasAnyValue(input) ||
            (preserveTrainerMeasurements &&
             entry is not null &&
             HasAnyTrainerMeasurement(entry));

        if (!hasAnyValue)
        {
            if (entry is null)
            {
                return MemberProgressTrackingResult.Fail("Kaydedilecek değer yok.");
            }

            dbContext.MemberProgressEntries.Remove(entry);
            await SyncChallengeProgressAsync(profile.Id, input.EntryDate, null);
            await dbContext.SaveChangesAsync();

            return MemberProgressTrackingResult.Ok("Kayıt silindi.");
        }

        if (entry is null)
        {
            entry = new MemberProgressEntry
            {
                MemberProfileId = profile.Id,
                EntryDate = input.EntryDate
            };

            dbContext.MemberProgressEntries.Add(entry);
        }
        else
        {
            entry.UpdatedAtUtc = DateTime.UtcNow;
        }

        if (input.CaloriesConsumed.HasValue || !preserveCaloriesWhenMissing)
        {
            entry.CaloriesConsumed = input.CaloriesConsumed;
        }

        // Challenge calorie entries are intentionally separate from body
        // measurements. A calorie-only edit must not erase an existing
        // measurement recorded on the same day.
        if (!preserveMeasurements)
        {
            entry.BodyWeightKg = Normalize(input.BodyWeightKg);
            entry.BodyFatKg = Normalize(input.BodyFatKg);
            entry.BodyFatPercent = Normalize(input.BodyFatPercent);
            entry.MuscleMassKg = Normalize(input.MuscleMassKg);
            entry.MuscleMassPercent = Normalize(input.MuscleMassPercent);
            entry.BodyWaterAmount = Normalize(input.BodyWaterAmount);
            entry.BodyWaterPercent = Normalize(input.BodyWaterPercent);
            entry.DailyWaterIntakeLiters = Normalize(input.DailyWaterIntakeLiters);
        }

        if (!preserveTrainerMeasurements)
        {
            entry.HeightCm = Normalize(input.HeightCm);
            entry.ShoulderCm = Normalize(input.ShoulderCm);
            entry.ChestCm = Normalize(input.ChestCm);
            entry.RightArmCm = Normalize(input.RightArmCm);
            entry.LeftArmCm = Normalize(input.LeftArmCm);
            entry.WaistCm = Normalize(input.WaistCm);
            entry.AbdomenCm = Normalize(input.AbdomenCm);
            entry.HipCm = Normalize(input.HipCm);
            entry.RightUpperLegCm = Normalize(input.RightUpperLegCm);
            entry.LeftUpperLegCm = Normalize(input.LeftUpperLegCm);
        }

        await SyncChallengeProgressAsync(profile.Id, input.EntryDate, entry.CaloriesConsumed);
        await dbContext.SaveChangesAsync();

        return MemberProgressTrackingResult.Ok("Kayıt güncellendi.");
    }

    private async Task SyncChallengeProgressAsync(
        int memberProfileId,
        DateOnly entryDate,
        int? caloriesConsumed)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var participations = (await dbContext.CommunityChallengeParticipations
            .Include(item => item.CommunityChallenge)
            .Include(item => item.ProgressEntries)
            .Where(item =>
                item.MemberProfileId == memberProfileId &&
                item.Status != CommunityChallengeParticipationStatus.Withdrawn &&
                item.CommunityChallenge.Status != CommunityChallengeStatus.Cancelled &&
                item.CommunityChallenge.StartsOn <= entryDate &&
                item.CommunityChallenge.EndsOn >= entryDate)
            .ToListAsync())
            .Where(item => CommunityChallengeLifecycle.CanLogCalories(
                CommunityChallengeLifecycle.GetEffectiveStatus(
                    item.CommunityChallenge.Status,
                    item.CommunityChallenge.StartsOn,
                    item.CommunityChallenge.EndsOn,
                    today)))
            .ToList();

        foreach (var participation in participations)
        {
            var challenge = participation.CommunityChallenge;
            var challengeEntry = participation.ProgressEntries
                .FirstOrDefault(item => item.EntryDate == entryDate);

            if (!caloriesConsumed.HasValue)
            {
                if (challengeEntry is not null)
                {
                    participation.ProgressEntries.Remove(challengeEntry);
                    dbContext.ChallengeProgressEntries.Remove(challengeEntry);
                }

                UpdateParticipationStats(participation);
                continue;
            }

            var range = CommunityChallengeProgressCalculator.GetCalorieRange(
                challenge.TargetDailyCalories,
                challenge.CalorieTolerancePercent);
            var isCompliant = CommunityChallengeProgressCalculator.IsCalorieCompliant(
                caloriesConsumed.Value,
                range);

            if (challengeEntry is null)
            {
                challengeEntry = new ChallengeProgressEntry
                {
                    EntryDate = entryDate
                };

                participation.ProgressEntries.Add(challengeEntry);
            }
            else
            {
                challengeEntry.UpdatedAtUtc = DateTime.UtcNow;
            }

            challengeEntry.CaloriesConsumed = caloriesConsumed.Value;
            challengeEntry.TargetDailyCaloriesSnapshot = challenge.TargetDailyCalories;
            challengeEntry.CalorieTolerancePercentSnapshot = challenge.CalorieTolerancePercent;
            challengeEntry.MinCaloriesSnapshot = range.MinCalories;
            challengeEntry.MaxCaloriesSnapshot = range.MaxCalories;
            challengeEntry.IsCompliant = isCompliant;

            UpdateParticipationStats(participation);
        }
    }

    private static void UpdateParticipationStats(CommunityChallengeParticipation participation)
    {
        var challenge = participation.CommunityChallenge;
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
    }

    private static bool HasAnyValue(MemberProgressEntryInputViewModel input)
    {
        return input.CaloriesConsumed.HasValue ||
               input.BodyWeightKg.HasValue ||
               input.BodyFatKg.HasValue ||
               input.BodyFatPercent.HasValue ||
               input.MuscleMassKg.HasValue ||
               input.MuscleMassPercent.HasValue ||
               input.BodyWaterAmount.HasValue ||
               input.BodyWaterPercent.HasValue ||
               input.DailyWaterIntakeLiters.HasValue ||
               input.HeightCm.HasValue ||
               input.ShoulderCm.HasValue ||
               input.ChestCm.HasValue ||
               input.RightArmCm.HasValue ||
               input.LeftArmCm.HasValue ||
               input.WaistCm.HasValue ||
               input.AbdomenCm.HasValue ||
               input.HipCm.HasValue ||
               input.RightUpperLegCm.HasValue ||
               input.LeftUpperLegCm.HasValue;
    }

    private static bool HasAnyTrainerMeasurement(MemberProgressEntry entry)
    {
        return entry.HeightCm.HasValue ||
               entry.ShoulderCm.HasValue ||
               entry.ChestCm.HasValue ||
               entry.RightArmCm.HasValue ||
               entry.LeftArmCm.HasValue ||
               entry.WaistCm.HasValue ||
               entry.AbdomenCm.HasValue ||
               entry.HipCm.HasValue ||
               entry.RightUpperLegCm.HasValue ||
               entry.LeftUpperLegCm.HasValue;
    }

    private static decimal? Normalize(decimal? value)
    {
        return value.HasValue
            ? decimal.Round(value.Value, 2, MidpointRounding.AwayFromZero)
            : null;
    }
}

public record MemberProgressTrackingResult(
    bool Succeeded,
    string Message)
{
    public static MemberProgressTrackingResult Ok(string message)
    {
        return new MemberProgressTrackingResult(true, message);
    }

    public static MemberProgressTrackingResult Fail(string message)
    {
        return new MemberProgressTrackingResult(false, message);
    }
}
