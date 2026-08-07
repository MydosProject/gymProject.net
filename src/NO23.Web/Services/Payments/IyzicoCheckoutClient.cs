using System.Globalization;
using System.Text.Json;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.Extensions.Options;
using SdkOptions = Iyzipay.Options;

namespace NO23.Web.Services.Payments;

public sealed class IyzicoCheckoutClient : IIyzicoCheckoutClient
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IyzicoOptions settings;
    private readonly SdkOptions sdkOptions;

    public IyzicoCheckoutClient(IOptions<IyzicoOptions> options)
    {
        settings = options.Value;

        sdkOptions = new SdkOptions
        {
            BaseUrl = settings.BaseUrl,
            ApiKey = settings.ApiKey,
            SecretKey = settings.SecretKey
        };
    }

    public async Task<IyzicoCheckoutInitializeResult> InitializeAsync(
        IyzicoCheckoutInitializeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!settings.Enabled)
        {
            return new IyzicoCheckoutInitializeResult
            {
                Succeeded = false,
                ErrorMessage = "iyzico ödeme sistemi etkin değil."
            };
        }

        ValidateInitializeRequest(request);

        var sdkRequest = new CreateCheckoutFormInitializeRequest
        {
            Locale = settings.Locale,
            ConversationId = request.ConversationId,
            Price = FormatAmount(request.Price),
            PaidPrice = FormatAmount(request.PaidPrice),
            Currency = settings.Currency,
            BasketId = request.BasketId,
            PaymentGroup = PaymentGroup.PRODUCT.ToString(),
            CallbackUrl = string.IsNullOrWhiteSpace(request.CallbackUrl)
            ? settings.CallbackUrl
            : request.CallbackUrl,
            EnabledInstallments = settings.EnabledInstallments.ToList(),

            Buyer = MapBuyer(request.Buyer),
            ShippingAddress = MapAddress(request.ShippingAddress),
            BillingAddress = MapAddress(request.BillingAddress),

            BasketItems = request.Items
                .Select(MapBasketItem)
                .ToList()
        };

        var response = await CheckoutFormInitialize
            .Create(sdkRequest, sdkOptions)
            .WaitAsync(cancellationToken);

        var hasRedirectInformation =
            !string.IsNullOrWhiteSpace(response.Token) &&
            Uri.TryCreate(
                response.PaymentPageUrl,
                UriKind.Absolute,
                out var paymentPageUri) &&
            paymentPageUri.Scheme == Uri.UriSchemeHttps;

        var succeeded =
            string.Equals(
                response.Status,
                Status.SUCCESS.ToString(),
                StringComparison.OrdinalIgnoreCase) &&
            hasRedirectInformation;

        return new IyzicoCheckoutInitializeResult
        {
            Succeeded = succeeded,
            StatusCode = response.StatusCode,
            ConversationId = response.ConversationId,
            RawStatus = response.Status,
            Token = response.Token,
            TokenExpireTime = response.TokenExpireTime,
            PaymentPageUrl = response.PaymentPageUrl,
            ErrorCode = response.ErrorCode,
            ErrorGroup = response.ErrorGroup,
            ErrorMessage = GetInitializeErrorMessage(
                response,
                hasRedirectInformation),
            RawResponseJson = SerializeInitializeResponse(response)
        };
    }

    public async Task<IyzicoCheckoutRetrieveResult> RetrieveAsync(
        string conversationId,
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            throw new ArgumentException(
                "ConversationId boş olamaz.",
                nameof(conversationId));
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException(
                "Token boş olamaz.",
                nameof(token));
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (!settings.Enabled)
        {
            return new IyzicoCheckoutRetrieveResult
            {
                Succeeded = false,
                ConversationId = conversationId,
                Token = token,
                ErrorMessage = "iyzico ödeme sistemi etkin değil."
            };
        }

        var sdkRequest = new RetrieveCheckoutFormRequest
        {
            Locale = settings.Locale,
            ConversationId = conversationId,
            Token = token
        };

        var response = await CheckoutForm
            .Retrieve(sdkRequest, sdkOptions)
            .WaitAsync(cancellationToken);

        var succeeded = string.Equals(
            response.Status,
            Status.SUCCESS.ToString(),
            StringComparison.OrdinalIgnoreCase);

        return new IyzicoCheckoutRetrieveResult
        {
            Succeeded = succeeded,
            StatusCode = response.StatusCode,
            ConversationId = response.ConversationId,
            RawStatus = response.Status,
            Token = response.Token,
            PaymentId = response.PaymentId,
            PaymentStatus = response.PaymentStatus,
            FraudStatus = response.FraudStatus,
            BasketId = response.BasketId,
            Price = response.Price,
            PaidPrice = response.PaidPrice,
            Currency = response.Currency,
            ErrorCode = response.ErrorCode,
            ErrorGroup = response.ErrorGroup,
            ErrorMessage = string.IsNullOrWhiteSpace(response.ErrorMessage)
                ? succeeded
                    ? null
                    : "iyzico ödeme sonucu alınamadı."
                : response.ErrorMessage,
            RawResponseJson = SerializeRetrieveResponse(response)
        };
    }

    private static Buyer MapBuyer(IyzicoCheckoutBuyer buyer)
    {
        return new Buyer
        {
            Id = buyer.Id,
            Name = buyer.Name,
            Surname = buyer.Surname,
            IdentityNumber = buyer.IdentityNumber,
            Email = buyer.Email,
            GsmNumber = buyer.GsmNumber,
            RegistrationAddress = buyer.RegistrationAddress,
            City = buyer.City,
            Country = buyer.Country,
            ZipCode = buyer.ZipCode,
            Ip = buyer.IpAddress,
            RegistrationDate = FormatDateTime(buyer.RegistrationDateUtc),
            LastLoginDate = FormatDateTime(buyer.LastLoginDateUtc)
        };
    }

    private static Address MapAddress(IyzicoCheckoutAddress address)
    {
        return new Address
        {
            ContactName = address.ContactName,
            Description = address.Description,
            City = address.City,
            Country = address.Country,
            ZipCode = address.ZipCode
        };
    }

    private static BasketItem MapBasketItem(IyzicoCheckoutItem item)
    {
        return new BasketItem
        {
            Id = item.Id,
            Name = item.Name,
            Category1 = item.Category1,
            Category2 = item.Category2,
            Price = FormatAmount(item.Price),
            ItemType = item.ItemType == IyzicoCheckoutItemType.Virtual
                ? BasketItemType.VIRTUAL.ToString()
                : BasketItemType.PHYSICAL.ToString()
        };
    }

    private static void ValidateInitializeRequest(
        IyzicoCheckoutInitializeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ConversationId))
        {
            throw new ArgumentException(
                "ConversationId boş olamaz.",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.BasketId))
        {
            throw new ArgumentException(
                "BasketId boş olamaz.",
                nameof(request));
        }

        if (request.Price <= 0 || request.PaidPrice <= 0)
        {
            throw new ArgumentException(
                "Ödeme tutarı sıfırdan büyük olmalıdır.",
                nameof(request));
        }

        if (request.PaidPrice < request.Price)
        {
            throw new ArgumentException(
                "Ödenen tutar ürün toplamından küçük olamaz.",
                nameof(request));
        }

        if (request.Items.Count == 0)
        {
            throw new ArgumentException(
                "iyzico sepeti boş olamaz.",
                nameof(request));
        }

        if (request.Items.Any(item => item.Price <= 0))
        {
            throw new ArgumentException(
                "Sepet kalemi tutarı sıfırdan büyük olmalıdır.",
                nameof(request));
        }

        var basketTotal = request.Items.Sum(item => item.Price);

        if (basketTotal != request.Price)
        {
            throw new ArgumentException(
                "Sepet kalemlerinin toplamı Price alanına eşit olmalıdır.",
                nameof(request));
        }
    }

    private static string GetInitializeErrorMessage(
        CheckoutFormInitialize response,
        bool hasRedirectInformation)
    {
        if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
        {
            return response.ErrorMessage;
        }

        if (!string.Equals(
                response.Status,
                Status.SUCCESS.ToString(),
                StringComparison.OrdinalIgnoreCase))
        {
            return "iyzico ödeme formu başlatılamadı.";
        }

        return hasRedirectInformation
            ? string.Empty
            : "iyzico token veya ödeme sayfası adresi döndürmedi.";
    }

    private static string FormatAmount(decimal amount)
    {
        return amount.ToString(
            "0.00",
            CultureInfo.InvariantCulture);
    }

    private static string FormatDateTime(DateTime dateTime)
    {
        return dateTime
            .ToUniversalTime()
            .ToString(
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture);
    }

    private static string SerializeInitializeResponse(
        CheckoutFormInitialize response)
    {
        return JsonSerializer.Serialize(new
        {
            response.Status,
            response.StatusCode,
            response.ConversationId,
            response.Locale,
            response.SystemTime,
            response.ErrorCode,
            response.ErrorMessage,
            response.ErrorGroup,
            response.Token,
            response.TokenExpireTime,
            response.PaymentPageUrl
        }, JsonOptions);
    }

    private static string SerializeRetrieveResponse(
        CheckoutForm response)
    {
        return JsonSerializer.Serialize(new
        {
            response.Status,
            response.StatusCode,
            response.ConversationId,
            response.Locale,
            response.SystemTime,
            response.ErrorCode,
            response.ErrorMessage,
            response.ErrorGroup,
            response.Token,
            response.PaymentId,
            response.PaymentStatus,
            response.FraudStatus,
            response.BasketId,
            response.Price,
            response.PaidPrice,
            response.Currency,
            response.Installment
        }, JsonOptions);
    }
}