namespace NO23.Web.Domain.Enums;

public enum UserNotificationType
{
    Message = 1,
    PersonalTrainingRequested = 2,
    PersonalTrainingScheduled = 3,
    PersonalTrainingRejected = 4,
    PersonalTrainingCancelled = 5,
    PersonalTrainingCompleted = 6,

    KitchenStockCritical = 7,
    KitchenStockOut = 8,
    ShopStockCritical = 9,
    ShopStockOut = 10,
    OrderStatusChanged = 11,
    GroupClassSessionChanged = 12,
    GroupClassSessionCancelled = 13,
    PersonalTrainingRescheduled = 14
}