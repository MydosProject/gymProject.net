namespace NO23.Web.ViewModels.Trainers;

public class TrainerCardViewModel
{
    public int Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Specialty { get; init; } = string.Empty;

    public string? Certifications { get; init; }

    public string? Bio { get; init; }

    public IReadOnlyList<string> Classes { get; init; } = [];
}
