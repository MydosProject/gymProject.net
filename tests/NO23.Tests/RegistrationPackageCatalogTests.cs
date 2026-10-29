using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using NO23.Web.Areas.Admin.Controllers;
using NO23.Web.Areas.Identity.Pages.Account;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Infrastructure.Identity;
using NO23.Web.Infrastructure.Validation;
using NO23.Web.Services;
using NO23.Web.Services.Payments;
using NO23.Web.ViewModels.Admin;
using NO23.Web.ViewModels.Member;

namespace NO23.Tests;

public class RegistrationPackageCatalogTests
{
    [Theory]
    [InlineData("kitchen", "/Member/Kitchen/ResumePendingSubscription")]
    [InlineData(null, "/Member/Kitchen/ResumePendingSubscription")]
    [InlineData(null, "/Kitchen/ContinueSubscription?packageId=3&meal=Lunch")]
    public async Task RegisterGet_KitchenCheckoutUsesAccountOnlyFormAndPreservesResumeUrl(string? package, string resume)
    {
        await using var provider = CreateServices();
        var catalog = await SeedCatalogAsync(provider);
        var page = CreatePage(provider);
        await page.OnGetAsync(package: package, returnUrl: resume, variant: catalog.Hybrid.Id);
        Assert.True(page.IsKitchenRegistration);
        Assert.Equal(RegistrationFlow.Kitchen, page.Input.Flow);
        Assert.Equal(string.Empty, page.Input.PackageCode);
        Assert.Null(page.Input.ServicePackageVariantId);
        Assert.Empty(page.PackageOptions);
        Assert.Empty(page.ServiceOptions);
        Assert.Equal(resume, page.ReturnUrl);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RegisterPost_KitchenCheckoutCreatesAccountWithoutSportsEvenWithInjectedVariant(bool injectVariant)
    {
        await using var provider = CreateServices();
        var catalog = await SeedCatalogAsync(provider);
        var page = CreatePage(provider);
        page.Input = ValidInput("kitchen", injectVariant ? catalog.Black.Id : null);
        page.Input.Flow = RegistrationFlow.Kitchen;
        page.Input.FitnessGoal = "Injected sports data";
        const string resume = "/Member/Kitchen/ResumePendingSubscription";
        Assert.Equal(resume, Assert.IsType<LocalRedirectResult>(await page.OnPostAsync(resume)).Url);
        Assert.Equal(resume, page.ReturnUrl);
        var db = provider.GetRequiredService<ApplicationDbContext>();
        var profile = await db.MemberProfiles.Include(x => x.MembershipPackage).SingleAsync();
        Assert.Equal(MembershipPackageCode.KitchenOnly, profile.MembershipPackage.Code);
        Assert.Null(profile.ServicePackageVariantId);
        Assert.Null(profile.MembershipPackageOptionId);
        Assert.Null(profile.MembershipStartsAtUtc);
        Assert.Null(profile.MembershipEndsAtUtc);
        Assert.Null(profile.FitnessGoal);
        Assert.Equal(0, profile.RemainingClassCredits);
        Assert.False(MemberPackageEntitlement.HasUnlimitedClassAccess(profile));
        Assert.False(MemberPackageEntitlement.IsActive(profile, DateTime.UtcNow));
        Assert.Empty(await db.Orders.ToListAsync());
        Assert.NotNull(provider.GetRequiredService<CapturingAuthenticationService>().SignedInUser);
        Assert.True(await provider.GetRequiredService<UserManager<ApplicationUser>>()
            .IsInRoleAsync(await db.Users.SingleAsync(), ApplicationRoles.Member));
        var admin = CreateAdminController(provider);
        var listed = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<MemberListItemViewModel>>(
            Assert.IsType<ViewResult>(await admin.Index()).Model));
        Assert.True(listed.IsKitchenOnly);
        Assert.False(listed.IsUnlimitedPackage);
    }

    [Theory]
    [InlineData("kitchen")]
    [InlineData("hybrid")]
    public async Task RegisterPost_RejectsRegistrationWhenNoSportsPackageIsPublished(string packageCode)
    {
        await using var provider = CreateServices();
        await SeedCatalogAsync(provider);
        var db = provider.GetRequiredService<ApplicationDbContext>();
        foreach (var package in await db.ServicePackages.ToListAsync()) package.IsActive = false;
        await db.SaveChangesAsync();
        var page = CreatePage(provider);
        page.Input = ValidInput(packageCode, null);
        Assert.IsType<PageResult>(await page.OnPostAsync());
        Assert.NotEmpty(page.ModelState["Input.PackageCode"]!.Errors);
        Assert.Empty(page.PackageOptions);
        Assert.Empty(await db.MemberProfiles.ToListAsync());
        Assert.Empty(await db.Users.ToListAsync());
    }

