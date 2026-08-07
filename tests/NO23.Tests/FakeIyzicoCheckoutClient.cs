using NO23.Web.Services.Payments;

namespace NO23.Tests;

internal sealed class FakeIyzicoCheckoutClient
    : IIyzicoCheckoutClient
{
    private readonly IyzicoCheckoutInitializeResult?
        initializeResult;

    private readonly IyzicoCheckoutRetrieveResult?
        retrieveResult;

    public FakeIyzicoCheckoutClient(
        IyzicoCheckoutInitializeResult initializeResult)
    {
        this.initializeResult = initializeResult;
    }

    public FakeIyzicoCheckoutClient(
        IyzicoCheckoutRetrieveResult retrieveResult)
    {
        this.retrieveResult = retrieveResult;
    }

    public IyzicoCheckoutInitializeRequest?
        LastInitializeRequest
    {
        get;
        private set;
    }

    public string? LastRetrieveConversationId
    {
        get;
        private set;
    }

    public string? LastRetrieveToken
    {
        get;
        private set;
    }

    public Task<IyzicoCheckoutInitializeResult>
        InitializeAsync(
            IyzicoCheckoutInitializeRequest request,
            CancellationToken cancellationToken = default)
    {
        LastInitializeRequest = request;

        if (initializeResult is null)
        {
            throw new InvalidOperationException(
                "Fake initialize sonucu yapılandırılmadı.");
        }

        return Task.FromResult(initializeResult);
    }

    public Task<IyzicoCheckoutRetrieveResult>
        RetrieveAsync(
            string conversationId,
            string token,
            CancellationToken cancellationToken = default)
    {
        LastRetrieveConversationId =
            conversationId;

        LastRetrieveToken =
            token;

        if (retrieveResult is null)
        {
            throw new InvalidOperationException(
                "Fake retrieve sonucu yapılandırılmadı.");
        }

        return Task.FromResult(retrieveResult);
    }
}