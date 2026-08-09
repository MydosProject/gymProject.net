namespace NO23.Web.Domain.Entities;

public class Trainer
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Specialty { get; set; } = string.Empty;

    public string? Certifications { get; set; }

    public string? Bio { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public string? ApplicationUserId { get; set; }

    public ApplicationUser? ApplicationUser { get; set; }

    public ICollection<GroupClass> GroupClasses { get; set; } = [];

    public ICollection<PersonalTrainingRequest> PersonalTrainingRequests { get; set; } = [];

    public ICollection<TrainerConversation> TrainerConversations { get; set; } = [];
}