    [Fact]
    public async Task KitchenCheckout_DoesNotDependOnSportsCatalogAvailability()
    {
        await using var provider = CreateServices();
        await SeedCatalogAsync(provider);
        var db = provider.GetRequiredService<ApplicationDbContext>();
        foreach (var package in await db.ServicePackages.ToListAsync()) package.IsActive = false;
        await db.SaveChangesAsync();
        var page = CreatePage(provider);
        Assert.IsType<PageResult>(await page.OnGetAsync(flow: RegistrationFlow.Kitchen));
        Assert.Empty(page.PackageOptions);
        page.Input = ValidInput(string.Empty, null);
        page.Input.Flow = RegistrationFlow.Kitchen;
        Assert.Equal("/Member/Kitchen", Assert.IsType<LocalRedirectResult>(await page.OnPostAsync()).Url);
        Assert.Equal(0, (await db.MemberProfiles.SingleAsync()).RemainingClassCredits);
        Assert.Empty(await db.Orders.ToListAsync());
    }

    [Fact]
    public async Task RegisterGet_ExplicitSportsFlowKeepsPackageSelectionEvenWithKitchenReturnUrl()
    {
        await using var provider = CreateServices();
        var catalog = await SeedCatalogAsync(provider);
        var page = CreatePage(provider);
        await page.OnGetAsync(returnUrl: "/Member/Kitchen/ResumePendingSubscription", flow: RegistrationFlow.Sports);
        Assert.False(page.IsKitchenRegistration);
        Assert.Equal("hybrid", page.Input.PackageCode);
        Assert.Equal(catalog.Hybrid.Id, page.Input.ServicePackageVariantId);
    }

    [Theory]
    [InlineData("//example.test/Member/Kitchen/ResumePendingSubscription")]
    [InlineData("https://example.test/Member/Kitchen/ResumePendingSubscription")]
    public async Task KitchenRegistration_DoesNotRedirectToExternalUrls(string returnUrl)
    {
        await using var provider = CreateServices();
        await SeedCatalogAsync(provider);
        var page = CreatePage(provider);
        await page.OnGetAsync(returnUrl: returnUrl, flow: RegistrationFlow.Kitchen);
        Assert.Null(page.ReturnUrl);
        page.Input = ValidInput(string.Empty, null);
        page.Input.Flow = RegistrationFlow.Kitchen;
        Assert.Equal("/Member/Kitchen", Assert.IsType<LocalRedirectResult>(await page.OnPostAsync(returnUrl)).Url);
    }

    [Fact]
    public async Task Registration_InvalidFlowCannotCreateAnAccount()
    {
        await using var provider = CreateServices();
        var catalog = await SeedCatalogAsync(provider);
        var page = CreatePage(provider);
        Assert.IsType<BadRequestResult>(await page.OnGetAsync(flow: (RegistrationFlow)99));
        page.Input = ValidInput("hybrid", catalog.Hybrid.Id);
        page.Input.Flow = (RegistrationFlow)99;
        Assert.IsType<PageResult>(await page.OnPostAsync());
        Assert.NotEmpty(page.ModelState["Input.Flow"]!.Errors);
        Assert.Empty(await provider.GetRequiredService<ApplicationDbContext>().Users.ToListAsync());
    }

