using NO23.Web.Data;
using NO23.Web.Features.Membership;
using NO23.Web.Services;

namespace NO23.Tests;

internal static class TestMembershipModule
{
    public static MemberDirectoryService CreateDirectory(ApplicationDbContext db, IClubClock? clock = null) =>
        new(db, new LessonCreditSummaryService(db), new MemberPaymentHistoryService(db), clock ?? new ClubClock(TimeProvider.System));
}
