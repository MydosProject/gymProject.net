namespace NO23.Web.Services.Payments;

public sealed record IyzicoPaymentCallbackResult(
    bool Succeeded,
    int? OrderId,
    int? PaymentTransactionId,
    string? ErrorMessage)
{
    public static IyzicoPaymentCallbackResult Success(
        int orderId,
        int paymentTransactionId)
    {
        return new IyzicoPaymentCallbackResult(
            true,
            orderId,
            paymentTransactionId,
            null);
    }

    public static IyzicoPaymentCallbackResult Fail(
        string errorMessage,
        int? orderId = null,
        int? paymentTransactionId = null)
    {
        return new IyzicoPaymentCallbackResult(
            false,
            orderId,
            paymentTransactionId,
            errorMessage);
    }
}