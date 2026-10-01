using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NO23.Web.Areas.Admin.Controllers;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Infrastructure.Identity;
using NO23.Web.Infrastructure.Validation;
using NO23.Web.ViewModels.Admin;

namespace NO23.Tests;

public class MemberRegistrationFlowTests
{
    [Fact]
    public void AdminMemberModels_AllowMissingEmail()
    {
        var createModel = new MemberCreateViewModel
        {
            FirstName = "Merve",
            LastName = "Kırcıoğlu",
            NationalIdentityNumber = "10000000146",
            Password = "Test123!",
            ServicePackageVariantId = 1
        };
        var editModel = new MemberEditViewModel
        {
            FirstName = "Merve",
            LastName = "Kırcıoğlu",
            NationalIdentityNumber = "10000000146"
        };

        Assert.DoesNotContain(Validate(createModel), result =>
            result.MemberNames.Contains(nameof(MemberCreateViewModel.Email)));
        Assert.DoesNotContain(Validate(editModel), result =>
            result.MemberNames.Contains(nameof(MemberEditViewModel.Email)));
    }

    [Fact]
    public async Task AdminCreate_CreatesMemberWithoutEmail_AndSetsMembershipDates()
    {
        await using var provider = CreateServices();
        var dbContext = provider.GetRequiredService<ApplicationDbContext>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        await roleManager.CreateAsync(new IdentityRole(ApplicationRoles.Member));

        var legacyPackage = new MembershipPackage
        {
            Code = MembershipPackageCode.Start,
            Name = "START",
            Audience = "Test",
            Description = "Test",
            IsActive = true
        };
        var servicePackage = new ServicePackage
        {
            Category = ServicePackageCategory.PersonalTraining,
            Slug = "flex-test",
            Name = "FLEX",
            Subtitle = "Test",
            Description = "Test",
            IsActive = true
        };
        var variant = new ServicePackageVariant
        {
            ServicePackage = servicePackage,
            Name = "Esnek",
            BillingType = ServicePackageBillingType.OneTime,
            TotalPrice = 14000m,
            PersonalTrainingSessionCount = 8,
            IsActive = true
        };
        dbContext.MembershipPackages.Add(legacyPackage);
        dbContext.ServicePackageVariants.Add(variant);
        await dbContext.SaveChangesAsync();

        var controller = new MembersController(dbContext, userManager)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.TempData = new TempDataDictionary(
            controller.HttpContext,
            new MemoryTempDataProvider());

        var result = await controller.Create(new MemberCreateViewModel
        {
            FirstName = "Merve",
            LastName = "Kırcıoğlu",
            NationalIdentityNumber = "10000000146",
            PhoneNumber = "+905530048000",
            Password = "Test123!",
            ServicePackageVariantId = variant.Id
        });

        Assert.IsType<RedirectToActionResult>(result);
        var user = await userManager.FindByNameAsync(
            MemberLoginName.BuildUserName("Merve", "Kırcıoğlu"));
        Assert.NotNull(user);
        Assert.Null(user.Email);
        var profile = await dbContext.MemberProfiles.SingleAsync();
        Assert.Equal("10000000146", profile.NationalIdentityNumber);
        Assert.NotNull(profile.MembershipStartsAtUtc);
        Assert.NotNull(profile.MembershipEndsAtUtc);
    }

    [Fact]
    public async Task AdminDelete_RemovesMemberWithPersonalTrainingHistory_AndPreservesOrder()
    {
        await using var provider = CreateServices();
        var dbContext = provider.GetRequiredService<ApplicationDbContext>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        await roleManager.CreateAsync(new IdentityRole(ApplicationRoles.Member));

        var user = new ApplicationUser
        {
            UserName = MemberLoginName.BuildUserName("Seda", "Uzun"),
            FirstName = "Seda",
            LastName = "Uzun"
        };
        Assert.True((await userManager.CreateAsync(user, "Test123!")).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(user, ApplicationRoles.Member)).Succeeded);

        var package = new MembershipPackage
        {
            Code = MembershipPackageCode.Start,
            Name = "START",
            Audience = "Test",
            Description = "Test"
        };
        var trainer = new Trainer
        {
            FirstName = "Test",
            LastName = "Trainer",
            Specialty = "PT"
        };
        var profile = new MemberProfile
        {
            ApplicationUserId = user.Id,
            MembershipPackage = package,
            ReferralCode = "NO23-DELETE",
            RemainingClassCredits = 0
        };
        dbContext.MemberProfiles.Add(profile);
        dbContext.Trainers.Add(trainer);
        await dbContext.SaveChangesAsync();

        dbContext.PersonalTrainingSessions.Add(new PersonalTrainingSession
        {
            MemberProfileId = profile.Id,
            TrainerId = trainer.Id,
            StartsAtUtc = DateTime.UtcNow.AddDays(-1),
            Status = PersonalTrainingSessionStatus.Completed
        });
        var order = new Order
        {
            OrderNumber = "DELETE-TEST-1",
            MemberProfileId = profile.Id,
            DeliveryFullName = "Seda Uzun",
            DeliveryPhoneNumber = "-",
            DeliveryAddressLine = "-",
            DeliveryDistrict = "-",
            DeliveryCity = "-"
        };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        var controller = SetupController(new MembersController(dbContext, userManager));
        var result = await controller.DeleteConfirmed(profile.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Null(await userManager.FindByIdAsync(user.Id));
        Assert.Empty(await dbContext.MemberProfiles.ToListAsync());
        Assert.Empty(await dbContext.PersonalTrainingSessions.ToListAsync());
        Assert.Null((await dbContext.Orders.SingleAsync()).MemberProfileId);
    }

    private static IReadOnlyList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            results,
            validateAllProperties: true);
        return results;
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services
            .AddIdentityCore<ApplicationUser>(options =>
                options.User.RequireUniqueEmail = false)
            .AddRoles<IdentityRole>()
            .AddUserValidator<OptionalEmailUserValidator>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        return services.BuildServiceProvider();
    }

    private static T SetupController<T>(T controller) where T : Controller
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.TempData = new TempDataDictionary(
            controller.HttpContext,
            new MemoryTempDataProvider());
        return controller;
    }

    private sealed class MemoryTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(
            HttpContext context,
            IDictionary<string, object> values)
        {
        }
    }
}
