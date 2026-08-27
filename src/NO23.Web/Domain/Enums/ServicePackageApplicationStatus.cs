using System.ComponentModel.DataAnnotations;

namespace NO23.Web.Domain.Enums;

public enum ServicePackageApplicationStatus
{
    [Display(Name = "Yeni")]
    Pending = 1,

    [Display(Name = "İletişime geçildi")]
    Contacted = 2,

    [Display(Name = "Onaylandı")]
    Approved = 3,

    [Display(Name = "Kapatıldı")]
    Closed = 4
}
