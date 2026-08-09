using Microsoft.AspNetCore.Identity;

namespace NO23.Web.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAtUtc { get; set; }

    public MemberProfile? MemberProfile { get; set; }

    public Trainer? TrainerProfile { get; set; }

    public ICollection<TrainerMessage> SentTrainerMessages { get; set; } = [];
}
