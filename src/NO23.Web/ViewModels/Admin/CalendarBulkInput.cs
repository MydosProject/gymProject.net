using System.ComponentModel.DataAnnotations;
using NO23.Web.Services;

namespace NO23.Web.ViewModels.Admin;

public enum CalendarBulkOperation { UpdateTime = 1, Delete = 2 }

public class CalendarBulkInput
{
    public int[] PersonalIds { get; set; } = [];
    public int[] GroupIds { get; set; } = [];
    [EnumDataType(typeof(CalendarBulkOperation))] public CalendarBulkOperation Operation { get; set; }
    public TimeOnly? Time { get; set; }
    public DateTime Week { get; set; } = ClubTime.Now.Date;
    public int? TrainerId { get; set; }
    public int? MemberId { get; set; }
    public bool AllMemberLessons { get; set; }
    public DateOnly From { get; set; } = DateOnly.FromDateTime(ClubTime.Now);
    public DateOnly Until { get; set; } = DateOnly.FromDateTime(ClubTime.Now).AddMonths(3);
}
