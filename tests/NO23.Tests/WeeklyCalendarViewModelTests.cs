using NO23.Web.Domain.Entities;
using NO23.Web.Services;
using NO23.Web.ViewModels.Admin;

namespace NO23.Tests;

public class WeeklyCalendarViewModelTests
{
    [Fact]
    public void EntriesFor_MergesGroupAndPersonalSessionsInStartTimeOrder()
    {
        var date = new DateTime(2026, 9, 22);
        var model = new WeeklyCalendarViewModel
        {
            Groups = [new ClassSession { StartsAtUtc = ClubTime.ToUtc(date.AddHours(8)) }],
            Personal = [new PersonalTrainingSession { StartsAtUtc = ClubTime.ToUtc(date.AddHours(7)) }]
        };

        var entries = model.EntriesFor(date);

        Assert.Equal(2, entries.Count);
        Assert.NotNull(entries[0].Personal);
        Assert.NotNull(entries[1].Group);
    }
}
