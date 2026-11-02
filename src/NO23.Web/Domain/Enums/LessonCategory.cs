using System.ComponentModel.DataAnnotations;
namespace NO23.Web.Domain.Enums;

public enum LessonCategory
{
    [Display(Name = "Birebir PT")] PersonalTraining = 1,
    [Display(Name = "Performance")] Performance = 2,
    [Display(Name = "Reformer")] Reformer = 3,
    [Display(Name = "Genel grup")] Group = 4,
    [Display(Name = "Kids Club")] Kids = 5
}
