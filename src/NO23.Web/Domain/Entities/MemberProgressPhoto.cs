using System.ComponentModel.DataAnnotations;

namespace NO23.Web.Domain.Entities;

public class MemberProgressPhoto
{
    public int Id { get; set; }
    public int MemberProfileId { get; set; }
    public MemberProfile MemberProfile { get; set; } = null!;
    public DateOnly TakenOn { get; set; }
    [MaxLength(200)] public string? Caption { get; set; }
    [MaxLength(30)] public string ContentType { get; set; } = string.Empty;
    public byte[] Data { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