    [Fact]
    public void RegistrationValidation_RequiresSportsPackageAndDurationOnlyForSportsFlow()
    {
        var input = ValidInput(string.Empty, null);
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(input, new ValidationContext(input), errors, true));
        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(input.PackageCode)));
        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(input.ServicePackageVariantId)));
        input.Flow = RegistrationFlow.Kitchen;
        errors.Clear();
        Assert.True(Validator.TryValidateObject(input, new ValidationContext(input), errors, true));
    }

    [Fact]
    public async Task KitchenCheckout_RegistrationRetryPreservesMealsCaloriesAndDeliveryUntilCheckout()
    {
        await using var provider = CreateServices();
        await SeedCatalogAsync(provider);
        var db = provider.GetRequiredService<ApplicationDbContext>();
        var package = new KitchenSubscriptionPackage
        {
            Name = "5 Günlük Kitchen", Plan = KitchenSubscriptionPlan.FiveDays, Days = 5,
            UnitPrice = 3000, TwoMainMealsPrice = 5500, ThreeMainMealsPrice = 7500, DailyDeliveryFee = 95
        };
        db.KitchenSubscriptionPackages.Add(package);
        await db.SaveChangesAsync();
        var page = CreatePage(provider);
        page.HttpContext.Session = new MemorySession();
        var pending = new PendingKitchenSubscriptionData(package.Plan,
            [KitchenMealSlot.Breakfast, KitchenMealSlot.Lunch, KitchenMealSlot.AfternoonSnack],
            OrderDeliveryMethod.AddressDelivery, new CalorieCalculatorInputViewModel(),
            new CalorieRecommendationViewModel { DailyCalories = 2270, ProteinGrams = 98, CarbohydrateGrams = 298, FatGrams = 76 });
        var raw = JsonSerializer.Serialize(pending);
        page.HttpContext.Session.SetString(PendingKitchenSubscription.SessionKey, raw);
        const string resume = "/Member/Kitchen/ResumePendingSubscription";
        await page.OnGetAsync(flow: RegistrationFlow.Kitchen, returnUrl: resume);
        page.Input = ValidInput(string.Empty, null);
        page.Input.Flow = RegistrationFlow.Kitchen;
        page.Input.Password = page.Input.ConfirmPassword = "missinguppercase123!";
        Assert.IsType<PageResult>(await page.OnPostAsync());
        Assert.True(page.IsKitchenRegistration);
        Assert.Equal(raw, page.HttpContext.Session.GetString(PendingKitchenSubscription.SessionKey));
        Assert.Empty(await db.Users.ToListAsync());

        page.ModelState.Clear();
        page.Input.Password = page.Input.ConfirmPassword = "Kitchen123!";
        Assert.Equal(resume, Assert.IsType<LocalRedirectResult>(await page.OnPostAsync()).Url);
        Assert.Equal(raw, page.HttpContext.Session.GetString(PendingKitchenSubscription.SessionKey));
        page.HttpContext.User = provider.GetRequiredService<CapturingAuthenticationService>().SignedInUser!;
        var options = Options.Create(new IyzicoOptions());
        var payments = new IyzicoPaymentService(db, new NoPaymentClient(), new KitchenPlanMatchingService(db),
            options, NullLogger<IyzicoPaymentService>.Instance);
        var kitchen = new NO23.Web.Areas.Member.Controllers.KitchenController(db, new CalorieCalculatorService(),
            new CommerceService(db), payments, options, Options.Create(new ClubPickupOptions()), new ClubClock(TimeProvider.System))
        {
            ControllerContext = new ControllerContext { HttpContext = page.HttpContext },
            Url = page.Url,
            TempData = new TempDataDictionary(page.HttpContext, new MemoryTempDataProvider())
        };
        var redirect = Assert.IsType<RedirectToActionResult>(await kitchen.ResumePendingSubscription());
        Assert.Equal("Checkout", redirect.ActionName);
        Assert.Equal(OrderDeliveryMethod.AddressDelivery, redirect.RouteValues!["deliveryMethod"]);
        var subscription = await db.KitchenSubscriptions.SingleAsync();
        Assert.Equal(KitchenSubscriptionStatus.PendingPayment, subscription.Status);
        Assert.Equal(5500m, subscription.PackagePriceSnapshot);
        Assert.Equal(95m, subscription.DailyDeliveryFeeSnapshot);
        Assert.Equal(2270, subscription.DailyCalories);
        Assert.True(KitchenMealSelection.TryCreateMask(pending.SelectedMeals, out var mask));
        Assert.Equal(mask, subscription.SelectedMealSlotsMask);
        Assert.Null(page.HttpContext.Session.GetString(PendingKitchenSubscription.SessionKey));
        var checkout = Assert.IsType<KitchenCheckoutViewModel>(Assert.IsType<ViewResult>(
            await kitchen.Checkout(subscription.Id, pending.DeliveryMethod)).Model);
        Assert.Equal(5975m, checkout.AddressDeliveryTotal);
        Assert.Equal(pending.DeliveryMethod, checkout.DeliveryMethod);
        Assert.Contains("Kahvaltı", checkout.SelectedMeals);
        Assert.Contains("Öğle", checkout.SelectedMeals);
        Assert.Empty(await db.Orders.ToListAsync());
        Assert.Equal(0, (await db.MemberProfiles.SingleAsync()).RemainingClassCredits);
    }

    [Fact]
    public async Task RegisterGet_UsesCurrentMembershipCatalog_AndExcludesDemoOrUnavailableOptions()
    {
        await using var provider = CreateServices();
        var catalog = await SeedCatalogAsync(provider);
        var page = CreatePage(provider);

        Assert.IsType<PageResult>(await page.OnGetAsync());

        Assert.Equal(new[] { "HYBRID", "PRO", "BLACK" }, page.PackageOptions.Select(option => option.Name));
        Assert.Equal(new[] { "hybrid", "pro-membership", "black" }, page.PackageOptions.Select(option => option.Code));
        Assert.Equal(new[] { catalog.Hybrid.Id, catalog.Pro.Id, catalog.Black.Id }, page.ServiceOptions.Select(option => option.Id));
        Assert.Equal("hybrid", page.Input.PackageCode);
        Assert.Equal(catalog.Hybrid.Id, page.Input.ServicePackageVariantId);
        var hybrid = Assert.Single(page.ServiceOptions, option => option.Id == catalog.Hybrid.Id);
        Assert.Equal("4.000,00 ₺", hybrid.Price);
        Assert.Equal("aylık", hybrid.PriceNote);
        Assert.Equal("8.000,00 ₺", hybrid.TotalPrice);
        Assert.Equal("2 ay", hybrid.Duration);
        Assert.Contains("12 grup dersi", hybrid.Rights);
        Assert.Contains("8 Reformer dersi", hybrid.Rights);
    }

    [Theory]
    [InlineData("PLUS", "hybrid")]
    [InlineData("PRO", "pro-membership")]
    [InlineData("ELITE", "black")]
    public async Task RegisterGet_ResolvesOldLinksToCurrentCatalog(string legacyCode, string currentSlug)
    {
        await using var provider = CreateServices();
        var catalog = await SeedCatalogAsync(provider);
        var page = CreatePage(provider);
        var selectedVariant = currentSlug switch
        {
            "hybrid" => catalog.Hybrid,
            "pro-membership" => catalog.Pro,
            _ => catalog.Black
        };

        await page.OnGetAsync(package: legacyCode, variant: selectedVariant.Id);

        Assert.Equal(currentSlug, page.Input.PackageCode);
        Assert.Equal(selectedVariant.Id, page.Input.ServicePackageVariantId);
    }

    [Fact]
    public async Task RegisterGet_DoesNotReuseLegacyOptionIdsOrSelectAnotherPackagesVariant()
    {
        await using var provider = CreateServices();
        var catalog = await SeedCatalogAsync(provider);
        var page = CreatePage(provider);

        await page.OnGetAsync(package: "pro-membership", option: catalog.Hybrid.Id, variant: catalog.Black.Id);

        Assert.Equal("pro-membership", page.Input.PackageCode);
        Assert.Equal(catalog.Pro.Id, page.Input.ServicePackageVariantId);

        await page.OnGetAsync(package: "START");
        Assert.Equal("hybrid", page.Input.PackageCode);
        Assert.DoesNotContain(page.PackageOptions, option => option.Name == "START" || option.Name == "PLUS");
    }

    [Theory]
    [InlineData("demo", "Input.PackageCode")]
    [InlineData("legacy-alias", "Input.PackageCode")]
    [InlineData("cross-package", "Input.ServicePackageVariantId")]
    [InlineData("inactive-variant", "Input.ServicePackageVariantId")]
    [InlineData("inactive-parent", "Input.PackageCode")]
    [InlineData("missing-variant", "Input.ServicePackageVariantId")]
    [InlineData("unknown-variant", "Input.ServicePackageVariantId")]
    [InlineData("missing-package", "Input.PackageCode")]
    public async Task RegisterPost_RejectsUnavailableSelectionBeforeCreatingAccount(string selection, string errorKey)
    {
        await using var provider = CreateServices();
        var catalog = await SeedCatalogAsync(provider);
        var page = CreatePage(provider);
        var (slug, variantId) = selection switch
        {
            "demo" => ("START", (int?)catalog.Hybrid.Id),
            "legacy-alias" => ("PLUS", (int?)catalog.Hybrid.Id),
            "cross-package" => ("hybrid", (int?)catalog.Black.Id),
            "inactive-variant" => ("hybrid", (int?)catalog.InactiveHybrid.Id),
            "inactive-parent" => ("retired-membership", (int?)catalog.Retired.Id),
            "missing-variant" => ("hybrid", (int?)null),
            "missing-package" => ("", (int?)null),
            _ => ("hybrid", (int?)int.MaxValue)
        };
        page.Input = ValidInput(slug, variantId);

        Assert.IsType<PageResult>(await page.OnPostAsync(returnUrl: "/member"));

        Assert.NotEmpty(page.ModelState[errorKey]!.Errors);
        Assert.Equal(slug, page.Input.PackageCode);
        Assert.Equal(variantId, page.Input.ServicePackageVariantId);
        var dbContext = provider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await dbContext.Users.ToListAsync());
        Assert.Empty(await dbContext.MemberProfiles.ToListAsync());
        Assert.Null(provider.GetRequiredService<CapturingAuthenticationService>().SignedInUser);
    }

    [Fact]
    public async Task RegisterPost_PersistsSelectedCurrentVariant_AndItsDurationAndLessonRights()
    {
        await using var provider = CreateServices();
        var catalog = await SeedCatalogAsync(provider);
        var page = CreatePage(provider);
        page.Input = ValidInput("hybrid", catalog.Hybrid.Id);
        var beforeRegistration = DateTime.UtcNow;

        var result = Assert.IsType<LocalRedirectResult>(await page.OnPostAsync(returnUrl: "/member"));

        Assert.Equal("/member", result.Url);
        var dbContext = provider.GetRequiredService<ApplicationDbContext>();
        var profile = await dbContext.MemberProfiles.SingleAsync();
        Assert.Equal(catalog.Hybrid.Id, profile.ServicePackageVariantId);
        Assert.Equal(new DateOnly(1995, 6, 15), profile.BirthDate);
        Assert.Equal(catalog.Hybrid.ServicePackage.MembershipPackageId, profile.MembershipPackageId);
        Assert.NotNull(profile.MembershipStartsAtUtc);
        Assert.InRange(profile.MembershipStartsAtUtc.Value, beforeRegistration, DateTime.UtcNow);
        Assert.Equal(profile.MembershipStartsAtUtc.Value.AddMonths(2), profile.MembershipEndsAtUtc);
        Assert.Equal(48, profile.RemainingClassCredits);
        var user = await dbContext.Users.SingleAsync();
        Assert.Equal(user.Id, profile.ApplicationUserId);
        Assert.True(await provider.GetRequiredService<UserManager<ApplicationUser>>().IsInRoleAsync(user, ApplicationRoles.Member));
        var signedInUser = provider.GetRequiredService<CapturingAuthenticationService>().SignedInUser;
        Assert.NotNull(signedInUser);
        Assert.Equal(user.Id, signedInUser.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("future")]
    [InlineData("implausible")]
    public async Task RegisterPost_RejectsInvalidBirthDateBeforeCreatingAccount(string selection)
    {
        await using var provider = CreateServices();
        var catalog = await SeedCatalogAsync(provider);
        var page = CreatePage(provider);
        page.Input = ValidInput("hybrid", catalog.Hybrid.Id);
        page.Input.BirthDate = InvalidBirthDate(selection);

        Assert.IsType<PageResult>(await page.OnPostAsync(returnUrl: "/member"));

        Assert.NotEmpty(page.ModelState["Input.BirthDate"]!.Errors);
        Assert.Empty(await provider.GetRequiredService<ApplicationDbContext>().Users.ToListAsync());
        Assert.Empty(await provider.GetRequiredService<ApplicationDbContext>().MemberProfiles.ToListAsync());
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("future")]
    [InlineData("implausible")]
    public async Task AdminCreatePost_RejectsInvalidBirthDateBeforeCreatingAccount(string selection)
    {
        await using var provider = CreateServices();
        var catalog = await SeedCatalogAsync(provider);
        var controller = CreateAdminController(provider);

        Assert.IsType<ViewResult>(await controller.Create(new MemberCreateViewModel
        {
            FirstName = "Merve",
            LastName = "Kırcıoğlu",
            NationalIdentityNumber = "10000000146",
            BirthDate = InvalidBirthDate(selection),
            Password = "Test123!",
            ServicePackageVariantId = catalog.Hybrid.Id
        }));

        Assert.NotEmpty(controller.ModelState[nameof(MemberCreateViewModel.BirthDate)]!.Errors);
        Assert.Empty(await provider.GetRequiredService<ApplicationDbContext>().Users.ToListAsync());
        Assert.Empty(await provider.GetRequiredService<ApplicationDbContext>().MemberProfiles.ToListAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("1995-06-15")]
    public async Task AdminEdit_StoresBirthDate_AndAllowsLegacyMissingDate(string? birthDate)
    {
        await using var provider = CreateServices();
        var catalog = await SeedCatalogAsync(provider);
        var profile = await SeedMemberAsync(provider, catalog.Hybrid);
        var controller = CreateAdminController(provider);
        var getResult = Assert.IsType<ViewResult>(await controller.Edit(profile.Id));
        var model = Assert.IsType<MemberEditViewModel>(getResult.Model);
        Assert.Null(model.BirthDate);
        model.BirthDate = birthDate is null ? null : DateOnly.ParseExact(birthDate, "yyyy-MM-dd");

        Assert.IsType<RedirectToActionResult>(await controller.Edit(profile.Id, model));

        Assert.Equal(model.BirthDate, (await provider.GetRequiredService<ApplicationDbContext>().MemberProfiles.SingleAsync()).BirthDate);
    }

    [Fact]
    public async Task MemberProfileGet_ShowsStoredBirthDate()
    {
        await using var provider = CreateServices();
        var catalog = await SeedCatalogAsync(provider);
        var profile = await SeedMemberAsync(provider, catalog.Hybrid);
        profile.BirthDate = new DateOnly(1995, 6, 15);
        await provider.GetRequiredService<ApplicationDbContext>().SaveChangesAsync();
        var context = new DefaultHttpContext
        {
            RequestServices = provider,
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, profile.ApplicationUserId)], "test"))
        };
        var controller = new NO23.Web.Areas.Member.Controllers.ProfileController(
            provider.GetRequiredService<ApplicationDbContext>(), provider.GetRequiredService<UserManager<ApplicationUser>>())
        {
            ControllerContext = new ControllerContext { HttpContext = context },
            TempData = new TempDataDictionary(context, new MemoryTempDataProvider())
        };

        var result = Assert.IsType<ViewResult>(await controller.Index());

        Assert.Equal(profile.BirthDate, Assert.IsType<MemberProfileIndexViewModel>(result.Model).BirthDate);
    }

    [Fact]
    public async Task AdminCreateGet_ProvidesSeparatePackageAndVariantMetadata()
    {
        await using var provider = CreateServices();
        var catalog = await SeedCatalogAsync(provider);
        var controller = CreateAdminController(provider);

        Assert.IsType<ViewResult>(await controller.Create());

        var options = Assert.IsAssignableFrom<IReadOnlyList<ManualMemberPackageOptionViewModel>>(controller.ViewData["PackageOptions"]);
        var hybrid = Assert.Single(options, option => option.Id == catalog.Hybrid.Id);
        Assert.Equal(catalog.Hybrid.ServicePackageId, hybrid.PackageId);
        Assert.Equal("HYBRID", hybrid.PackageName);
        Assert.Equal("2 Aylık (1 ayı hediye)", hybrid.VariantName);
        Assert.Equal("4.000,00 ₺", hybrid.Price);
        Assert.Equal("aylık", hybrid.PriceNote);
        Assert.Equal("8.000,00 ₺", hybrid.TotalPrice);
        Assert.Equal("2 ay", hybrid.Duration);
        Assert.Contains("12 grup dersi", hybrid.Rights);
        Assert.DoesNotContain(options, option => option.Id == catalog.InactiveHybrid.Id || option.Id == catalog.Retired.Id);
    }

    [Fact]
    public async Task AdminCreatePost_DoesNotReinsertPostedInactiveVariantIntoSelectableOptions()
    {
        await using var provider = CreateServices();
        var catalog = await SeedCatalogAsync(provider);
        var controller = CreateAdminController(provider);

        var result = await controller.Create(new MemberCreateViewModel
        {
            FirstName = "Merve",
            LastName = "Kırcıoğlu",
            NationalIdentityNumber = "10000000146",
            BirthDate = new DateOnly(1995, 6, 15),
            Password = "Test123!",
            ServicePackageVariantId = catalog.InactiveHybrid.Id
        });

        Assert.IsType<ViewResult>(result);
        Assert.NotEmpty(controller.ModelState[nameof(MemberCreateViewModel.ServicePackageVariantId)]!.Errors);
        var options = Assert.IsAssignableFrom<IReadOnlyList<ManualMemberPackageOptionViewModel>>(controller.ViewData["PackageOptions"]);
        Assert.DoesNotContain(options, option => option.Id == catalog.InactiveHybrid.Id);
        Assert.Empty(await provider.GetRequiredService<ApplicationDbContext>().Users.ToListAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(int.MaxValue)]
    public async Task AdminCreatePost_RequiresSportsPackageEvenWhenAdminEntersAnAmount(int? variantId)
    {
        await using var provider = CreateServices();
        await SeedCatalogAsync(provider);
        var controller = CreateAdminController(provider);
        var result = await controller.Create(new MemberCreateViewModel
        {
            FirstName = "Merve", LastName = "Kırcıoğlu", NationalIdentityNumber = "10000000146",
            BirthDate = new DateOnly(1995, 6, 15), Password = "Test123!",
            ServicePackageVariantId = variantId,
            Payment = new ManualPaymentInputViewModel { Amount = 7000m, Method = PaymentMethod.Cash }
        });
        Assert.IsType<ViewResult>(result);
        Assert.NotEmpty(controller.ModelState[nameof(MemberCreateViewModel.ServicePackageVariantId)]!.Errors);
        var db = provider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await db.Users.ToListAsync());
        Assert.Empty(await db.MemberProfiles.ToListAsync());
        Assert.Empty(await db.Orders.ToListAsync());
    }

    [Theory]
    [InlineData(8000, 5000)]
    [InlineData(7000, 4000)]
    [InlineData(10000, 7000)]
    public async Task AdminCreatePost_PersistsChosenSportsPackageWithAgreedAmountAndInstallmentSchedule(decimal amount, decimal secondInstallment)
    {
        await using var provider = CreateServices();
        var catalog = await SeedCatalogAsync(provider);
        var controller = CreateAdminController(provider);
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "admin-1")], "test"));
        var today = DateOnly.FromDateTime(ClubTime.Now);

        var result = await controller.Create(new MemberCreateViewModel
        {
            FirstName = "Merve",
            LastName = "Kırcıoğlu",
            NationalIdentityNumber = "10000000146",
            BirthDate = new DateOnly(1995, 6, 15),
            PhoneNumber = "+905530048000",
            Password = "Test123!",
            ServicePackageVariantId = catalog.Hybrid.Id,
            Payment = new ManualPaymentInputViewModel
            {
                Method = PaymentMethod.CreditCard,
                Amount = amount,
                InstallmentCount = 2,
                FirstDueDate = today,
                Reference = "  POS-TEST  ",
                Installments = [new() { Amount = 3000m, DueDate = today, PaidOn = today },
                    new() { Amount = secondInstallment, DueDate = today.AddMonths(1) }]
            }
        });

        Assert.IsType<RedirectToActionResult>(result);
        var dbContext = provider.GetRequiredService<ApplicationDbContext>();
        Assert.Single(await dbContext.Users.ToListAsync());
        var profile = await dbContext.MemberProfiles.SingleAsync();
        Assert.Equal(new DateOnly(1995, 6, 15), profile.BirthDate);
        Assert.Equal(catalog.Hybrid.Id, profile.ServicePackageVariantId);
        Assert.Equal(48, profile.RemainingClassCredits);
        var order = await dbContext.Orders.Include(item => item.PaymentInstallments).Include(item => item.Items).SingleAsync();
        Assert.Equal(profile.Id, order.MemberProfileId);
        Assert.Equal(catalog.Hybrid.Id, order.ServicePackageVariantId);
        Assert.True(order.IsManualPayment);
        Assert.Equal(OrderType.ServicePackagePurchase, order.Type);
        Assert.Equal(PaymentMethod.CreditCard, order.PaymentMethod);
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
        Assert.Equal(amount, order.Total);
        Assert.Equal(Math.Max(8000m, amount), order.Subtotal);
        Assert.Equal(order.Subtotal - amount, order.DiscountAmount);
        Assert.Equal("POS-TEST", order.PaymentReference);
        Assert.Equal("admin-1", order.PaymentRecordedByUserId);
        Assert.Equal("HYBRID — 2 Aylık (1 ayı hediye)", Assert.Single(order.Items).ProductName);
        var installments = order.PaymentInstallments.OrderBy(item => item.Number).ToArray();
        Assert.Equal(new[] { 3000m, secondInstallment }, installments.Select(item => item.Amount));
        Assert.Equal(new[] { today, today.AddMonths(1) }, installments.Select(item => item.DueDate));
        Assert.Equal(today, installments[0].PaidOn);
        Assert.Equal("admin-1", installments[0].RecordedByUserId);
        Assert.Equal("POS-TEST", installments[0].PaymentReference);
        Assert.Null(installments[1].PaidOn);
        Assert.Null(installments[1].RecordedByUserId);
        Assert.Equal(2, await dbContext.Set<OrderPaymentInstallment>().CountAsync());
    }

    [Theory]
    [InlineData(PaymentMethod.Cash)]
    [InlineData(PaymentMethod.BankTransfer)]
    public async Task AdminCreatePost_RecordsMembershipWithCashOrTransfer(PaymentMethod method)
    {
        await using var provider = CreateServices();
        var catalog = await SeedCatalogAsync(provider);
        var controller = CreateAdminController(provider);

        var result = await controller.Create(new MemberCreateViewModel
        {
            FirstName = "Merve",
            LastName = "Kırcıoğlu",
            NationalIdentityNumber = "10000000146",
            BirthDate = new DateOnly(1995, 6, 15),
            Password = "Test123!",
            ServicePackageVariantId = catalog.Hybrid.Id,
            Payment = new ManualPaymentInputViewModel { Method = method, Amount = 8000m }
        });

        Assert.IsType<RedirectToActionResult>(result);
        Assert.True(controller.ModelState.IsValid);
        var dbContext = provider.GetRequiredService<ApplicationDbContext>();
        Assert.Single(await dbContext.Users.ToListAsync());
        var member = Assert.Single(await dbContext.MemberProfiles.ToListAsync());
        var order = Assert.Single(await dbContext.Orders.ToListAsync());
        Assert.Equal(member.Id, order.MemberProfileId);
        Assert.Equal(method, order.PaymentMethod);
        Assert.Equal(8000m, order.Total);
        Assert.True(order.IsManualPayment);
        Assert.Equal(8000m, Assert.Single(await dbContext.Set<OrderPaymentInstallment>().ToListAsync()).Amount);
    }

    private static RegisterModel.InputModel ValidInput(string packageCode, int? variantId) => new()
    {
        FirstName = "Merve",
        LastName = "Kırcıoğlu",
        NationalIdentityNumber = "10000000146",
        BirthDate = new DateOnly(1995, 6, 15),
        PhoneNumber = "+905530048000",
        PackageCode = packageCode,
        ServicePackageVariantId = variantId,
        Password = "Test123!",
        ConfirmPassword = "Test123!"
    };

    private static DateOnly? InvalidBirthDate(string selection)
    {
        var today = DateOnly.FromDateTime(ClubTime.Now);
        return selection switch
        {
            "missing" => null,
            "future" => today.AddDays(1),
            _ => today.AddYears(-120).AddDays(-1)
        };
    }

    private static async Task<MemberProfile> SeedMemberAsync(ServiceProvider provider, ServicePackageVariant variant)
    {
        var user = new ApplicationUser
        {
            FirstName = "Merve",
            LastName = "Kırcıoğlu",
            UserName = MemberLoginName.BuildUserName("Merve", "Kırcıoğlu")
        };
        Assert.True((await provider.GetRequiredService<UserManager<ApplicationUser>>().CreateAsync(user, "Test123!")).Succeeded);
        var profile = new MemberProfile
        {
            ApplicationUserId = user.Id,
            NationalIdentityNumber = "10000000146",
            MembershipPackageId = variant.ServicePackage.MembershipPackageId!.Value,
            ServicePackageVariantId = variant.Id,
            ReferralCode = "NO23-BIRTH-DATE"
        };
        var dbContext = provider.GetRequiredService<ApplicationDbContext>();
        dbContext.MemberProfiles.Add(profile);
        await dbContext.SaveChangesAsync();
        return profile;
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddAuthentication();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<ApplicationUser>(options => options.User.RequireUniqueEmail = false)
            .AddRoles<IdentityRole>()
            .AddUserValidator<OptionalEmailUserValidator>()
            .AddSignInManager()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddSingleton<CapturingAuthenticationService>();
        services.AddSingleton<IAuthenticationService>(provider => provider.GetRequiredService<CapturingAuthenticationService>());
        return services.BuildServiceProvider();
    }

    private static RegisterModel CreatePage(ServiceProvider provider)
    {
        var httpContext = new DefaultHttpContext { RequestServices = provider };
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;
        return new RegisterModel(provider.GetRequiredService<UserManager<ApplicationUser>>(),
            provider.GetRequiredService<SignInManager<ApplicationUser>>(), provider.GetRequiredService<ApplicationDbContext>())
        {
            PageContext = new PageContext { HttpContext = httpContext },
            Url = new UrlHelper(new ActionContext(httpContext, new RouteData(), new ActionDescriptor()))
        };
    }

    private static MembersController CreateAdminController(ServiceProvider provider)
    {
        var httpContext = new DefaultHttpContext { RequestServices = provider };
        var controller = new MembersController(provider.GetRequiredService<ApplicationDbContext>(),
            provider.GetRequiredService<UserManager<ApplicationUser>>(), TestMembershipModule.CreateDirectory(provider.GetRequiredService<ApplicationDbContext>()))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext, RouteData = new(), ActionDescriptor = new() },
            TempData = new TempDataDictionary(httpContext, new MemoryTempDataProvider())
        };
        controller.Url = new UrlHelper(controller.ControllerContext);
        return controller;
    }

    private static async Task<Catalog> SeedCatalogAsync(ServiceProvider provider)
    {
        var dbContext = provider.GetRequiredService<ApplicationDbContext>();
        var legacyPackages = Enum.GetValues<MembershipPackageCode>().Select(code => new MembershipPackage
        {
            Code = code,
            Name = code.ToString().ToUpperInvariant(),
            Audience = "Eski demo",
            Description = "Eski demo paket",
            IsActive = true
        }).ToDictionary(package => package.Code);
        dbContext.MembershipPackages.AddRange(legacyPackages.Values);
        dbContext.MembershipPackageOptions.Add(new MembershipPackageOption
        {
            MembershipPackage = legacyPackages[MembershipPackageCode.Start],
            Name = "Eski START seçeneği",
            Description = "Eski demo seçenek",
            DurationDays = 30,
            GroupClassCreditCount = 99,
            IsActive = true
        });

        var hybrid = AddPackage("hybrid", "HYBRID", MembershipPackageCode.Plus, 1);
        hybrid.Name = "2 Aylık (1 ayı hediye)";
        hybrid.BillingType = ServicePackageBillingType.MonthlySubscription;
        hybrid.MonthlyPrice = 4000m;
        hybrid.DurationMonths = 2;
        hybrid.BonusMonths = 1;
        hybrid.LessonsRenewMonthly = true;
        hybrid.GroupClassCreditCount = 12;
        hybrid.ReformerClassCreditCount = 8;
        hybrid.PerformanceClassCreditCount = 4;
        var pro = AddPackage("pro-membership", "PRO", MembershipPackageCode.Pro, 2);
        var black = AddPackage("black", "BLACK", MembershipPackageCode.Elite, 3);
        var retired = AddPackage("retired-membership", "Emekli üyelik", MembershipPackageCode.Plus, 4);
        retired.ServicePackage.IsActive = false;
        var inactiveHybrid = new ServicePackageVariant
        {
            ServicePackage = hybrid.ServicePackage,
            Name = "Eski HYBRID süresi",
            BillingType = ServicePackageBillingType.OneTime,
            IsActive = false
        };
        dbContext.ServicePackageVariants.Add(inactiveHybrid);
        var pt = AddPackage("pt-flex", "FLEX", MembershipPackageCode.Start, 5);
        pt.ServicePackage.Category = ServicePackageCategory.PersonalTraining;
        var noActiveVariants = AddPackage("unavailable-membership", "Aktif süresi yok", MembershipPackageCode.Plus, 6);
        noActiveVariants.IsActive = false;
        await dbContext.SaveChangesAsync();
        Assert.True((await provider.GetRequiredService<RoleManager<IdentityRole>>().CreateAsync(new IdentityRole(ApplicationRoles.Member))).Succeeded);
        return new Catalog(hybrid, pro, black, inactiveHybrid, retired);

        ServicePackageVariant AddPackage(string slug, string name, MembershipPackageCode legacyCode, int order)
        {
            var package = new ServicePackage
            {
                Category = ServicePackageCategory.Membership,
                Slug = slug,
                Name = name,
                Subtitle = "Güncel paket",
                Description = "Güncel paket açıklaması",
                MembershipPackage = legacyPackages[legacyCode],
                IsActive = true,
                DisplayOrder = order
            };
            var variant = new ServicePackageVariant
            {
                ServicePackage = package,
                Name = "1 Ay",
                BillingType = ServicePackageBillingType.OneTime,
                TotalPrice = 6000m,
                DurationMonths = 1,
                GroupClassCreditCount = 10,
                IsActive = true
            };
            dbContext.ServicePackageVariants.Add(variant);
            return variant;
        }
    }

    private sealed record Catalog(ServicePackageVariant Hybrid, ServicePackageVariant Pro,
        ServicePackageVariant Black, ServicePackageVariant InactiveHybrid, ServicePackageVariant Retired);

    private sealed class MemoryTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }

    private sealed class MemorySession : ISession
    {
        private readonly Dictionary<string, byte[]> values = [];
        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => values.Keys;
        public void Clear() => values.Clear();
        public void Remove(string key) => values.Remove(key);
        public void Set(string key, byte[] value) => values[key] = value;
        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => values.TryGetValue(key, out value);
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoPaymentClient : IIyzicoCheckoutClient
    {
        public Task<IyzicoCheckoutInitializeResult> InitializeAsync(IyzicoCheckoutInitializeRequest request,
            CancellationToken cancellationToken = default) => throw new InvalidOperationException("Registration must not initiate payment.");
        public Task<IyzicoCheckoutRetrieveResult> RetrieveAsync(string conversationId, string token,
            CancellationToken cancellationToken = default) => throw new InvalidOperationException("Registration must not retrieve payment.");
    }

    private sealed class CapturingAuthenticationService : IAuthenticationService
    {
        public ClaimsPrincipal? SignedInUser { get; private set; }
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
            Task.FromResult(AuthenticateResult.NoResult());
        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        {
            SignedInUser = principal;
            return Task.CompletedTask;
        }
        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
    }
}
