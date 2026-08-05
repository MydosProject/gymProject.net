using NO23.Web.Domain.Enums;
using NO23.Web.Extensions;

namespace NO23.Tests;

public class AdminEnumDisplayNameTests
{
    public static IEnumerable<object[]> AdminDisplayNameCases =>
    [
        [ClassSessionStatus.Scheduled, "Planlanmış"],
        [ClassSessionStatus.Completed, "Tamamlandı"],
        [ClassSessionStatus.Cancelled, "İptal edildi"],
        [CommunityEventStatus.Scheduled, "Planlanmış"],
        [CommunityEventStatus.Completed, "Tamamlandı"],
        [CommunityEventStatus.Cancelled, "İptal edildi"],
        [OrderType.OneTime, "Tek seferlik"],
        [OrderType.KitchenSubscription, "Kitchen aboneliği"],
        [OrderStatus.Pending, "Beklemede"],
        [OrderStatus.Confirmed, "Onaylandı"],
        [OrderStatus.Preparing, "Hazırlanıyor"],
        [OrderStatus.OutForDelivery, "Teslimata çıktı"],
        [OrderStatus.Delivered, "Teslim edildi"],
        [OrderStatus.Cancelled, "İptal edildi"],
        [PaymentStatus.Pending, "Ödeme bekleniyor"],
        [PaymentStatus.Paid, "Ödendi"],
        [PaymentStatus.Failed, "Başarısız"],
        [PaymentStatus.Refunded, "İade edildi"],
        [ContentStatus.Draft, "Taslak"],
        [ContentStatus.Published, "Yayında"],
        [ContentStatus.Archived, "Arşivlendi"],
        [ClassDifficultyLevel.Beginner, "Başlangıç"],
        [ClassDifficultyLevel.Intermediate, "Orta"],
        [ClassDifficultyLevel.Advanced, "İleri"],
        [ClassDifficultyLevel.AllLevels, "Tüm seviyeler"],
        [PersonalTrainingRequestStatus.Pending, "Beklemede"],
        [PersonalTrainingRequestStatus.Scheduled, "Planlandı"],
        [PersonalTrainingRequestStatus.Rejected, "Reddedildi"],
        [PersonalTrainingRequestStatus.Cancelled, "İptal edildi"],
        [PersonalTrainingRequestStatus.Completed, "Tamamlandı"]
    ];

    [Theory]
    [MemberData(nameof(AdminDisplayNameCases))]
    public void GetDisplayName_ReturnsTurkishAdminLabels(Enum value, string expected)
    {
        Assert.Equal(expected, value.GetDisplayName());
    }
}
