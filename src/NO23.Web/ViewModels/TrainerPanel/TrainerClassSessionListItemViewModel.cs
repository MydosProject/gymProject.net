namespace NO23.Web.ViewModels.TrainerPanel;

public class TrainerClassSessionListItemViewModel
{
    public int SessionId { get; set; }

    public string ClassName { get; set; } = string.Empty;

    public DateTime StartsAtUtc { get; set; }

    public int DurationMinutes { get; set; }

    public int Capacity { get; set; }

    public int ReservedCount { get; set; }

    public string StatusDisplayName { get; set; } = string.Empty;
}