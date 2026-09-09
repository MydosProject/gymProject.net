using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class AppointmentRequest
{
    public int Id { get; set; }
    [MaxLength(160)] public string FullName { get; set; } = string.Empty;
    [MaxLength(40)] public string PhoneNumber { get; set; } = string.Empty;
    [MaxLength(80)] public string Service { get; set; } = string.Empty;
    // Requested wall-clock date/time in Turkey, not a confirmed calendar booking.
    public DateOnly PreferredDate { get; set; }
    public TimeOnly PreferredTime { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
    public ServicePackageApplicationStatus Status { get; set; } = ServicePackageApplicationStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
