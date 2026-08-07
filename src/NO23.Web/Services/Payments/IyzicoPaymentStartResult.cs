namespace NO23.Web.Services.Payments;

public sealed record IyzicoPaymentStartResult(
    bool Succeeded,
    int? OrderId,
    int? PaymentTransactionId,
    string? RedirectUrl,
    string? ErrorMessage)
{
    public static IyzicoPaymentStartResult Success(
        int orderId,
        int paymentTransactionId,
        string redirectUrl)
    {
        return new IyzicoPaymentStartResult(
            true,
            orderId,
            paymentTransactionId,
            redirectUrl,
            null);
    }

    public static IyzicoPaymentStartResult Fail(
        string errorMessage,
        int? orderId = null,
        int? paymentTransactionId = null)
    {
        return new IyzicoPaymentStartResult(
            false,
            orderId,
            paymentTransactionId,
            null,
            errorMessage);
    }
}