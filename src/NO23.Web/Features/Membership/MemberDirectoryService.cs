using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Features.Membership;

public sealed class MemberDirectoryService(ApplicationDbContext db, LessonCreditSummaryService creditService,
    MemberPaymentHistoryService paymentHistory, IClubClock clock)
{
    public async Task<MemberDirectoryPage> LoadAsync(int page, string? query, string? state)
    {
        var allMembers = db.MemberProfiles
            .AsNoTracking()
            .Include(profile => profile.ApplicationUser)
            .Include(profile => profile.MembershipPackage)
            .Include(profile => profile.ServicePackageVariant)
                .ThenInclude(variant => variant!.ServicePackage)
            .Select(profile => new MemberListItemViewModel
            {
                Id = profile.ApplicationUserId,
                MemberProfileId = profile.Id,
                FullName = ((profile.ApplicationUser.FirstName ?? "") + " " + (profile.ApplicationUser.LastName ?? "")).Trim(),
                Email = profile.ApplicationUser.Email ?? "",
                PhoneNumber = profile.ApplicationUser.PhoneNumber,
                PackageName = profile.ServicePackageVariant == null
                    ? db.ServicePackages
                    .Where(package => package.Category == ServicePackageCategory.Membership && package.IsActive &&
                        package.MembershipPackageId == profile.MembershipPackageId)
                    .OrderBy(package => package.DisplayOrder)
                    .Select(package => package.Name)
                    .FirstOrDefault() ?? profile.MembershipPackage.Name
                    : profile.ServicePackageVariant.ServicePackage.Name + " — " + profile.ServicePackageVariant.Name,
                FamilyCode = profile.FamilyCode,
                SiblingDiscountPercent = profile.SiblingDiscountPercent,
                FitnessGoal = profile.FitnessGoal,
                RemainingClassCredits = profile.RemainingClassCredits,
                MembershipEndsAtUtc = profile.MembershipEndsAtUtc,
                IsKitchenOnly = profile.MembershipPackage.Code == MembershipPackageCode.KitchenOnly,
                IsUnlimitedPackage = profile.MembershipPackage.Code != MembershipPackageCode.KitchenOnly &&
                    profile.ServicePackageVariantId == null && profile.MembershipPackage.WeeklyClassLimit == null,
                AssignedTrainerId = profile.AssignedTrainerId,
                AssignedTrainerName = profile.AssignedTrainer == null
                    ? null
                    : profile.AssignedTrainer.FirstName + " " + profile.AssignedTrainer.LastName,
                CreatedAtUtc = profile.CreatedAtUtc
            });
        var overallCount = await allMembers.CountAsync();
        var packageCount = await allMembers.Select(x => x.PackageName).Distinct().CountAsync();
        var month = ClubTime.ToUtc(new DateTime(clock.Today.Year, clock.Today.Month, 1));
        var newThisMonthCount = await allMembers.CountAsync(x => x.CreatedAtUtc >= month && x.CreatedAtUtc < month.AddMonths(1));
        var filtered = allMembers;
        if (!string.IsNullOrEmpty(query))
        {
            var normalized = query.ToLower(CultureInfo.GetCultureInfo("tr-TR"));
            int.TryParse(query.TrimStart('#'), out var memberId);
            var phone = new string(query.Where(char.IsDigit).ToArray());
            var numericPhone = phone.Length >= 2 && query.All(c => char.IsDigit(c) || "+- ()".Contains(c));
            filtered = filtered.Where(x => x.MemberProfileId == memberId ||
                x.FullName.Replace("I", "ı").Replace("İ", "i").ToLower().Contains(normalized) ||
                x.Email.ToLower().Contains(normalized) ||
                (numericPhone && x.PhoneNumber != null && x.PhoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Contains(phone)));
        }
        var now = ClubTime.ToUtc(clock.Now);
        filtered = state switch
        {
            "kitchen" => filtered.Where(x => x.IsKitchenOnly),
            "expired" => filtered.Where(x => !x.IsKitchenOnly && x.MembershipEndsAtUtc <= now),
            "active" => filtered.Where(x => !x.IsKitchenOnly && (!x.MembershipEndsAtUtc.HasValue || x.MembershipEndsAtUtc > now)),
            _ => filtered
        };
        var navigation = new NO23.Web.ViewModels.ListNavigationViewModel(await filtered.CountAsync(), page, query, state,
            [new("Süresi devam eden spor", "active"), new("Süresi biten spor", "expired"), new("Kitchen hesapları", "kitchen")]);

        var members = await filtered.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.MemberProfileId)
            .Skip(navigation.Skip).Take(NO23.Web.ViewModels.ListNavigationViewModel.PageSize).ToListAsync();

        var creditSummaries = await creditService.LoadAsync(members.Select(m => m.MemberProfileId));
        foreach (var member in members) member.Credits = creditSummaries.GetValueOrDefault(member.MemberProfileId);
        var payments = (await paymentHistory.LoadAsync(members.Select(m => m.MemberProfileId).ToArray()))
            .ToLookup(o => o.MemberId);
        foreach (var member in members) member.Payments = MemberPaymentSummary.Calculate(payments[member.MemberProfileId]);
        var trainers = new SelectList(
            await db.Trainers.AsNoTracking().Where(item => item.IsActive)
                .OrderBy(item => item.FirstName).ThenBy(item => item.LastName)
                .Select(item => new { item.Id, Name = item.FirstName + " " + item.LastName })
                .ToListAsync(), "Id", "Name");
        return new MemberDirectoryPage(members, navigation, overallCount, packageCount, newThisMonthCount, trainers);
    }
}

public sealed record MemberDirectoryPage(IReadOnlyList<MemberListItemViewModel> Members,
    ListNavigationViewModel Navigation, int OverallCount, int PackageCount, int NewThisMonthCount, SelectList Trainers);