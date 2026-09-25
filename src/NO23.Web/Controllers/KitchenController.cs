using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.GuestOrders;
using NO23.Web.ViewModels;
using NO23.Web.ViewModels.Member;
using NO23.Web.Services.Payments;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Security.Claims;

namespace NO23.Web.Controllers;

[AllowAnonymous]
public class KitchenController(
    ApplicationDbContext dbContext,
    CommerceService commerceService,
    IyzicoPaymentService iyzicoPaymentService,
    IOptions<IyzicoOptions> paymentOptions,
    IOptions<ClubPickupOptions> clubPickupOptions,
    CalorieCalculatorService calorieCalculator) : Controller
{
    private const string PublicCalculatorInputSessionKey = "NO23.PublicKitchen.CalculatorInput";
    private const string PublicCalculatorResultSessionKey = "NO23.PublicKitchen.CalculatorResult";
    private readonly IyzicoOptions paymentSettings = paymentOptions.Value;
    private readonly ClubPickupOptions clubPickupSettings = clubPickupOptions.Value;

    public async Task<IActionResult> Index()
    {
        var menuItems = await dbContext.KitchenMenuItems
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .Select(item => new
            {
                ItemId = item.Id,
                ItemName = item.Name,
                Description = item.Description,
                item.Category,
                UnitPrice = item.UnitPrice,
                Calories = item.Calories,
                ProteinGrams = item.ProteinGrams,
                CarbohydrateGrams = item.CarbohydrateGrams,
                FatGrams = item.FatGrams,
                Ingredients = item.Ingredients,
                AllergenNames = item.MenuItemAllergens.OrderBy(x => x.KitchenAllergen.DisplayOrder)
                    .Select(x => x.KitchenAllergen.Name).ToList()
            })
            .ToListAsync();

        var model = menuItems
            .Select(item => new GuestOrderPageViewModel
            {
                ItemId = item.ItemId,
                ItemName = item.ItemName,
                Description = item.Description,
                Category = GetMenuCategoryLabel(item.Category),
                UnitPrice = item.UnitPrice,
                Calories = item.Calories,
                ProteinGrams = item.ProteinGrams,
                CarbohydrateGrams = item.CarbohydrateGrams,
                FatGrams = item.FatGrams,
                Ingredients = item.Ingredients,
                Allergens = string.Join(", ", item.AllergenNames)
            })
            .ToList();

        var subscriptionPlans = await dbContext.KitchenSubscriptionPackages
            .AsNoTracking()
            .Where(package => package.IsActive && (package.Days == 5 || package.Days == 20))
            .OrderBy(package => package.DisplayOrder)
            .Select(package => new KitchenSubscriptionPlanViewModel
            {
                Plan = package.Plan,
                Name = package.Name,
                Description = package.Description,
                Days = package.Days,
                UnitPrice = package.UnitPrice,
                TwoMainMealsPrice = package.TwoMainMealsPrice,
                ThreeMainMealsPrice = package.ThreeMainMealsPrice,
                DailyDeliveryFee = package.DailyDeliveryFee,
                IsActive = package.IsActive
            })
            .ToListAsync();

        return View(new KitchenPublicPageViewModel
        {
            MenuItems = model,
            SubscriptionPlans = subscriptionPlans,
            CalculatorInput = ReadSession<CalorieCalculatorInputViewModel>(PublicCalculatorInputSessionKey)
                ?? new CalorieCalculatorInputViewModel(),
            Recommendation = ReadSession<CalorieRecommendationViewModel>(PublicCalculatorResultSessionKey)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CalculateCalories(
        [Bind(Prefix = "CalculatorInput")] CalorieCalculatorInputViewModel input)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Kalori hesabı için boy, kilo, yaş ve hedef bilgilerini kontrol et.";
            return LocalRedirect($"{Url.Action(nameof(Index))}#calorie-calculator");
        }

        var result = calorieCalculator.Calculate(new CalorieCalculationRequest
        {
            HeightCm = input.HeightCm,
            WeightKg = input.WeightKg,
            Age = input.Age,
            Gender = input.Gender,
            ActivityLevel = input.ActivityLevel,
            Goal = input.Goal
        });
        var recommendation = new CalorieRecommendationViewModel
        {
            Goal = input.Goal,
            DailyCalories = result.DailyCalories,
            ProteinGrams = result.ProteinGrams,
            CarbohydrateGrams = result.CarbohydrateGrams,
            FatGrams = result.FatGrams
        };

        HttpContext.Session.SetString(
            PublicCalculatorInputSessionKey,
            JsonSerializer.Serialize(input));
        HttpContext.Session.SetString(
            PublicCalculatorResultSessionKey,
            JsonSerializer.Serialize(recommendation));

        return LocalRedirect($"{Url.Action(nameof(Index))}#plans");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartSubscription(
        KitchenSubscriptionPlan plan,
        KitchenMealSlot[]? selectedMeals,
        OrderDeliveryMethod deliveryMethod)
    {
        var package = await dbContext.KitchenSubscriptionPackages.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Plan == plan && x.IsActive);
        KitchenSubscriptionPriceQuote? quote = null;
        string? error = null;
        if (package is null || !KitchenSubscriptionPricing.TryCalculate(package, selectedMeals, out quote, out error))
        {
            TempData["ErrorMessage"] = error ?? "Seçilen Kitchen paketi bulunamadı.";
            return LocalRedirect($"{Url.Action(nameof(Index))}#plans");
        }

        if (!Enum.IsDefined(deliveryMethod)) deliveryMethod = OrderDeliveryMethod.ClubPickup;
        var storedInput = ReadSession<CalorieCalculatorInputViewModel>(
            PublicCalculatorInputSessionKey);
        var recommendation = ReadSession<CalorieRecommendationViewModel>(
            PublicCalculatorResultSessionKey);
        if (storedInput is null || recommendation is null)
        {
            TempData["ErrorMessage"] =
                "Kitchen planını seçmeden önce kalori ve makro hedefini hesaplamalısın.";
            return LocalRedirect($"{Url.Action(nameof(Index))}#calorie-calculator");
        }

        HttpContext.Session.SetString(PendingKitchenSubscription.SessionKey, JsonSerializer.Serialize(
            new PendingKitchenSubscriptionData(plan, selectedMeals!.Distinct().ToArray(), deliveryMethod, storedInput, recommendation)));

        var resumeUrl = Url.Action("ResumePendingSubscription", "Kitchen", new { area = "Member" }) ?? "/Member/Kitchen/ResumePendingSubscription";
        if (User.Identity?.IsAuthenticated == true && User.IsInRole(NO23.Web.Data.Seed.ApplicationRoles.Member))
            return LocalRedirect(resumeUrl);
        return RedirectToAction(nameof(ContinueSubscription));
    }

    [HttpGet]
    public async Task<IActionResult> ContinueSubscription()
    {
        var raw = HttpContext.Session.GetString(PendingKitchenSubscription.SessionKey);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return LocalRedirect($"{Url.Action(nameof(Index))}#plans");
        }

        PendingKitchenSubscriptionData? pending;
        try
        {
            pending = JsonSerializer.Deserialize<PendingKitchenSubscriptionData>(raw);
        }
        catch (JsonException)
        {
            pending = null;
        }

        if (pending is null)
        {
            HttpContext.Session.Remove(PendingKitchenSubscription.SessionKey);
            return LocalRedirect($"{Url.Action(nameof(Index))}#plans");
        }

        var resumeUrl = Url.Action(
            "ResumePendingSubscription",
            "Kitchen",
            new { area = "Member" }) ?? "/Member/Kitchen/ResumePendingSubscription";

        if (User.Identity?.IsAuthenticated == true &&
            User.IsInRole(NO23.Web.Data.Seed.ApplicationRoles.Member))
        {
            return LocalRedirect(resumeUrl);
        }

        var package = await dbContext.KitchenSubscriptionPackages
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Plan == pending.Plan && item.IsActive);

        if (package is null ||
            !KitchenSubscriptionPricing.TryCalculate(
                package,
                pending.SelectedMeals,
                out var quote,
                out _))
        {
            HttpContext.Session.Remove(PendingKitchenSubscription.SessionKey);
            TempData["ErrorMessage"] = "Seçtiğin Kitchen paketi artık kullanılamıyor.";
            return LocalRedirect($"{Url.Action(nameof(Index))}#plans");
        }

        var deliveryPrice = pending.DeliveryMethod == OrderDeliveryMethod.AddressDelivery
            ? package.DailyDeliveryFee * package.Days
            : 0m;

        return View(new KitchenSubscriptionAuthChoiceViewModel
        {
            PackageName = package.Name,
            PackageDays = package.Days,
            SelectedMeals = string.Join(", ", pending.SelectedMeals.Select(KitchenMealSelection.Name)),
            DailyCalories = pending.Recommendation?.DailyCalories ?? 0,
            PackagePrice = quote!.PackagePrice,
            DeliveryPrice = deliveryPrice,
            TotalPrice = quote.PackagePrice + deliveryPrice,
            DeliveryMethod = pending.DeliveryMethod,
            ResumeUrl = resumeUrl
        });
    }

    private T? ReadSession<T>(string key)
    {
        var value = HttpContext.Session.GetString(key);

        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(value);
        }
        catch (JsonException)
        {
            HttpContext.Session.Remove(key);
            return default;
        }
    }

    public async Task<IActionResult> Order(int menuItemId)
    {
        var model = await BuildKitchenOrderPageAsync(menuItemId);

        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(
        int menuItemId,
        [Bind(Prefix = "input")] GuestOrderInputViewModel input)
    {
    var model = await BuildKitchenOrderPageAsync(menuItemId, input);

    if (model is null)
    {
        return NotFound();
    }

    if (!paymentSettings.Enabled)
    {
        ModelState.AddModelError(
            string.Empty,
            "Online ödeme şu anda kullanılamıyor. Lütfen daha sonra tekrar dene.");
    }

    if (!ModelState.IsValid)
    {
        return View("Order", model);
    }

    var result = await commerceService.CreateGuestKitchenOrderAsync(
        menuItemId,
        input.Quantity,
        input);

    if (!result.Succeeded || result.EntityId is null)
    {
        ModelState.AddModelError(
            string.Empty,
            result.ErrorMessage ?? "Sipariş oluşturulamadı.");

        return View("Order", model);
    }

    var orderNumber = await dbContext.Orders
        .AsNoTracking()
        .Where(order => order.Id == result.EntityId)
        .Select(order => order.OrderNumber)
        .FirstAsync();

    var returnUrl = Url.Action(
        nameof(KitchenController.Confirmation),
        "Kitchen",
        new
        {
            orderNumber
        },
        "http",
        "213.254.136.245:5044");

    var paymentResult =
        await iyzicoPaymentService.InitializeAsync(
            result.EntityId.Value,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            returnUrl);

    if (!paymentResult.Succeeded ||
        string.IsNullOrWhiteSpace(paymentResult.RedirectUrl))
    {
        ModelState.AddModelError(
            string.Empty,
            paymentResult.ErrorMessage
            ?? "Ödeme başlatılamadı. Lütfen tekrar dene.");

        return View("Order", model);
    }

    return Redirect(paymentResult.RedirectUrl);
    }

    public async Task<IActionResult> Confirmation(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            return NotFound();
        }

        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(item => item.Items)
            .FirstOrDefaultAsync(order =>
                order.OrderNumber == orderNumber &&
                order.GuestEmail != null);

        if (order is null)
        {
            return NotFound();
        }

        var orderItem = order.Items
            .OrderBy(item => item.Id)
            .FirstOrDefault();

        if (orderItem is null)
        {
            return NotFound();
        }

        if (order.DeliveryDate is null ||string.IsNullOrWhiteSpace(order.DeliveryTimeSlot))
        {
            return NotFound();
        }

        return View(new GuestOrderConfirmationViewModel
        {
            OrderNumber = order.OrderNumber,
            ProductName = orderItem.ProductName,
            Quantity = orderItem.Quantity,
            RemovedIngredientNames = orderItem.RemovedIngredientNames,
            AddedIngredientNames = orderItem.AddedIngredientNames,
            Total = order.Total,
            DeliveryDate = order.DeliveryDate.Value,
            DeliveryTimeSlot = order.DeliveryTimeSlot,
            DeliveryMethodText = GetDeliveryMethodLabel(order.DeliveryMethod),
            PaymentStatus = order.PaymentStatus,
            PaymentStatusText = GetPaymentStatusLabel(order.PaymentStatus)
        });
    }

    private async Task<GuestOrderPageViewModel?> BuildKitchenOrderPageAsync(
        int menuItemId,
        GuestOrderInputViewModel? input = null)
    {
        var menuItem = await dbContext.KitchenMenuItems
            .AsNoTracking()
            .Where(item => item.Id == menuItemId && item.IsActive)
            .Select(item => new
            {
                ItemId = item.Id,
                ItemName = item.Name,
                Description = item.Description,
                item.Category,
                UnitPrice = item.UnitPrice,
                Calories = item.Calories,
                ProteinGrams = item.ProteinGrams,
                CarbohydrateGrams = item.CarbohydrateGrams,
                FatGrams = item.FatGrams,
                Ingredients = item.Ingredients,
                RemovableIngredients = item.RecipeIngredients
                    .OrderBy(recipe => recipe.KitchenIngredient.Name)
                    .Select(recipe => new KitchenCustomizationOptionViewModel
                    {
                        Id = recipe.KitchenIngredientId,
                        Name = recipe.KitchenIngredient.Name
                    })
                    .ToList(),
                AllergenNames = item.MenuItemAllergens.OrderBy(x => x.KitchenAllergen.DisplayOrder)
                    .Select(x => x.KitchenAllergen.Name).ToList()
            })
            .FirstOrDefaultAsync();

        if (menuItem is null)
        {
            return null;
        }

        var recipeIngredientIds = menuItem.RemovableIngredients
            .Select(item => item.Id)
            .ToList();
        var additionalIngredients = await dbContext.KitchenIngredients
            .AsNoTracking()
            .Where(item => item.IsActive && !recipeIngredientIds.Contains(item.Id))
            .OrderBy(item => item.Name)
            .Select(item => new KitchenCustomizationOptionViewModel
            {
                Id = item.Id,
                Name = item.Name
            })
            .ToListAsync();

        return new GuestOrderPageViewModel
            {
                ItemId = menuItem.ItemId,
                ItemName = menuItem.ItemName,
                Description = menuItem.Description,
                Category = GetMenuCategoryLabel(menuItem.Category),
                UnitPrice = menuItem.UnitPrice,
                Calories = menuItem.Calories,
                ProteinGrams = menuItem.ProteinGrams,
                CarbohydrateGrams = menuItem.CarbohydrateGrams,
                FatGrams = menuItem.FatGrams,
                Ingredients = menuItem.Ingredients,
                Allergens = string.Join(", ", menuItem.AllergenNames),
                RemovableIngredients = menuItem.RemovableIngredients,
                AdditionalIngredients = additionalIngredients,
                IsPaymentAvailable = paymentSettings.Enabled,
                ClubPickupDisplayName = clubPickupSettings.EffectiveDisplayName,
                Input = input ?? new GuestOrderInputViewModel()
            };
    }

    private static string GetMenuCategoryLabel(MenuItemCategory category)
    {
        return category switch
        {
            MenuItemCategory.Breakfast => "Kahvaltı",
            MenuItemCategory.MainMeal => "Ana Öğün",
            MenuItemCategory.Snack => "Ara Öğün",
            MenuItemCategory.Dessert => "Tatlı",
            MenuItemCategory.Beverage => "İçecek",
            _ => category.ToString()
        };
    }

    private static string GetPaymentStatusLabel(PaymentStatus status)
    {
         return status switch
        {
        PaymentStatus.Pending => "Ödeme Bekliyor",
        PaymentStatus.Paid => "Ödendi",
        PaymentStatus.Failed => "Ödeme Başarısız",
        PaymentStatus.Refunded => "İade Edildi",
        PaymentStatus.Expired => "Ödeme Süresi Doldu",
        _ => status.ToString()
        };
    }

    private static string GetDeliveryMethodLabel(OrderDeliveryMethod method) =>
        method == OrderDeliveryMethod.ClubPickup
            ? "Salondan teslim"
            : "Adrese teslim";
}
