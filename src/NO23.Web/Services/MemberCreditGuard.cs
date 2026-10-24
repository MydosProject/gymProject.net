using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;

namespace NO23.Web.Services;

public static class MemberCreditGuard
{
    public static async Task LockAsync(ApplicationDbContext db, int memberId)
    {
        if (!db.Database.IsRelational()) return;
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Hak kilidi işlem içinde alınmalıdır.");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"MemberProfiles\" SET \"UpdatedAtUtc\" = \"UpdatedAtUtc\" WHERE \"Id\" = {memberId}");
    }
}
