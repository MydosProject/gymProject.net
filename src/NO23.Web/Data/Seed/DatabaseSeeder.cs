using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Data.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await SeedRolesAsync(roleManager);
        await SeedMembershipPackagesAsync(dbContext);
        await SeedClassOperationsAsync(dbContext);
        await SeedKitchenMenuItemsAsync(dbContext);
        await SeedShopProductsAsync(dbContext);
        await SeedCommunityContentAsync(dbContext);
        await SeedAdminUserAsync(userManager, configuration);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in ApplicationRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var adminUser = await userManager.FindByEmailAsync(email);

        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = configuration["SeedAdmin:FirstName"],
                LastName = configuration["SeedAdmin:LastName"]
            };

            var createResult = await userManager.CreateAsync(adminUser, password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Default admin user could not be created: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, ApplicationRoles.Admin))
        {
            await userManager.AddToRoleAsync(adminUser, ApplicationRoles.Admin);
        }
    }

    private static async Task SeedMembershipPackagesAsync(ApplicationDbContext dbContext)
    {
        foreach (var defaultPackage in MembershipPackageSeed.Defaults)
        {
            var package = await dbContext.MembershipPackages
                .FirstOrDefaultAsync(existing => existing.Code == defaultPackage.Code);

            if (package is null)
            {
                dbContext.MembershipPackages.Add(defaultPackage);
                continue;
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedClassOperationsAsync(ApplicationDbContext dbContext)
    {
        if (await dbContext.Trainers.AnyAsync() || await dbContext.GroupClasses.AnyAsync())
        {
            return;
        }

        var trainers = new[]
        {
            new Trainer
            {
                FirstName = "Deniz",
                LastName = "Arslan",
                Specialty = "Functional Training",
                Certifications = "NASM CPT, Functional Training Specialist",
                Bio = "Metcon, bootcamp ve kuvvet odaklı grup dersleri verir."
            },
            new Trainer
            {
                FirstName = "Ece",
                LastName = "Kaya",
                Specialty = "Pilates",
                Certifications = "Mat Pilates, Reformer Pilates",
                Bio = "Postür, core ve mobilite odaklı pilates dersleri verir."
            }
        };

        var bootcamp = new GroupClass
        {
            Name = "Bootcamp",
            Description = "Yüksek tempo, kuvvet ve kondisyon odaklı grup dersi.",
            DurationMinutes = 50,
            DifficultyLevel = ClassDifficultyLevel.Intermediate,
            AverageCaloriesBurned = 520,
            Capacity = 16,
            Trainer = trainers[0]
        };

        var reformerPilates = new GroupClass
        {
            Name = "Reformer Pilates",
            Description = "Kontrollü güç, core stabilizasyonu ve mobilite odaklı ders.",
            DurationMinutes = 45,
            DifficultyLevel = ClassDifficultyLevel.AllLevels,
            AverageCaloriesBurned = 280,
            Capacity = 8,
            Trainer = trainers[1]
        };

        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        var nextDay = DateTime.UtcNow.Date.AddDays(2);

        bootcamp.Sessions.Add(new ClassSession
        {
            StartsAtUtc = tomorrow.AddHours(15)
        });

        bootcamp.Sessions.Add(new ClassSession
        {
            StartsAtUtc = nextDay.AddHours(16)
        });

        reformerPilates.Sessions.Add(new ClassSession
        {
            StartsAtUtc = tomorrow.AddHours(8),
            CapacityOverride = 6
        });

        dbContext.Trainers.AddRange(trainers);
        dbContext.GroupClasses.AddRange(bootcamp, reformerPilates);

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedKitchenMenuItemsAsync(ApplicationDbContext dbContext)
    {
        foreach (var defaultItem in KitchenMenuItemSeed.Defaults)
        {
            var item = await dbContext.KitchenMenuItems
                .FirstOrDefaultAsync(existing => existing.Name == defaultItem.Name);

            if (item is null)
            {
                dbContext.KitchenMenuItems.Add(defaultItem);
                continue;
            }

            item.Description = defaultItem.Description;
            item.Category = defaultItem.Category;
            item.Calories = defaultItem.Calories;
            item.UnitPrice = defaultItem.UnitPrice;
            item.ProteinGrams = defaultItem.ProteinGrams;
            item.CarbohydrateGrams = defaultItem.CarbohydrateGrams;
            item.FatGrams = defaultItem.FatGrams;
            item.Ingredients = defaultItem.Ingredients;
            item.Allergens = defaultItem.Allergens;
            item.Tags = defaultItem.Tags;
            item.IsPlanEligible = defaultItem.IsPlanEligible;
            item.DisplayOrder = defaultItem.DisplayOrder;
            item.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedShopProductsAsync(ApplicationDbContext dbContext)
    {
        if (await dbContext.ShopProducts.AnyAsync())
        {
            return;
        }

        dbContext.ShopProducts.AddRange(ShopProductSeed.Defaults);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedCommunityContentAsync(ApplicationDbContext dbContext)
    {
        foreach (var defaultEvent in CommunityContentSeed.Events)
        {
            if (!await dbContext.CommunityEvents.AnyAsync(item => item.Slug == defaultEvent.Slug))
            {
                dbContext.CommunityEvents.Add(defaultEvent);
            }
        }

        foreach (var defaultChallenge in CommunityContentSeed.Challenges)
        {
            var challenge = await dbContext.CommunityChallenges
                .FirstOrDefaultAsync(item => item.Slug == defaultChallenge.Slug);

            if (challenge is null)
            {
                dbContext.CommunityChallenges.Add(defaultChallenge);
                continue;
            }

            challenge.TargetDailyCalories = defaultChallenge.TargetDailyCalories;
            challenge.CalorieTolerancePercent = defaultChallenge.CalorieTolerancePercent;
            challenge.RequiredCompletionPercent = defaultChallenge.RequiredCompletionPercent;
            challenge.UpdatedAtUtc = DateTime.UtcNow;
        }

        foreach (var defaultPost in CommunityContentSeed.BlogPosts)
        {
            if (!await dbContext.BlogPosts.AnyAsync(item => item.Slug == defaultPost.Slug))
            {
                dbContext.BlogPosts.Add(defaultPost);
            }
        }

        foreach (var defaultStory in CommunityContentSeed.SuccessStories)
        {
            if (!await dbContext.SuccessStories.AnyAsync(item => item.Slug == defaultStory.Slug))
            {
                dbContext.SuccessStories.Add(defaultStory);
            }
        }

        await dbContext.SaveChangesAsync();
    }
}
