namespace NO23.Web.ViewModels.Member;

public class CalorieCalculatorPageViewModel
{
    public CalorieCalculatorInputViewModel CalculatorInput { get; init; } = new();

    public CalorieRecommendationViewModel? Recommendation { get; init; }
}
