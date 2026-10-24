using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NO23.Web.Domain.Entities;

namespace NO23.Web.ViewModels.Admin;

public sealed class AppointmentScheduleViewModel
{
    public int Id { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Antrenör")] public int TrainerId { get; set; }
    [Required, Display(Name = "Kesin tarih ve saat")] public DateTime StartsAt { get; set; }
    [Range(15, 240), Display(Name = "Süre (dakika)")] public int DurationMinutes { get; set; } = 30;
    [StringLength(1000), Display(Name = "İşlem notu")] public string? Note { get; set; }
    [Required] public Guid? ExpectedVersion { get; set; }
    [ValidateNever] public AppointmentRequest Request { get; set; } = null!;
    [ValidateNever] public List<SelectListItem> Trainers { get; set; } = [];
}
