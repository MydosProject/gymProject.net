using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using Microsoft.AspNetCore.WebUtilities;

namespace NO23.Web.Services.Payments;

public sealed class IyzicoPaymentService(
    ApplicationDbContext dbContext,
    IIyzicoCheckoutClient checkoutClient,
    IOptions<IyzicoOptions> options,
    ILogger<IyzicoPaymentService> logger)
{
    private const string ProviderName = "iyzico";
    private const int LastErrorMaximumLength = 2000;

    private readonly IyzicoOptions settings = options.Value;

    public async Task<IyzicoPaymentStartResult> InitializeAsync(
        int orderId,
        string? ipAddress,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
      
    {   
 
        var order = await LoadOrderAsync(orderId, cancellationToken);

        if (order is null)
        {
            return IyzicoPaymentStartResult.Fail(
                "Ödeme başlatılacak sipariş bulunamadı.");
        }

        if (order.Status != OrderStatus.Pending ||
            order.PaymentStatus != PaymentStatus.Pending)
        {
            return IyzicoPaymentStartResult.Fail(
                "Bu sipariş için yeni ödeme başlatılamaz.",
                order.Id);
        }

        if (order.Items.Count == 0)
        {
            return IyzicoPaymentStartResult.Fail(
                "Sipariş kalemi bulunamadı.",
                order.Id);
        }

        if (order.Subtotal <= 0 || order.Total <= 0)
        {
            return IyzicoPaymentStartResult.Fail(
                "Sipariş tutarı geçerli değil.",
                order.Id);
        }

        var existingTransaction = order.PaymentTransactions
            .Where(payment =>
                payment.Provider == ProviderName &&
                payment.PaymentStatus == PaymentStatus.Pending &&
                !string.IsNullOrWhiteSpace(payment.Token) &&
                !string.IsNullOrWhiteSpace(payment.PaymentPageUrl))
            .OrderByDescending(payment => payment.CreatedAtUtc)
            .FirstOrDefault();

        if (existingTransaction is not null)
        {
            return IyzicoPaymentStartResult.Success(
                order.Id,
                existingTransaction.Id,
                existingTransaction.PaymentPageUrl!);
        }

        

        var paymentTransaction = new PaymentTransaction
        {
            OrderId = order.Id,
            Provider = ProviderName,
            ConversationId = GenerateConversationId(order.Id),
            BasketId = order.OrderNumber,
            PaymentStatus = PaymentStatus.Pending,
            Amount = order.Total,
            Currency = settings.Currency,
            CreatedAtUtc = DateTime.UtcNow
        };

        order.PaymentTransactions.Add(paymentTransaction);

        // iyzico çağrısından önce ödeme denemesi veritabanına yazılır.
        await dbContext.SaveChangesAsync(cancellationToken);

        IyzicoCheckoutInitializeResult initializeResult;

        try
        {
            var callbackUrl =
                string.IsNullOrWhiteSpace(returnUrl)
                    ? settings.CallbackUrl
                    : QueryHelpers.AddQueryString(
                        settings.CallbackUrl,
                        "returnUrl",
                        returnUrl);

            var checkoutRequest = BuildCheckoutRequest(
                order,
                paymentTransaction,
                ipAddress,
                callbackUrl);

            initializeResult = await checkoutClient.InitializeAsync(
                checkoutRequest,
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "iyzico ödeme formu başlatılırken hata oluştu. OrderId: {OrderId}, PaymentTransactionId: {PaymentTransactionId}",
                order.Id,
                paymentTransaction.Id);

            return await MarkInitializationFailedAsync(
                order,
                paymentTransaction,
                initializeResult: null,
                userMessage: "Ödeme sistemiyle iletişim kurulamadı. Lütfen tekrar dene.",
                diagnosticMessage: exception.Message,
                CancellationToken.None);
        }

        if (!initializeResult.Succeeded)
        {
            var errorMessage = string.IsNullOrWhiteSpace(
                initializeResult.ErrorMessage)
                ? "iyzico ödeme formu başlatılamadı."
                : initializeResult.ErrorMessage;

            var diagnosticMessage =
                string.IsNullOrWhiteSpace(initializeResult.ErrorCode)
                    ? errorMessage
                    : $"{initializeResult.ErrorCode}: {errorMessage}";

            return await MarkInitializationFailedAsync(
                order,
                paymentTransaction,
                initializeResult,
                "Ödeme sayfası oluşturulamadı. Lütfen tekrar dene.",
                diagnosticMessage,
                cancellationToken);
        }

        paymentTransaction.Token =
            initializeResult.Token;

        paymentTransaction.PaymentPageUrl =
            initializeResult.PaymentPageUrl;

        var checkoutInitializedAtUtc =
            DateTime.UtcNow;

        paymentTransaction.CheckoutExpiresAtUtc =
            initializeResult.TokenExpireTime is > 0
                ? checkoutInitializedAtUtc.AddSeconds(
                    initializeResult.TokenExpireTime.Value)
                : checkoutInitializedAtUtc.AddMinutes(
                    settings.CheckoutFallbackExpirationMinutes);

        paymentTransaction.RawStatus =
            initializeResult.RawStatus;

        paymentTransaction.RawInitializeResponseJson =
            initializeResult.RawResponseJson;

        paymentTransaction.LastError =
            null;

        paymentTransaction.UpdatedAtUtc =
            checkoutInitializedAtUtc;

        await ClearMemberCartAsync(
            order.MemberProfileId,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return IyzicoPaymentStartResult.Success(
            order.Id,
            paymentTransaction.Id,
            initializeResult.PaymentPageUrl!);
    }

    public async Task<IyzicoPaymentCallbackResult> HandleCallbackAsync(
    string token,
    CancellationToken cancellationToken = default)
    {
    if (string.IsNullOrWhiteSpace(token))
    {
        return IyzicoPaymentCallbackResult.Fail(
            "Ödeme token bilgisi bulunamadı.");
    }

    var paymentTransaction =
        await dbContext.PaymentTransactions
            .Include(payment => payment.Order)
                .ThenInclude(order => order.Items)
                    .ThenInclude(item => item.ShopProduct)
            .FirstOrDefaultAsync(
                payment =>
                    payment.Provider == ProviderName &&
                    payment.Token == token,
                cancellationToken);

    if (paymentTransaction is null)
    {
        return IyzicoPaymentCallbackResult.Fail(
            "Ödeme işlemi bulunamadı.");
    }

    var order = paymentTransaction.Order;

    // Aynı başarılı callback tekrar gelirse işlemi tekrar yapma.
    if (paymentTransaction.PaymentStatus == PaymentStatus.Paid &&
        order.PaymentStatus == PaymentStatus.Paid)
    {
        return IyzicoPaymentCallbackResult.Success(
            order.Id,
            paymentTransaction.Id);
    }

    var callbackReceivedAtUtc = DateTime.UtcNow;

    paymentTransaction.CallbackReceivedAtUtc =
        callbackReceivedAtUtc;

    paymentTransaction.UpdatedAtUtc =
        callbackReceivedAtUtc;

    IyzicoCheckoutRetrieveResult retrieveResult;

    try
    {
        retrieveResult =
            await checkoutClient.RetrieveAsync(
                paymentTransaction.ConversationId,
                token,
                cancellationToken);
    }
    catch (Exception exception)
    {
        logger.LogError(
            exception,
            "iyzico ödeme sonucu alınırken hata oluştu. OrderId: {OrderId}, PaymentTransactionId: {PaymentTransactionId}",
            order.Id,
            paymentTransaction.Id);

        // Burada ödeme Failed yapılmıyor.
        //
        // Çünkü iyzico'ya erişememek,
        // ödemenin gerçekten başarısız olduğu anlamına gelmez.
        paymentTransaction.LastError =
            TruncateLastError(exception.Message);

        await dbContext.SaveChangesAsync(
            CancellationToken.None);

        return IyzicoPaymentCallbackResult.Fail(
            "Ödeme sonucu doğrulanamadı. Lütfen tekrar deneyin.",
            order.Id,
            paymentTransaction.Id);
    }

    paymentTransaction.RawStatus =
        retrieveResult.RawStatus;

    paymentTransaction.PaymentId =
        retrieveResult.PaymentId;

    paymentTransaction.FraudStatus =
        retrieveResult.FraudStatus;

    paymentTransaction.RawRetrieveResponseJson =
        retrieveResult.RawResponseJson;

    paymentTransaction.UpdatedAtUtc =
        DateTime.UtcNow;

    // iyzico isteğin kendisini işleyememişse ödeme hakkında
    // kesin Failed kararı vermiyoruz.
    if (!retrieveResult.Succeeded)
    {
        var errorMessage =
            string.IsNullOrWhiteSpace(
                retrieveResult.ErrorMessage)
                ? "iyzico ödeme sonucu alınamadı."
                : retrieveResult.ErrorMessage;

        paymentTransaction.LastError =
            TruncateLastError(errorMessage);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return IyzicoPaymentCallbackResult.Fail(
            "Ödeme sonucu doğrulanamadı.",
            order.Id,
            paymentTransaction.Id);
    }

    // Gelen cevabın gerçekten bizim siparişimize ait olduğunu
    // kontrol et.
    if (!string.IsNullOrWhiteSpace(
            retrieveResult.ConversationId) &&
        !string.Equals(
            retrieveResult.ConversationId,
            paymentTransaction.ConversationId,
            StringComparison.Ordinal))
    {
        paymentTransaction.LastError =
            "iyzico ConversationId eşleşmedi.";

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return IyzicoPaymentCallbackResult.Fail(
            "Ödeme doğrulaması başarısız.",
            order.Id,
            paymentTransaction.Id);
    }

    if (!string.IsNullOrWhiteSpace(
            retrieveResult.BasketId) &&
        !string.Equals(
            retrieveResult.BasketId,
            paymentTransaction.BasketId,
            StringComparison.Ordinal))
    {
        paymentTransaction.LastError =
            "iyzico BasketId eşleşmedi.";

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return IyzicoPaymentCallbackResult.Fail(
            "Ödeme doğrulaması başarısız.",
            order.Id,
            paymentTransaction.Id);
    }

    var paymentSucceeded =
        string.Equals(
            retrieveResult.PaymentStatus,
            "SUCCESS",
            StringComparison.OrdinalIgnoreCase);

    if (paymentSucceeded)
    {
        var completedAtUtc = DateTime.UtcNow;

        paymentTransaction.PaymentStatus =
            PaymentStatus.Paid;

        paymentTransaction.CompletedAtUtc =
            completedAtUtc;

        paymentTransaction.FailedAtUtc = null;

        paymentTransaction.LastError = null;

        paymentTransaction.UpdatedAtUtc =
            completedAtUtc;

        order.PaymentStatus =
            PaymentStatus.Paid;

        order.Status =
            OrderStatus.Confirmed;

        order.UpdatedAtUtc =
            completedAtUtc;

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return IyzicoPaymentCallbackResult.Success(
            order.Id,
            paymentTransaction.Id);
    }

    // Retrieve isteği başarılı ancak ödeme sonucu başarısız.
    var failedAtUtc = DateTime.UtcNow;

    paymentTransaction.PaymentStatus =
        PaymentStatus.Failed;

    paymentTransaction.FailedAtUtc =
        failedAtUtc;

    paymentTransaction.LastError =
        TruncateLastError(
            $"iyzico ödeme sonucu başarısız: {retrieveResult.PaymentStatus ?? "UNKNOWN"}.");

    paymentTransaction.UpdatedAtUtc =
        failedAtUtc;

    order.PaymentStatus =
        PaymentStatus.Failed;

    order.Status =
        OrderStatus.Cancelled;

    order.UpdatedAtUtc =
        failedAtUtc;

    OrderWorkflowService.RestoreShopProductStockOnce(
        order);

    await dbContext.SaveChangesAsync(
        cancellationToken);

    return IyzicoPaymentCallbackResult.Fail(
        "Ödeme işlemi başarısız.",
        order.Id,
        paymentTransaction.Id);
    }


    private async Task<Order?> LoadOrderAsync(
        int orderId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Orders
            .AsSplitQuery()
            .Include(order => order.MemberProfile)
                .ThenInclude(member => member!.ApplicationUser)
            .Include(order => order.Items)
                .ThenInclude(item => item.ShopProduct)
            .Include(order => order.Items)
                .ThenInclude(item => item.KitchenMenuItem)
            .Include(order => order.PaymentTransactions)
            .FirstOrDefaultAsync(
                order => order.Id == orderId,
                cancellationToken);
    }

    private IyzicoCheckoutInitializeRequest BuildCheckoutRequest(
        Order order,
        PaymentTransaction paymentTransaction,
        string? ipAddress,
        string callbackUrl)
    {
        var buyer = BuildBuyer(order, ipAddress);

        var address = new IyzicoCheckoutAddress
        {
            ContactName = order.DeliveryFullName,
            Description = BuildAddressDescription(order),
            City = order.DeliveryCity,
            Country = "Turkey",
            ZipCode = GetZipCode(order.DeliveryPostalCode)
        };

        var basketItems = order.Items
            .OrderBy(item => item.Id)
            .Select(item => new IyzicoCheckoutItem
            {
                Id = $"OI-{item.Id}",
                Name = item.ProductName,
                Category1 = GetItemCategory1(item),
                Category2 = GetItemCategory2(item),
                Price = item.LineTotal,
                ItemType = IyzicoCheckoutItemType.Physical
            })
            .ToList();

        return new IyzicoCheckoutInitializeRequest
        {
            ConversationId = paymentTransaction.ConversationId,
            BasketId = paymentTransaction.BasketId,
            Price = order.Subtotal,
            PaidPrice = order.Total,
            Buyer = buyer,
            CallbackUrl = callbackUrl,
            ShippingAddress = address,
            BillingAddress = address,
            Items = basketItems
        };

    }

    private IyzicoCheckoutBuyer BuildBuyer(
        Order order,
        string? ipAddress)
    {
        var applicationUser =
            order.MemberProfile?.ApplicationUser;

        var email = applicationUser?.Email ??
                    order.GuestEmail;

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException(
                "Ödeme için müşteri e-posta adresi bulunamadı.");
        }

        if (string.IsNullOrWhiteSpace(
                settings.SandboxBuyerIdentityNumber))
        {
            throw new InvalidOperationException(
                "Sandbox alıcı kimlik numarası yapılandırılmamış.");
        }

        var (name, surname) =
            SplitFullName(order.DeliveryFullName);

        var registrationDateUtc =
            applicationUser?.CreatedAtUtc ??
            order.CreatedAtUtc;

        var lastLoginDateUtc =
            applicationUser?.LastLoginAtUtc ??
            registrationDateUtc;

        return new IyzicoCheckoutBuyer
        {
            Id = order.MemberProfileId.HasValue
                ? $"MEMBER-{order.MemberProfileId.Value}"
                : $"GUEST-{order.Id}",
            Name = name,
            Surname = surname,
            IdentityNumber =
                settings.SandboxBuyerIdentityNumber,
            Email = email.Trim(),
            GsmNumber = NormalizePhoneNumber(
                order.DeliveryPhoneNumber),
            RegistrationAddress =
                BuildAddressDescription(order),
            City = order.DeliveryCity,
            Country = "Turkey",
            ZipCode = GetZipCode(
                order.DeliveryPostalCode),
            IpAddress = NormalizeIpAddress(ipAddress),
            RegistrationDateUtc = registrationDateUtc,
            LastLoginDateUtc = lastLoginDateUtc
        };
    }

    private async Task<IyzicoPaymentStartResult>
        MarkInitializationFailedAsync(
            Order order,
            PaymentTransaction paymentTransaction,
            IyzicoCheckoutInitializeResult? initializeResult,
            string userMessage,
            string diagnosticMessage,
            CancellationToken cancellationToken)
    {
        var failedAtUtc = DateTime.UtcNow;

        paymentTransaction.Token =
            initializeResult?.Token;
        paymentTransaction.PaymentPageUrl =
            initializeResult?.PaymentPageUrl;
        paymentTransaction.RawStatus =
            initializeResult?.RawStatus;
        paymentTransaction.RawInitializeResponseJson =
            initializeResult?.RawResponseJson;
        paymentTransaction.PaymentStatus =
            PaymentStatus.Failed;
        paymentTransaction.FailedAtUtc =
            failedAtUtc;
        paymentTransaction.UpdatedAtUtc =
            failedAtUtc;
        paymentTransaction.LastError =
            TruncateLastError(diagnosticMessage);

        order.PaymentStatus = PaymentStatus.Failed;
        order.Status = OrderStatus.Cancelled;
        order.UpdatedAtUtc = failedAtUtc;

        OrderWorkflowService.RestoreShopProductStockOnce(
            order);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return IyzicoPaymentStartResult.Fail(
            userMessage,
            order.Id,
            paymentTransaction.Id);
    }

    private async Task ClearMemberCartAsync(
        int? memberProfileId,
        CancellationToken cancellationToken)
    {
        if (!memberProfileId.HasValue)
        {
            return;
        }

        var cart = await dbContext.ShoppingCarts
            .Include(item => item.Items)
            .FirstOrDefaultAsync(
                item =>
                    item.MemberProfileId ==
                    memberProfileId.Value,
                cancellationToken);

        if (cart is null)
        {
            return;
        }

        dbContext.CartItems.RemoveRange(cart.Items);
        dbContext.ShoppingCarts.Remove(cart);
    }

    private static string GenerateConversationId(
        int orderId)
    {
        return $"NO23-{orderId}-{Guid.NewGuid():N}";
    }

    private static string BuildAddressDescription(
        Order order)
    {
        return $"{order.DeliveryAddressLine}, {order.DeliveryDistrict}";
    }

    private static string GetItemCategory1(
        OrderItem item)
    {
        return item.ItemType switch
        {
            CartItemType.ShopProduct => "Shop",
            CartItemType.KitchenMenuItem => "Kitchen",
            _ => "NO23"
        };
    }

    private static string? GetItemCategory2(
        OrderItem item)
    {
        return item.ItemType switch
        {
            CartItemType.ShopProduct =>
                item.ShopProduct?.Category,

            CartItemType.KitchenMenuItem =>
                item.KitchenMenuItem?.Category.ToString(),

            _ => null
        };
    }

    private static (string Name, string Surname)
        SplitFullName(string fullName)
    {
        var parts = fullName.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return ("NO23", "Customer");
        }

        if (parts.Length == 1)
        {
            return (parts[0], "Customer");
        }

        var name = string.Join(
            " ",
            parts[..^1]);

        return (name, parts[^1]);
    }

    private static string NormalizePhoneNumber(
        string phoneNumber)
    {
        var digits = new string(
            phoneNumber
                .Where(char.IsDigit)
                .ToArray());

        if (digits.StartsWith("0090") &&
            digits.Length == 14)
        {
            return $"+{digits[2..]}";
        }

        if (digits.StartsWith("90") &&
            digits.Length == 12)
        {
            return $"+{digits}";
        }

        if (digits.StartsWith('0') &&
            digits.Length == 11)
        {
            return $"+90{digits[1..]}";
        }

        if (digits.Length == 10)
        {
            return $"+90{digits}";
        }

        return phoneNumber.Trim();
    }

    private static string NormalizeIpAddress(
        string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) ||
            ipAddress == "::1")
        {
            return "127.0.0.1";
        }

        return ipAddress.Trim();
    }

    private static string GetZipCode(
        string? postalCode)
    {
        return string.IsNullOrWhiteSpace(postalCode)
            ? "00000"
            : postalCode.Trim();
    }

    private static string TruncateLastError(
        string message)
    {
        return message.Length <= LastErrorMaximumLength
            ? message
            : message[..LastErrorMaximumLength];
    }
}