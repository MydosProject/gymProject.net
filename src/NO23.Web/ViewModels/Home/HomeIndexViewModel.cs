namespace NO23.Web.ViewModels.Home;

public class HomeIndexViewModel
{
    public IReadOnlyList<NO23.Web.ViewModels.Plans.ServicePackageCardViewModel> MembershipPackages { get; init; } = [];
    public IReadOnlyList<NO23.Web.ViewModels.Member.KitchenSubscriptionPlanViewModel> KitchenPackages { get; init; } = [];
}
