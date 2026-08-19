using System.ComponentModel.DataAnnotations;

namespace NO23.Web.Domain.Enums;

public enum MembershipPackageChangeRequestStatus
{
    [Display(Name = "Bekliyor")]
    Pending = 1,

    [Display(Name = "Onaylandı")]
    Approved = 2,

    [Display(Name = "Reddedildi")]
    Rejected = 3,

    [Display(Name = "İptal edildi")]
    Cancelled = 4
}
