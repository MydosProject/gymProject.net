namespace NO23.Web.Services.Payments;

public interface IIyzicoCheckoutClient
{
    Task<IyzicoCheckoutInitializeResult> InitializeAsync(
        IyzicoCheckoutInitializeRequest request,
        CancellationToken cancellationToken = default);

    Task<IyzicoCheckoutRetrieveResult> RetrieveAsync(
        string conversationId,
        string token,
        CancellationToken cancellationToken = default);
}