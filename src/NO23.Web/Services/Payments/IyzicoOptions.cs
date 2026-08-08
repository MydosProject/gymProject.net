namespace NO23.Web.Services.Payments;

public sealed class IyzicoOptions
{
    public const string SectionName = "Iyzico";

    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } =
        "https://sandbox-api.iyzipay.com";

    public string ApiKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string CallbackUrl { get; set; } = string.Empty;

    public string Currency { get; set; } = "TRY";

    public string Locale { get; set; } = "tr";

    public int[] EnabledInstallments { get; set; } = [];

    public string SandboxBuyerIdentityNumber { get; set; } =
        "74300864791";

    public int CheckoutFallbackExpirationMinutes { get; set; } = 30;
}