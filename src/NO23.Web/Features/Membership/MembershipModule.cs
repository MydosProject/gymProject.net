using NO23.Web.Services;

namespace NO23.Web.Features.Membership;

public static class MembershipModule
{
    public static IServiceCollection AddMembershipModule(this IServiceCollection services)
    {
        services.AddScoped<LessonCreditSummaryService>();
        services.AddScoped<MemberPaymentHistoryService>();
        services.AddScoped<MemberDirectoryService>();
        return services;
    }
}
