using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class AppointmentScheduleHistory
{
    public int Id { get; set; }
    public int AppointmentRequestId { get; set; }
    public AppointmentRequest AppointmentRequest { get; set; } = null!;
    public DateTime? PreviousStartsAtUtc { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public int? PreviousTrainerId { get; set; }
    public int? TrainerId { get; set; }
    public int PreviousDurationMinutes { get; set; }
    public int DurationMinutes { get; set; }
    public AppointmentScheduleStatus? PreviousStatus { get; set; }
    public AppointmentScheduleStatus Status { get; set; }
    [MaxLength(450)] public string ChangedByUserId { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Note { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}
