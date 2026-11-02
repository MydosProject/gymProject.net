using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.TrainerPanel;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.Services;

public sealed class WeeklyPersonalTrainingPlan
{
    public bool Succeeded { get; set; }
    public string Message { get; set; } = "";
    public int? PackageLessons { get; set; }
    public int CompletedLessons { get; set; }
    public int PlannedLessons { get; set; }
    public int RemainingCredits { get; set; }
    public int StoredBalance { get; set; }
    public int? AvailableLessons { get; set; }
    public int UnchargedCompletedLessons { get; set; }
    public bool CanReviewCredits { get; set; }
    public bool IsUnlimited { get; set; }
    public int LessonCount => Starts.Count;
    public int WeekCount => Starts.Select(start => ClubTime.Monday(ClubTime.ToLocal(start))).Distinct().Count();
    public DateTime? PackageEnd { get; set; }
    public DateTime? PackageStart { get; set; }
    public LessonCreditSummary? CreditSummary { get; set; }
    [JsonIgnore] public List<DateTime> Starts { get; } = [];
    [JsonIgnore] public MemberLessonAccount? PersonalAccount { get; set; }
}

public partial class PersonalTrainingCalendarService
{
    public Task<WeeklyPersonalTrainingPlan> PreviewWeeklyAsync(int trainerId, WeeklyPersonalSessionInput input) =>
        BuildWeeklyPlanAsync(trainerId, input, false, false, false);

    public Task<WeeklyPersonalTrainingPlan> PreviewWeeklyByAdminAsync(int trainerId, WeeklyPersonalSessionInput input) =>
        BuildWeeklyPlanAsync(trainerId, input, true, true, false);

    private IQueryable<PersonalTrainingSession> PackageSessions(MemberProfile member) =>
        dbContext.PersonalTrainingSessions.AsNoTracking().Where(session => session.MemberProfileId == member.Id &&
            session.Status != PersonalTrainingSessionStatus.Cancelled &&
            (session.LessonAccount == null || session.LessonAccount.PeriodId == member.LessonPeriodId) &&
            (!member.MembershipStartsAtUtc.HasValue || session.StartsAtUtc >= member.MembershipStartsAtUtc.Value) &&
            (!member.MembershipEndsAtUtc.HasValue || session.StartsAtUtc < member.MembershipEndsAtUtc.Value));

