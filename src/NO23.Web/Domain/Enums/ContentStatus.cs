using System.ComponentModel.DataAnnotations;

namespace NO23.Web.Domain.Enums;

public enum ContentStatus
{
    [Display(Name = "Taslak")]
    Draft = 1,

    [Display(Name = "Yayında")]
    Published = 2,

    [Display(Name = "Arşivlendi")]
    Archived = 3
}
