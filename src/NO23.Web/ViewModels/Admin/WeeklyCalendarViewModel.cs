using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using NO23.Web.Domain.Entities;
using NO23.Web.ViewModels.TrainerPanel;

namespace NO23.Web.ViewModels.Admin;

public class WeeklyCalendarViewModel
{
    public DateTime Week { get; set; }
    public int? TrainerId { get; set; }
    public List<SelectListItem> Trainers { get; set; } = [];
    public List<SelectListItem> Classes { get; set; } = [];
    public List<SelectListItem> Members { get; set; } = [];
    public List<ClassSession> Groups { get; set; } = [];
    public List<PersonalTrainingSession> Personal { get; set; } = [];
}

public class WeeklyGroupInput
{
    [Range(1, int.MaxValue)] public int GroupClassId { get; set; }
    public DateTime Week { get; set; } = ClubWeek();
    public DayOfWeek[] Days { get; set; } = [];
    public TimeOnly Time { get; set; } = new(18, 0);
    [Range(1, 12)] public int Weeks { get; set; } = 1;
    [Range(1, 200)] public int? Capacity { get; set; }
    private static DateTime ClubWeek() => NO23.Web.Services.ClubTime.Monday(NO23.Web.Services.ClubTime.Now);
}

public class AdminPersonalSessionInput : CreateTrainerSessionViewModel
{
    [Range(1, int.MaxValue)] public int TrainerId { get; set; }
}

public class AdminSessionUpdateInput : UpdateTrainerSessionViewModel
{
    [Range(1, int.MaxValue)] public int TrainerId { get; set; }
}