    private async Task<WeeklyPersonalTrainingPlan> BuildWeeklyPlanAsync(int trainerId,
        WeeklyPersonalSessionInput input, bool allowUnassignedMember, bool allowPastDates, bool applyAccounts = true)
    {
        var plan = new WeeklyPersonalTrainingPlan();
        WeeklyPersonalTrainingPlan Fail(string message) { plan.Starts.Clear(); plan.Message = message; return plan; }
        var member = await dbContext.MemberProfiles
            .Include(m => m.MembershipPackage).Include(m => m.ServicePackageVariant)
            .FirstOrDefaultAsync(m => m.Id == input.MemberProfileId &&
                (allowUnassignedMember || m.AssignedTrainerId == trainerId));
        if (member is null) return Fail("Üye bulunamadı veya bu üyeye ders planlama yetkiniz yok.");

        var existing = await PackageSessions(member).ToListAsync();
        var variant = member.ServicePackageVariant;
        plan.IsUnlimited = MemberPackageEntitlement.HasUnlimitedClassAccess(member);
        plan.PackageLessons = variant is null ? null : variant.PersonalTrainingSessionCount *
            (variant.LessonsRenewMonthly ? Math.Max(1, variant.DurationMonths ?? 1) : 1);
        plan.CompletedLessons = existing.Count(s => s.Status is PersonalTrainingSessionStatus.Completed or PersonalTrainingSessionStatus.NoShow);
        plan.PlannedLessons = existing.Count(s => s.Status == PersonalTrainingSessionStatus.Scheduled);
        plan.UnchargedCompletedLessons = existing.Count(s => !s.CreditConsumed && s.Status is PersonalTrainingSessionStatus.Completed or PersonalTrainingSessionStatus.NoShow);
        var creditSummary = await new LessonCreditSummaryService(dbContext).GetAsync(member.Id);
        plan.CreditSummary = creditSummary;
        plan.RemainingCredits = creditSummary?.Remaining ?? member.RemainingClassCredits;
        plan.StoredBalance = member.RemainingClassCredits;
        plan.CanReviewCredits = !plan.IsUnlimited && member.MembershipStartsAtUtc.HasValue && plan.UnchargedCompletedLessons > 0;
        plan.PackageEnd = member.MembershipEndsAtUtc;
        plan.PackageStart = member.MembershipStartsAtUtc;
        var creditService = new CategoryCreditService(dbContext);
        var allocation = await creditService.PrepareAsync(member, applyAccounts);
        if (allocation.Error is not null)
        {
            var selectedMonday = ClubTime.Monday(input.Week);
            var dateDetail = member.MembershipEndsAtUtc is { } endDate && ClubTime.ToUtc(selectedMonday) >= endDate
                ? $" Seçilen başlangıç haftası ({selectedMonday:dd.MM.yyyy}) paket bitişinde veya sonrasında ({ClubTime.ToLocal(endDate):dd.MM.yyyy HH:mm})." : "";
            return Fail(allocation.Error + dateDetail);
        }
        var account = allocation.Accounts.Single(x => x.Category == LessonCategory.PersonalTraining);
        plan.PersonalAccount = account;
        plan.PackageLessons = account.Granted + account.ConvertedCredits;
        // Scheduled lessons reserve a place in the balance, including past lessons
        // awaiting the worker. Already debited reservations must not be counted twice.
        var available = await creditService.AvailableAsync(member, account);
        plan.AvailableLessons = plan.IsUnlimited && variant is null ? null : available;

        if (account.Granted + account.ConvertedCredits <= 0) return Fail("Üyenin seçili paketi birebir ders hakkı içermiyor.");
        if (input.Days.Length == 0 || input.Days.Any(day => !Enum.IsDefined(day)) || input.DurationMinutes is < 15 or > 240)
            return Fail("Üye, sabit günler ve ders süresini seçin.");
        if (!await dbContext.Trainers.AnyAsync(t => t.Id == trainerId && t.IsActive))
            return Fail("Aktif bir antrenör seçmelisiniz.");
        if (member.MembershipEndsAtUtc is null && input.Weeks is < 1 or > 52)
            return Fail("Paket bitiş tarihi bilinmeyen üyelerde hafta sayısı 1 ile 52 arasında olmalıdır.");
        var monday = ClubTime.Monday(input.Week);
        var period = "Paket dönemi: " +
            (member.MembershipStartsAtUtc is { } packageStart ? ClubTime.ToLocal(packageStart).ToString("dd.MM.yyyy HH:mm") : "başlangıç kayıtlı değil") +
            " - " + (member.MembershipEndsAtUtc is { } packageEnd ? ClubTime.ToLocal(packageEnd).ToString("dd.MM.yyyy HH:mm") : "bitiş kayıtlı değil") + ".";
        if (member.MembershipEndsAtUtc is { } endsAt && ClubTime.ToUtc(monday) >= endsAt)
            return Fail($"Seçilen başlangıç haftası ({monday:dd.MM.yyyy}) paket bitişinde veya sonrasında. {period} Kalan hak silinmedi; bu tarihlerde yeni ders planlanamaz.");
        var weeks = member.MembershipEndsAtUtc is { } end
            ? Math.Max(0, (int)((ClubTime.Monday(ClubTime.ToLocal(end)) - monday).TotalDays / 7) + 1) : input.Weeks;
        if (weeks > 5200) return Fail("Paket tarihlerini kontrol edin.");
        var candidates = Enumerable.Range(0, weeks)
            .SelectMany(w => input.Days.Distinct().Select(day => ClubTime.ToUtc(monday
                .AddDays(w * 7 + ((int)day + 6) % 7).Add(input.Time.ToTimeSpan()))))
            .Where(start => (!member.MembershipStartsAtUtc.HasValue || start >= member.MembershipStartsAtUtc) &&
                (!member.MembershipEndsAtUtc.HasValue || start.AddMinutes(input.DurationMinutes) <= member.MembershipEndsAtUtc))
            .OrderBy(start => start)
            .Where(start => !existing.Any(s => s.TrainerId == trainerId && s.StartsAtUtc == start && s.DurationMinutes == input.DurationMinutes))
            .ToList();
        if (!allowPastDates && candidates.Any(start => start <= DateTime.UtcNow))
            return Fail("Tüm birebir dersler gelecekte olmalı.");
        if (available == 0) return Fail("Planlanabilecek yeni birebir ders hakkı yok. Mevcut programı ve hak bakiyesini kontrol edin.");

        var byMonth = existing.GroupBy(s => Month(s.StartsAtUtc)).ToDictionary(g => g.Key, g => g.Count());
        var monthlyLimitDates = 0;
        foreach (var start in candidates)
        {
            if (plan.Starts.Count >= available) break;
            var month = Month(start);
            if (account.MonthlyLimit is { } monthlyLimit && byMonth.GetValueOrDefault(month) >= monthlyLimit)
            {
                if (member.MembershipEndsAtUtc is null) return Fail("Seçilen haftalar paketin aylık birebir ders sınırını aşıyor.");
                monthlyLimitDates++;
                continue;
            }
            plan.Starts.Add(start);
            byMonth[month] = byMonth.GetValueOrDefault(month) + 1;
        }
        if (member.MembershipEndsAtUtc is null && candidates.Count > available)
        { plan.Starts.Clear(); return Fail("Planlanan ders sayısı üyenin kullanılabilir ders hakkını aşıyor."); }
        if (plan.Starts.Count == 0) return Fail(monthlyLimitDates > 0
            ? $"Seçilen tarihlerde aylık birebir ders sınırı ({account.MonthlyLimit}) dolu; {monthlyLimitDates} tarih bu nedenle eklenemedi. {period} Kalan hak silinmedi."
            : $"Seçilen {monday:dd.MM.yyyy} haftasından itibaren gün/saat ve ders süresine uyan yeni tarih bulunmuyor; tarihler paket dışında veya aynı dersler zaten kayıtlı. {period} Kalan hak silinmedi.");

        var windowStart = plan.Starts[0].AddDays(-1);
        var windowEnd = plan.Starts[^1].AddDays(1);
        var personal = await dbContext.PersonalTrainingSessions.AsNoTracking()
            .Where(s => (s.TrainerId == trainerId || s.MemberProfileId == member.Id) &&
                s.Status == PersonalTrainingSessionStatus.Scheduled && s.StartsAtUtc >= windowStart && s.StartsAtUtc < windowEnd)
            .Select(s => new { s.StartsAtUtc, s.DurationMinutes }).ToListAsync();
        var groups = await dbContext.ClassSessions.AsNoTracking()
            .Where(s => s.GroupClass.IsActive && s.GroupClass.TrainerId == trainerId && s.Status == ClassSessionStatus.Scheduled &&
                s.StartsAtUtc >= windowStart && s.StartsAtUtc < windowEnd)
            .Select(s => new { s.StartsAtUtc, s.GroupClass.DurationMinutes }).ToListAsync();
        var appointments = await dbContext.AppointmentRequests.AsNoTracking()
            .Where(x => x.ScheduledTrainerId == trainerId && x.ScheduleStatus == AppointmentScheduleStatus.Scheduled &&
                x.ScheduledAtUtc >= windowStart && x.ScheduledAtUtc < windowEnd)
            .Select(x => new { x.ScheduledAtUtc, x.DurationMinutes }).ToListAsync();
        foreach (var start in plan.Starts)
        {
            var finish = start.AddMinutes(input.DurationMinutes);
            if (personal.Any(s => s.StartsAtUtc < finish && s.StartsAtUtc.AddMinutes(s.DurationMinutes) > start) ||
                groups.Any(s => s.StartsAtUtc < finish && s.StartsAtUtc.AddMinutes(s.DurationMinutes) > start) ||
                appointments.Any(x => x.ScheduledAtUtc < finish && x.ScheduledAtUtc!.Value.AddMinutes(x.DurationMinutes) > start))
                return Fail($"{ClubTime.ToLocal(start):dd.MM.yyyy HH:mm} saatinde eğitmen / üyenin dersi veya eğitmenin ön görüşmesi var. Hiçbir ders eklenmedi.");
        }
        plan.Succeeded = true;
        plan.Message = plan.AvailableLessons is { } limit && plan.LessonCount < limit
            ? $"{limit - plan.LessonCount} hak bu programa eklenemedi; kalan hak silinmedi. {period} " +
                $"Seçilen {monday:dd.MM.yyyy} haftasından itibaren {candidates.Count} yeni tarih bulundu; " +
                $"{monthlyLimitDates} tarih aylık sınıra takıldı. Başlangıç haftasını veya günleri kontrol edin."
            : "Program kullanılabilir ders hakkıyla sınırlandırıldı; kalan hak kayıt sırasında tekrar kontrol edilir.";
        return plan;
    }

    private static (int Year, int Month) Month(DateTime utc)
    { var date = ClubTime.ToLocal(utc); return (date.Year, date.Month); }
}
