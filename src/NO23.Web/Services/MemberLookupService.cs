using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;

namespace NO23.Web.Services;

public sealed class MemberLookupService(ApplicationDbContext db)
{
    public async Task<IReadOnlyList<MemberLookupResult>> SearchAsync(string? query, int? trainerId = null,
        CancellationToken cancellationToken = default)
    {
        var term = query?.Trim();
        if (string.IsNullOrEmpty(term) || term.Length < 2 || term.Length > 80) return [];
        var normalized = term.ToLower(CultureInfo.GetCultureInfo("tr-TR"));
        var phone = new string(term.Where(char.IsDigit).ToArray());
        var numericPhone = phone.Length >= 2 && term.All(c => char.IsDigit(c) || "+- ()".Contains(c));
        int.TryParse(term.TrimStart('#'), out var memberId);
        return await db.MemberProfiles.AsNoTracking()
            .Where(member => !trainerId.HasValue || member.AssignedTrainerId == trainerId)
            .Where(member => member.Id == memberId ||
                (member.ApplicationUser.FirstName + " " + member.ApplicationUser.LastName)
                    .Replace("I", "ı").Replace("İ", "i").ToLower().Contains(normalized) ||
                (numericPhone && member.ApplicationUser.PhoneNumber != null &&
                    member.ApplicationUser.PhoneNumber.Replace(" ", "").Replace("-", "")
                        .Replace("(", "").Replace(")", "").Contains(phone)))
            .OrderBy(member => member.ApplicationUser.FirstName).ThenBy(member => member.ApplicationUser.LastName)
            .ThenBy(member => member.Id).Take(20)
            .Select(member => new MemberLookupResult(member.Id,
                member.ApplicationUser.FirstName + " " + member.ApplicationUser.LastName,
                member.ApplicationUser.PhoneNumber, member.ServicePackageVariantId != null
                    ? member.ServicePackageVariant!.ServicePackage.Name : member.MembershipPackage.Name))
            .ToListAsync(cancellationToken);
    }
}

public record MemberLookupResult(int Id, string Name, string? Phone, string Package);
