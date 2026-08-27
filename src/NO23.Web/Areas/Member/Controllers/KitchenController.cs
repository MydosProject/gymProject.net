using System.Security.Claims;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Member;
using NO23.Web.ViewModels;
using NO23.Web.Services.Payments;
using Microsoft.Extensions.Options;

namespace NO23.Web.Areas.Member.Controllers;

[Area("Member")]
[Authorize(Roles = ApplicationRoles.Member)]
public class KitchenController(
    ApplicationDbContext dbContext,
    CalorieCalculatorService calorieCalculator,
    CommerceService commerceService,
    IyzicoPaymentService iyzicoPaymentService,
    IOptions<IyzicoOptions> paymentOptions,
    IOptions<ClubPickupOptions> clubPickupOptions) : Controller
{
    private readonly IyzicoOptions paymentSettings = paymentOptions.Value;
    private readonly ClubPickupOptions clubPickupSettings = clubPickupOptions.Value;

    private const string CalculatorInputSessionKey = "NO23.Kitchen.CalculatorInput";
    private const string CalculatorResultSessionKey = "NO23.Kitchen.CalculatorResult";
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly StringComparer TurkishIgnoreCaseComparer =
        StringComparer.Create(TurkishCulture, ignoreCase: true);
    private static readonly string[] PreferredTagOrder =
    [
        "yüksek protein",
        "düşük kalori",
        "glutensiz",
        "vejetaryen"
    ];

    public async Task<IActionResult> Index()
    {
        return View(await BuildDashboardAsync(
            GetStoredCalculatorInput() ?? new CalorieCalculatorInputViewModel(),
            GetStoredCalculatorResult()));
    }

    [HttpGet]
    public IActionResult Calculator()
    {
        return LocalRedirect($"{Url.Action(nameof(Index))}#calculator");
    }

    [HttpGet]
    public async Task<IActionResult> Menu()
    {
        return View(await BuildMenuDashboardAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculate(
        [Bind(Prefix = "CalculatorInput")] CalorieCalculatorInputViewModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(
                "Index",
                await BuildDashboardAsync(input, null));
        }

        var calculationRequest = new CalorieCalculationRequest
        {
            HeightCm = input.HeightCm,
            WeightKg = input.WeightKg,
            Age = input.Age,
            Gender = input.Gender,
            ActivityLevel = input.ActivityLevel,
            Goal = input.Goal
        };

        var result = calorieCalculator.Calculate(calculationRequest);

        var recommendation = new CalorieRecommendationViewModel
        {
            Goal = input.Goal,
            DailyCalories = result.DailyCalories,
            ProteinGrams = result.ProteinGrams,
            CarbohydrateGrams = result.CarbohydrateGrams,
            FatGrams = result.FatGrams
        };

        HttpContext.Session.SetString(
            CalculatorInputSessionKey,
            JsonSerializer.Serialize(input));
        HttpContext.Session.SetString(
            CalculatorResultSessionKey,
            JsonSerializer.Serialize(recommendation));

        return LocalRedirect($"{Url.Action(nameof(Index))}#calculator");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscribe(
        KitchenSubscriptionPlan plan,
        CalorieCalculatorInputViewModel input)
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] =
                "Paket satın almak için önce geçerli kalori bilgilerini girmelisin.";

            return View(
                "Index",
                await BuildDashboardAsync(input, null));
        }

        var profile = await dbContext.MemberProfiles
            .FirstOrDefaultAsync(member =>
                member.ApplicationUserId == userId);

        if (profile is null)
        {
            TempData["ErrorMessage"] =
                "Üye profili bulunamadı.";

            return RedirectToAction(nameof(Index));
        }

        var calculationRequest =
            new CalorieCalculationRequest
            {
                HeightCm = input.HeightCm,
                WeightKg = input.WeightKg,
                Age = input.Age,
                Gender = input.Gender,
                ActivityLevel = input.ActivityLevel,
                Goal = input.Goal
            };

        var result =
            calorieCalculator.Calculate(
                calculationRequest);

        var subscriptionPackage =
            await dbContext
                .KitchenSubscriptionPackages
                .AsNoTracking()
                .FirstOrDefaultAsync(package =>
                    package.Plan == plan &&
                    package.IsActive);

        if (subscriptionPackage is null)
        {
            TempData["ErrorMessage"] =
                "Seçilen Kitchen paketi şu anda aktif değil.";

            return RedirectToAction(nameof(Index));
        }

        if (subscriptionPackage.Days <= 0)
        {
            TempData["ErrorMessage"] =
                "Seçilen Kitchen paketinin gün sayısı geçerli değil.";

            return RedirectToAction(nameof(Index));
        }

        var today =
            DateOnly.FromDateTime(
                DateTime.Today);

        var hasActiveSubscription =
            await dbContext.KitchenSubscriptions
                .AnyAsync(subscription =>
                    subscription.MemberProfileId ==
                        profile.Id &&
                    subscription.Status ==
                        KitchenSubscriptionStatus.Active &&
                    subscription.EndsOn >= today);

        if (hasActiveSubscription)
        {
            TempData["ErrorMessage"] =
                "Aktif Kitchen paketin devam ediyor. " +
                "Yeni paket almadan önce mevcut paketini tamamlamalısın.";

            return RedirectToAction(nameof(Index));
        }

       
        var startsOn =
            DateOnly.FromDateTime(
                DateTime.Today.AddDays(1));

        var subscription =
            new KitchenSubscription
            {
                MemberProfileId =
                    profile.Id,

                KitchenSubscriptionPackageId =
                    subscriptionPackage.Id,

                Plan =
                    subscriptionPackage.Plan,

                Status =
                    KitchenSubscriptionStatus.PendingPayment,

                PackageNameSnapshot =
                    subscriptionPackage.Name,

                PackagePriceSnapshot =
                    subscriptionPackage.UnitPrice,

                PackageDaysSnapshot =
                    subscriptionPackage.Days,

                Goal =
                    input.Goal,

                SourceHeightCm =
                    input.HeightCm,

                SourceWeightKg =
                    input.WeightKg,

                SourceAge =
                    input.Age,

                SourceGender =
                    input.Gender,

                SourceActivityLevel =
                    input.ActivityLevel,

                DailyCalories =
                    result.DailyCalories,

                ProteinGrams =
                    result.ProteinGrams,

                CarbohydrateGrams =
                    result.CarbohydrateGrams,

                FatGrams =
                    result.FatGrams,

                StartsOn =
                    startsOn,

                EndsOn =
                    startsOn.AddDays(
                        subscriptionPackage.Days - 1)
            };

        dbContext.KitchenSubscriptions.Add(
            subscription);

        await dbContext.SaveChangesAsync();

        return RedirectToAction(
            nameof(Checkout),
            new
            {
                subscriptionId =
                    subscription.Id
            });
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(
        int subscriptionId)
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var subscription =
            await dbContext.KitchenSubscriptions
                .AsNoTracking()
                .Include(item =>
                    item.MemberProfile)
                .ThenInclude(profile =>
                    profile.ApplicationUser)
                .FirstOrDefaultAsync(item =>
                    item.Id == subscriptionId &&
                    item.MemberProfile.ApplicationUserId ==
                        userId);

        if (subscription is null)
        {
            return NotFound();
        }

        if (subscription.Status !=
            KitchenSubscriptionStatus.PendingPayment)
        {
            TempData["ErrorMessage"] =
                "Bu Kitchen paketi ödeme beklemiyor.";

            return RedirectToAction(nameof(Index));
        }

        var applicationUser =
            subscription
                .MemberProfile
                .ApplicationUser;

        var fullName =
            $"{applicationUser.FirstName} " +
            $"{applicationUser.LastName}";

        return View(
            new KitchenCheckoutViewModel
            {
                KitchenSubscriptionId =
                    subscription.Id,

                PackageName =
                    subscription.PackageNameSnapshot,

                PackageDays =
                    subscription.PackageDaysSnapshot,

                PackagePrice =
                    subscription.PackagePriceSnapshot,

                IsPaymentAvailable =
                    paymentSettings.Enabled,

                ClubPickupDisplayName =
                    clubPickupSettings.EffectiveDisplayName,

                FullName =
                    fullName.Trim(),

                PhoneNumber =
                    applicationUser.PhoneNumber
                    ?? string.Empty
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(
        KitchenCheckoutViewModel model)
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var subscription =
            await dbContext.KitchenSubscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Id ==
                        model.KitchenSubscriptionId &&
                    item.MemberProfile
                        .ApplicationUserId ==
                        userId);

        if (subscription is null)
        {
            return NotFound();
        }

        model.PackageName =
            subscription.PackageNameSnapshot;

        model.PackageDays =
            subscription.PackageDaysSnapshot;

        model.PackagePrice =
            subscription.PackagePriceSnapshot;

        model.IsPaymentAvailable =
            paymentSettings.Enabled;

        model.ClubPickupDisplayName =
            clubPickupSettings.EffectiveDisplayName;

        if (!paymentSettings.Enabled)
        {
            ModelState.AddModelError(
                string.Empty,
                "Online ödeme şu anda kullanılamıyor. Lütfen daha sonra tekrar dene.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result =
            await commerceService
                .CreateKitchenPackageOrderAsync(
                    userId,
                    subscription.Id,
                    new DeliveryDetails
                    {
                        DeliveryMethod =
                            model.DeliveryMethod,

                        FullName =
                            model.FullName,

                        PhoneNumber =
                            model.PhoneNumber,

                        AddressLine =
                            model.AddressLine,

                        District =
                            model.District,

                        City =
                            model.City,

                        PostalCode =
                            model.PostalCode,

                        DeliveryDate =
                            null,

                        DeliveryTimeSlot =
                            null
                    });

        if (!result.Succeeded ||
            result.EntityId is null)
        {
            ModelState.AddModelError(
                string.Empty,
                result.ErrorMessage
                ?? "Kitchen siparişi oluşturulamadı.");

            return View(model);
        }

        var returnUrl = Url.Action(
            "Index",
            "Orders",
            new
            {
                area = "Member"
            },
            Request.Scheme,
            Request.Host.Value);

        var paymentResult =
            await iyzicoPaymentService.InitializeAsync(
                result.EntityId.Value,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                returnUrl);

        if (!paymentResult.Succeeded ||
            string.IsNullOrWhiteSpace(
                paymentResult.RedirectUrl))
        {
            ModelState.AddModelError(
                string.Empty,
                paymentResult.ErrorMessage
                ?? "Ödeme başlatılamadı. Lütfen tekrar dene.");

            return View(model);
        }

        return Redirect(
            paymentResult.RedirectUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SkipMeal(int mealPlanItemId)
    {
        return await SetMealSkippedAsync(
            mealPlanItemId,
            isSkipped: true,
            successMessage: "Öğün pas geçildi. Plan yenilendiğinde bu öğün üretim ihtiyacına dahil edilmez.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreMeal(int mealPlanItemId)
    {
        return await SetMealSkippedAsync(
            mealPlanItemId,
            isSkipped: false,
            successMessage: "Öğün yeniden plana alındı. Üretim planını güncellemek için admin tarafında Planı Yenile kullanılmalı.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SkipDay(int mealPlanDayId)
    {
        return await SetDaySkippedAsync(
            mealPlanDayId,
            isSkipped: true,
            successMessage: "Gün pas geçildi. Bu güne ait aktif öğünler üretim ihtiyacına dahil edilmez.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreDay(int mealPlanDayId)
    {
        return await SetDaySkippedAsync(
            mealPlanDayId,
            isSkipped: false,
            successMessage: "Gün yeniden plana alındı. Üretim planını güncellemek için admin tarafında Planı Yenile kullanılmalı.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetGymPickup(int mealPlanDayId)
    {
        return await SetDeliveryMethodAsync(
            mealPlanDayId,
            KitchenDeliveryMethod.GymPickup);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetHomeDelivery(int mealPlanDayId)
    {
        return await SetDeliveryMethodAsync(
            mealPlanDayId,
            KitchenDeliveryMethod.HomeDelivery);
    }

    private async Task<IActionResult> SetMealSkippedAsync(
        int mealPlanItemId,
        bool isSkipped,
        string successMessage)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var meal = await dbContext.KitchenMealPlanItems
            .Include(item => item.KitchenMealPlanDay)
                .ThenInclude(day => day.Items)
            .Include(item => item.KitchenMealPlanDay)
                .ThenInclude(day => day.KitchenMealPlan)
                .ThenInclude(plan => plan.KitchenSubscription)
                .ThenInclude(subscription => subscription.MemberProfile)
            .FirstOrDefaultAsync(item => item.Id == mealPlanItemId);

        if (meal is null ||
            meal.KitchenMealPlanDay.KitchenMealPlan.KitchenSubscription.MemberProfile
                .ApplicationUserId != userId)
        {
            return NotFound();
        }

        var result = ValidateMealPlanChange(meal.KitchenMealPlanDay);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index), null, "nutrition-plan");
        }

        if (meal.IsSkipped != isSkipped)
        {
            var changedAtUtc = DateTime.UtcNow;

            meal.IsSkipped = isSkipped;
            meal.SkippedAtUtc =
                isSkipped
                    ? changedAtUtc
                    : null;

            if (isSkipped &&
                meal.KitchenMealPlanDay.Items.All(
                    item => item.IsSkipped))
            {
                ClearDeliveryPreference(
                    meal.KitchenMealPlanDay,
                    changedAtUtc);
            }

            await dbContext.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = successMessage;

        return RedirectToAction(nameof(Index), null, "nutrition-plan");
    }

    private async Task<IActionResult> SetDaySkippedAsync(
        int mealPlanDayId,
        bool isSkipped,
        string successMessage)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var day = await dbContext.KitchenMealPlanDays
            .Include(item => item.Items)
            .Include(item => item.KitchenMealPlan)
            .ThenInclude(plan => plan.KitchenSubscription)
            .ThenInclude(subscription => subscription.MemberProfile)
            .FirstOrDefaultAsync(item => item.Id == mealPlanDayId);

        if (day is null ||
            day.KitchenMealPlan.KitchenSubscription.MemberProfile.ApplicationUserId != userId)
        {
            return NotFound();
        }

        var result = ValidateMealPlanChange(day);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index), null, "nutrition-plan");
        }

        var changedAtUtc = DateTime.UtcNow;

        foreach (var meal in day.Items)
        {
            if (meal.IsSkipped == isSkipped)
            {
        continue;
            }

        meal.IsSkipped = isSkipped;
        meal.SkippedAtUtc =
            isSkipped
                ? changedAtUtc
                : null;
        }

        if (isSkipped)
        {
            ClearDeliveryPreference(
                day,
                changedAtUtc);
        }

        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = successMessage;

        return RedirectToAction(nameof(Index), null, "nutrition-plan");
    }

    private async Task<IActionResult> SetDeliveryMethodAsync(
    int mealPlanDayId,
    KitchenDeliveryMethod deliveryMethod)
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var day =
            await dbContext.KitchenMealPlanDays
                .Include(item => item.Items)
                .Include(item => item.KitchenMealPlan)
                    .ThenInclude(plan => plan.KitchenSubscription)
                        .ThenInclude(subscription => subscription.MemberProfile)
                .FirstOrDefaultAsync(
                    item => item.Id == mealPlanDayId);

        if (day is null ||
            day.KitchenMealPlan
                .KitchenSubscription
                .MemberProfile
                .ApplicationUserId != userId)
        {
            return NotFound();
        }

        var validationResult =
            ValidateMealPlanChange(day);

        if (!validationResult.Succeeded)
        {
            TempData["ErrorMessage"] =
                validationResult.Message;

            return RedirectToAction(
                nameof(Index),
                null,
                "nutrition-plan");
        }

        var hasActiveMeal =
            day.Items.Any(item => !item.IsSkipped);

        if (!hasActiveMeal)
        {
            TempData["ErrorMessage"] =
                "Bu günün tüm öğünleri pas geçildiği için teslimat tercihi yapılamaz.";

            return RedirectToAction(
                nameof(Index),
                null,
                "nutrition-plan");
        }

        if (deliveryMethod ==
            KitchenDeliveryMethod.GymPickup)
        {
            day.DeliveryMethod =
                KitchenDeliveryMethod.GymPickup;

            day.DeliveryFullName = null;
            day.DeliveryPhoneNumber = null;
            day.DeliveryAddressLine = null;
            day.DeliveryDistrict = null;
            day.DeliveryCity = null;
            day.DeliveryPostalCode = null;

            day.DeliveryPreferenceUpdatedAtUtc =
                DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"{day.PlanDate:dd.MM.yyyy} tarihli öğünlerini NO23 Sports Club'dan teslim alacaksın.";

            return RedirectToAction(
                nameof(Index),
                null,
                "nutrition-plan");
        }

        var subscriptionId =
            day.KitchenMealPlan.KitchenSubscription.Id;

        var paidKitchenOrder =
            await dbContext.Orders
                .AsNoTracking()
                .Where(order =>
                    order.KitchenSubscriptionId ==
                        subscriptionId &&
                    order.Type ==
                        OrderType.KitchenSubscription &&
                    order.PaymentStatus ==
                        PaymentStatus.Paid)
                .OrderByDescending(order =>
                    order.CreatedAtUtc)
                .FirstOrDefaultAsync();

        if (paidKitchenOrder is null)
        {
            TempData["ErrorMessage"] =
                "Kitchen paketine ait ödeme ve adres bilgileri bulunamadı.";

            return RedirectToAction(
                nameof(Index),
                null,
                "nutrition-plan");
        }

        if (string.IsNullOrWhiteSpace(
                paidKitchenOrder.DeliveryAddressLine) ||
            string.IsNullOrWhiteSpace(
                paidKitchenOrder.DeliveryCity) ||
            string.IsNullOrWhiteSpace(
                paidKitchenOrder.DeliveryDistrict))
        {
            TempData["ErrorMessage"] =
                "Eve teslim için kayıtlı adres bilgileri eksik.";

            return RedirectToAction(
                nameof(Index),
                null,
                "nutrition-plan");
        }

        day.DeliveryMethod =
            KitchenDeliveryMethod.HomeDelivery;

        day.DeliveryFullName =
            paidKitchenOrder.DeliveryFullName;

        day.DeliveryPhoneNumber =
            paidKitchenOrder.DeliveryPhoneNumber;

        day.DeliveryAddressLine =
            paidKitchenOrder.DeliveryAddressLine;

        day.DeliveryDistrict =
            paidKitchenOrder.DeliveryDistrict;

        day.DeliveryCity =
            paidKitchenOrder.DeliveryCity;

        day.DeliveryPostalCode =
            paidKitchenOrder.DeliveryPostalCode;

        day.DeliveryPreferenceUpdatedAtUtc =
            DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"{day.PlanDate:dd.MM.yyyy} tarihli öğünlerin kayıtlı adresine teslim edilecek.";

        return RedirectToAction(
            nameof(Index),
            null,
            "nutrition-plan");
    }

    private static void ClearDeliveryPreference(
    KitchenMealPlanDay day,
    DateTime changedAtUtc)
    {
        day.DeliveryMethod =
            KitchenDeliveryMethod.NotSelected;

        day.DeliveryFullName = null;
        day.DeliveryPhoneNumber = null;
        day.DeliveryAddressLine = null;
        day.DeliveryDistrict = null;
        day.DeliveryCity = null;
        day.DeliveryPostalCode = null;

        day.DeliveryPreferenceUpdatedAtUtc =
            changedAtUtc;
    }

    private static KitchenMealPlanChangeResult ValidateMealPlanChange(KitchenMealPlanDay day)
    {
        var subscription = day.KitchenMealPlan.KitchenSubscription;
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (subscription.Status != KitchenSubscriptionStatus.Active)
        {
            return KitchenMealPlanChangeResult.Fail(
                "Sadece aktif Kitchen aboneliğindeki öğünler değiştirilebilir.");
        }

        if (day.PlanDate <= today)
        {
            return KitchenMealPlanChangeResult.Fail(
                "Bugün veya geçmiş tarihli öğünler pas geçilemez. Sadece yarın ve sonrası için değişiklik yapabilirsin.");
        }

        return KitchenMealPlanChangeResult.Ok();
    }

    private async Task<KitchenDashboardViewModel> BuildDashboardAsync(
        CalorieCalculatorInputViewModel input,
        CalorieRecommendationViewModel? recommendation)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var today = DateOnly.FromDateTime(DateTime.Today);

        ActiveKitchenSubscriptionViewModel? activeSubscription = null;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var subscription = await dbContext.KitchenSubscriptions
                .AsNoTracking()
                .Where(item =>
                    item.MemberProfile.ApplicationUserId == userId &&
                    item.Status == KitchenSubscriptionStatus.Active &&
                    item.EndsOn >= today)
                .OrderByDescending(item => item.CreatedAtUtc)
                .ThenByDescending(item => item.Id)
                .Select(item => new
                {
                    item.Id,
                    item.Plan,
                    item.Status,
                    item.Goal,
                    item.PackageNameSnapshot,
                    item.PackagePriceSnapshot,
                    item.PackageDaysSnapshot,
                    item.DailyCalories,
                    item.ProteinGrams,
                    item.CarbohydrateGrams,
                    item.FatGrams,
                    item.StartsOn,
                    item.EndsOn
                })
                .FirstOrDefaultAsync();

            if (subscription is not null)
            {
                activeSubscription = new ActiveKitchenSubscriptionViewModel
                {
                    Id = subscription.Id,
                    Plan = subscription.Plan.ToString(),
                    Status = subscription.Status.ToString(),
                    Goal = subscription.Goal.ToString(),
                    PackageName = subscription.PackageNameSnapshot,
                    PackagePrice = subscription.PackagePriceSnapshot,
                    PackageDays = subscription.PackageDaysSnapshot,
                    DailyCalories = subscription.DailyCalories,
                    ProteinGrams = subscription.ProteinGrams,
                    CarbohydrateGrams = subscription.CarbohydrateGrams,
                    FatGrams = subscription.FatGrams,
                    StartsOn = subscription.StartsOn,
                    EndsOn = subscription.EndsOn,
                    RemainingDays = Math.Max(0, subscription.EndsOn.DayNumber - today.DayNumber + 1),
                    MealPlan = await BuildMealPlanAsync(subscription.Id)
                };
            }
        }

        return new KitchenDashboardViewModel
        {
            CalculatorInput = input,
            Recommendation = recommendation,
            ActiveSubscription = activeSubscription,
            SubscriptionPlans = await BuildSubscriptionPackagesAsync()
        };
    }

    private async Task<IReadOnlyList<KitchenSubscriptionPlanViewModel>> BuildSubscriptionPackagesAsync()
    {
        return await dbContext.KitchenSubscriptionPackages
            .AsNoTracking()
            .Where(package => package.IsActive)
            .OrderBy(package => package.DisplayOrder)
            .ThenBy(package => package.Name)
            .Select(package => new KitchenSubscriptionPlanViewModel
            {
                Plan = package.Plan,
                Name = package.Name,
                Description = package.Description,
                Days = package.Days,
                UnitPrice = package.UnitPrice,
                IsActive = package.IsActive
            })
            .ToListAsync();
    }

    private async Task<KitchenMealPlanViewModel?> BuildMealPlanAsync(int kitchenSubscriptionId)
    {
        var mealPlan = await dbContext.KitchenMealPlans
            .AsNoTracking()
            .Where(plan => plan.KitchenSubscriptionId == kitchenSubscriptionId)
            .OrderByDescending(plan => plan.GeneratedAtUtc)
            .ThenByDescending(plan => plan.Id)
            .Select(plan => new
            {
                plan.Id,
                plan.Status,
                plan.GeneratedAtUtc
            })
            .FirstOrDefaultAsync();

        if (mealPlan is null)
        {
            return null;
        }

        var days = await dbContext.KitchenMealPlanDays
            .AsNoTracking()
            .Where(day => day.KitchenMealPlanId == mealPlan.Id)
            .OrderBy(day => day.DayNumber)
            .Select(day => new
        {
            day.Id,
            day.DayNumber,
            day.PlanDate,

            day.DeliveryMethod,
            day.DeliveryFullName,
            day.DeliveryPhoneNumber,
            day.DeliveryAddressLine,
            day.DeliveryDistrict,
            day.DeliveryCity,
            day.DeliveryPostalCode,

            day.TotalCalories,
            day.TotalProteinGrams,
            day.TotalCarbohydrateGrams,
            day.TotalFatGrams
        })
            .ToListAsync();

        var dayIds = days
            .Select(day => day.Id)
            .ToList();

        var items = await dbContext.KitchenMealPlanItems
            .AsNoTracking()
            .Where(item => dayIds.Contains(item.KitchenMealPlanDayId))
            .OrderBy(item => item.KitchenMealPlanDayId)
            .ThenBy(item => item.MealSlot)
            .ThenBy(item => item.Id)
            .Select(item => new
            {
                item.Id,
                item.KitchenMealPlanDayId,
                item.KitchenMenuItemId,
                item.MealSlot,
                item.Quantity,
                item.ProductNameSnapshot,
                item.CaloriesSnapshot,
                item.ProteinGramsSnapshot,
                item.CarbohydrateGramsSnapshot,
                item.FatGramsSnapshot,
                item.UnitPriceSnapshot,
                item.IsSkipped
            })
            .ToListAsync();

        var itemsByDayId = items
            .GroupBy(item => item.KitchenMealPlanDayId)
            .ToDictionary(group => group.Key, group => group.ToList());

        return new KitchenMealPlanViewModel
        {
            Id = mealPlan.Id,
            Status = mealPlan.Status.ToString(),
            GeneratedAtUtc = mealPlan.GeneratedAtUtc,
            Days = days
                .Select(day =>
                {
                    var dayMeals = itemsByDayId.TryGetValue(day.Id, out var dayItems)
                        ? dayItems
                        : [];

                    var meals = dayMeals
                        .Select(item => new KitchenMealPlanMealViewModel
                        {
                            Id = item.Id,
                            KitchenMenuItemId = item.KitchenMenuItemId,
                            MealSlot = item.MealSlot.ToString(),
                            MealSlotDisplayName = GetMealSlotDisplayName(item.MealSlot),
                            Quantity = item.Quantity,
                            ProductName = item.ProductNameSnapshot,
                            Calories = item.CaloriesSnapshot * item.Quantity,
                            ProteinGrams = item.ProteinGramsSnapshot * item.Quantity,
                            CarbohydrateGrams = item.CarbohydrateGramsSnapshot * item.Quantity,
                            FatGrams = item.FatGramsSnapshot * item.Quantity,
                            UnitPrice = item.UnitPriceSnapshot,
                            TotalPrice = item.UnitPriceSnapshot * item.Quantity,
                            IsSkipped = item.IsSkipped,
                            CanSkip = day.PlanDate > DateOnly.FromDateTime(DateTime.Today)
                        })
                        .ToList();
                    var activeMeals = meals
                        .Where(meal => !meal.IsSkipped)
                        .ToList();

                    return new KitchenMealPlanDayViewModel
                    {
                        Id = day.Id,
                        DayNumber = day.DayNumber,
                        PlanDate = day.PlanDate,
                        DeliveryMethod =
                            day.DeliveryMethod.ToString(),

                        DeliveryMethodDisplayName =
                            GetDeliveryMethodDisplayName(
                                day.DeliveryMethod),

                        DeliveryFullName =
                            day.DeliveryFullName,

                        DeliveryPhoneNumber =
                            day.DeliveryPhoneNumber,

                        DeliveryAddressLine =
                            day.DeliveryAddressLine,

                        DeliveryDistrict =
                            day.DeliveryDistrict,

                        DeliveryCity =
                            day.DeliveryCity,

                        DeliveryPostalCode =
                            day.DeliveryPostalCode,
                        TotalCalories = activeMeals.Sum(meal => meal.Calories),
                        TotalProteinGrams = activeMeals.Sum(meal => meal.ProteinGrams),
                        TotalCarbohydrateGrams = activeMeals.Sum(meal => meal.CarbohydrateGrams),
                        TotalFatGrams = activeMeals.Sum(meal => meal.FatGrams),
                        TotalPrice = activeMeals.Sum(meal => meal.TotalPrice),
                        CanSkip = day.PlanDate > DateOnly.FromDateTime(DateTime.Today),
                        Meals = meals
                    };
                })
                .ToList()
        };
    }

    private static string GetDeliveryMethodDisplayName(
    KitchenDeliveryMethod deliveryMethod)
    {
        return deliveryMethod switch
        {
            KitchenDeliveryMethod.GymPickup =>
                "NO23 Sports Club'dan teslim al",

            KitchenDeliveryMethod.HomeDelivery =>
                "Adresime teslim",

            _ =>
                "Henüz seçilmedi"
        };
    }

    private static string GetMealSlotDisplayName(KitchenMealSlot slot)
    {
        return slot switch
        {
            KitchenMealSlot.Breakfast => "Kahvalt\u0131",
            KitchenMealSlot.MorningSnack => "1. Ara \u00d6\u011f\u00fcn",
            KitchenMealSlot.Lunch => "\u00d6\u011fle Yeme\u011fi",
            KitchenMealSlot.AfternoonSnack => "2. Ara \u00d6\u011f\u00fcn",
            KitchenMealSlot.Dinner => "Ak\u015fam Yeme\u011fi",
            _ => slot.ToString()
        };
    }

    private async Task<KitchenDashboardViewModel> BuildMenuDashboardAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var memberAllergenIds = await dbContext.MemberAllergens.AsNoTracking()
            .Where(x => x.MemberProfile.ApplicationUserId == userId)
            .Select(x => x.KitchenAllergenId).ToListAsync();
        var memberAllergenNames = await dbContext.KitchenAllergens.AsNoTracking()
            .Where(x => memberAllergenIds.Contains(x.Id)).OrderBy(x => x.DisplayOrder)
            .Select(x => x.Name).ToListAsync();
        var activeIngredients = await dbContext.KitchenIngredients
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new KitchenCustomizationOptionViewModel
            {
                Id = item.Id,
                Name = item.Name
            })
            .ToListAsync();
        var rawItems = await dbContext.KitchenMenuItems
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .Select(item => new
            {
                item.Id,
                item.Name,
                Category = item.Category.ToString(),
                item.Calories,
                item.UnitPrice,
                item.ProteinGrams,
                item.CarbohydrateGrams,
                item.FatGrams,
                item.Ingredients,
                item.Tags,
                RecipeIngredients = item.RecipeIngredients
                    .OrderBy(recipe => recipe.KitchenIngredient.Name)
                    .Select(recipe => new KitchenCustomizationOptionViewModel
                    {
                        Id = recipe.KitchenIngredientId,
                        Name = recipe.KitchenIngredient.Name
                    })
                    .ToList(),
                Allergens = item.MenuItemAllergens.OrderBy(x => x.KitchenAllergen.DisplayOrder)
                    .Select(x => new { x.KitchenAllergenId, x.KitchenAllergen.Name }).ToList()
            })
            .ToListAsync();

        var menuItems = rawItems
            .Select(item =>
            {
                var tagList = SplitTags(item.Tags);

                return new KitchenMenuItemCardViewModel
                {
                    Id = item.Id,
                    Name = item.Name,
                    Category = item.Category,
                    Calories = item.Calories,
                    UnitPrice = item.UnitPrice,
                    ProteinGrams = item.ProteinGrams,
                    CarbohydrateGrams = item.CarbohydrateGrams,
                    FatGrams = item.FatGrams,
                    Ingredients = item.Ingredients,
                    AllergenIds = item.Allergens.Select(x => x.KitchenAllergenId).ToList(),
                    AllergenNames = item.Allergens.Select(x => x.Name).ToList(),
                    MatchingAllergenNames = item.Allergens
                        .Where(x => memberAllergenIds.Contains(x.KitchenAllergenId))
                        .Select(x => x.Name).ToList(),
                    RemovableIngredients = item.RecipeIngredients,
                    AdditionalIngredients = activeIngredients
                        .Where(ingredient => !item.RecipeIngredients
                            .Any(recipe => recipe.Id == ingredient.Id))
                        .ToList(),
                    Tags = string.Join(", ", tagList),
                    TagList = tagList
                };
            })
            .ToList();

        return new KitchenDashboardViewModel
        {
            MenuItems = menuItems,
            MemberAllergenNames = memberAllergenNames,
            ClubPickupDisplayName = clubPickupSettings.EffectiveDisplayName,
            CategoryFilters = BuildCategoryFilters(menuItems),
            TagFilters = BuildTagFilters(menuItems)
        };
    }

    private static IReadOnlyList<KitchenFilterOptionViewModel> BuildCategoryFilters(
        IReadOnlyList<KitchenMenuItemCardViewModel> menuItems)
    {
        return Enum.GetValues<MenuItemCategory>()
            .Select(category =>
            {
                var value = category.ToString();

                return new KitchenFilterOptionViewModel
                {
                    Value = value,
                    Label = GetCategoryPluralName(category),
                    ItemCount = menuItems.Count(item => item.Category == value)
                };
            })
            .Where(filter => filter.ItemCount > 0)
            .ToList();
    }

    private static IReadOnlyList<KitchenFilterOptionViewModel> BuildTagFilters(
        IReadOnlyList<KitchenMenuItemCardViewModel> menuItems)
    {
        var tagCounts = menuItems
            .SelectMany(item => item.TagList.Distinct(TurkishIgnoreCaseComparer))
            .GroupBy(tag => tag, TurkishIgnoreCaseComparer)
            .ToDictionary(
                group => group.Key,
                group => group.Count(),
                TurkishIgnoreCaseComparer);

        return tagCounts
            .Select(item => new KitchenFilterOptionViewModel
            {
                Value = NormalizeTag(item.Key),
                Label = ToTitleCase(item.Key),
                ItemCount = item.Value
            })
            .OrderBy(filter =>
            {
                var index = Array.IndexOf(PreferredTagOrder, filter.Value);
                return index < 0 ? PreferredTagOrder.Length : index;
            })
            .ThenBy(filter => filter.Label, TurkishIgnoreCaseComparer)
            .ToList();
    }

    private static IReadOnlyList<string> SplitTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return [];
        }

        return tags
            .Split(
                [',', ';', '|'],
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Select(NormalizeTag)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(TurkishIgnoreCaseComparer)
            .ToList();
    }

    private static string NormalizeTag(string value)
    {
        return value
            .Trim()
            .ToLower(TurkishCulture);
    }

    private static string ToTitleCase(string value)
    {
        return TurkishCulture.TextInfo.ToTitleCase(value);
    }

    private static string GetCategoryPluralName(MenuItemCategory category)
    {
        return category switch
        {
            MenuItemCategory.Breakfast => "Kahvaltılar",
            MenuItemCategory.MainMeal => "Ana Öğünler",
            MenuItemCategory.Snack => "Ara Öğünler",
            MenuItemCategory.Dessert => "Tatlılar",
            MenuItemCategory.Beverage => "İçecekler",
            _ => category.ToString()
        };
    }

    private CalorieCalculatorInputViewModel? GetStoredCalculatorInput()
    {
        return GetSessionValue<CalorieCalculatorInputViewModel>(
            CalculatorInputSessionKey);
    }

    private CalorieRecommendationViewModel? GetStoredCalculatorResult()
    {
        return GetSessionValue<CalorieRecommendationViewModel>(
            CalculatorResultSessionKey);
    }

    private T? GetSessionValue<T>(string key)
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
}

public record KitchenMealPlanChangeResult(
    bool Succeeded,
    string? Message)
{
    public static KitchenMealPlanChangeResult Ok()
    {
        return new KitchenMealPlanChangeResult(true, null);
    }

    public static KitchenMealPlanChangeResult Fail(string message)
    {
        return new KitchenMealPlanChangeResult(false, message);
    }
}
