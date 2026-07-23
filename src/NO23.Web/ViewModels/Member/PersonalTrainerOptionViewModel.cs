namespace NO23.Web.ViewModels.Member;

public class PersonalTrainerOptionViewModel
{
    public int Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Specialty { get; init; } = string.Empty;

    public string? Bio { get; init; }
}
