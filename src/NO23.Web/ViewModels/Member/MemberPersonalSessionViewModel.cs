namespace NO23.Web.ViewModels.Member;

public sealed record MemberPersonalSessionViewModel(int Id, DateTime StartsAtUtc, int DurationMinutes, string TrainerName);
