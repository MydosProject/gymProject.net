using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Data.Seed;

public static class DatabaseSeeder
{
    private static readonly IReadOnlyDictionary<KitchenSubscriptionPlan, (string Name, string Description)>
        LegacyKitchenSubscriptionPackageTexts = new Dictionary<KitchenSubscriptionPlan, (string Name, string Description)>
        {
            [KitchenSubscriptionPlan.FiveDays] = (
                "5 Gunluk Kitchen Paketi",
                "Kalori ve makro hedeflerine gore hazirlanan 5 gunluk NO23 Kitchen yemek paketi."),
            [KitchenSubscriptionPlan.TenDays] = (
                "10 Gunluk Kitchen Paketi",
                "Duzenli beslenme ritmini kurmak icin 10 gunluk NO23 Kitchen yemek paketi."),
            [KitchenSubscriptionPlan.TwentyDays] = (
                "20 Gunluk Kitchen Paketi",
                "Uzun sureli hedef takibi icin 20 gunluk NO23 Kitchen yemek paketi."),
            [KitchenSubscriptionPlan.Monthly] = (
                "Aylik Kitchen Paketi",
                "Aylik rutin olusturmak isteyen uyeler icin 30 gunluk NO23 Kitchen yemek paketi.")
        };

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
        await SeedKitchenSubscriptionPackagesAsync(dbContext);
        await SeedKitchenMealSlotPricesAsync(dbContext);
        await SeedKitchenMenuItemsAsync(dbContext);
        await SeedKitchenStockAsync(dbContext);
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
        var existingTrainers = await dbContext.Trainers.ToListAsync();
        var existingClasses = await dbContext.GroupClasses.ToListAsync();
        var trainers = new List<Trainer>();

        var defaultTrainers = new[]
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

        foreach (var defaultTrainer in defaultTrainers)
        {
            var trainer = existingTrainers.FirstOrDefault(existing =>
                string.Equals(existing.FirstName, defaultTrainer.FirstName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(existing.LastName, defaultTrainer.LastName, StringComparison.OrdinalIgnoreCase));

            if (trainer is null)
            {
                dbContext.Trainers.Add(defaultTrainer);
                existingTrainers.Add(defaultTrainer);
                trainer = defaultTrainer;
            }

            trainers.Add(trainer);
        }

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

        if (!existingClasses.Any(existing =>
                string.Equals(existing.Name, bootcamp.Name, StringComparison.OrdinalIgnoreCase)))
        {
            dbContext.GroupClasses.Add(bootcamp);
        }

        if (!existingClasses.Any(existing =>
                string.Equals(existing.Name, reformerPilates.Name, StringComparison.OrdinalIgnoreCase)))
        {
            dbContext.GroupClasses.Add(reformerPilates);
        }

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
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedKitchenStockAsync(ApplicationDbContext dbContext)
    {
        var existingIngredientsByName = (await dbContext.KitchenIngredients.ToListAsync())
            .GroupBy(ingredient => ingredient.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var newIngredients = new List<KitchenIngredient>();

        foreach (var defaultIngredient in KitchenStockSeed.Ingredients)
        {
            if (existingIngredientsByName.ContainsKey(defaultIngredient.Name))
            {
                continue;
            }

            var ingredient = new KitchenIngredient
            {
                Name = defaultIngredient.Name,
                Unit = defaultIngredient.Unit,
                CurrentStockQuantity = defaultIngredient.CurrentStockQuantity,
                MinimumStockQuantity = defaultIngredient.MinimumStockQuantity,
                IsActive = true
            };

            dbContext.KitchenIngredients.Add(ingredient);
            newIngredients.Add(ingredient);
        }

        if (newIngredients.Count > 0)
        {
            await dbContext.SaveChangesAsync();

            foreach (var ingredient in newIngredients.Where(item => item.CurrentStockQuantity > 0))
            {
                dbContext.KitchenStockMovements.Add(new KitchenStockMovement
                {
                    KitchenIngredientId = ingredient.Id,
                    Type = KitchenStockMovementType.StockIn,
                    Quantity = ingredient.CurrentStockQuantity,
                    QuantityBeforeSnapshot = 0,
                    QuantityAfterSnapshot = ingredient.CurrentStockQuantity,
                    Note = "Seed başlangıç stok girişi"
                });
            }
        }

        var ingredientsByName = (await dbContext.KitchenIngredients.ToListAsync())
            .GroupBy(ingredient => ingredient.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var menuItemsByName = (await dbContext.KitchenMenuItems.ToListAsync())
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var menuItemIds = menuItemsByName.Values
            .Select(item => item.Id)
            .ToList();
        var existingRecipeKeys = await dbContext.KitchenRecipeIngredients
            .Where(recipe => menuItemIds.Contains(recipe.KitchenMenuItemId))
            .Select(recipe => recipe.KitchenMenuItemId + ":" + recipe.KitchenIngredientId)
            .ToListAsync();
        var existingRecipeKeySet = existingRecipeKeys.ToHashSet();

        foreach (var defaultRecipe in KitchenStockSeed.Recipes)
        {
            if (!menuItemsByName.TryGetValue(defaultRecipe.KitchenMenuItemName, out var menuItem) ||
                !ingredientsByName.TryGetValue(defaultRecipe.IngredientName, out var ingredient))
            {
                continue;
            }

            var recipeKey = menuItem.Id + ":" + ingredient.Id;

            if (existingRecipeKeySet.Contains(recipeKey))
            {
                continue;
            }

            dbContext.KitchenRecipeIngredients.Add(new KitchenRecipeIngredient
            {
                KitchenMenuItemId = menuItem.Id,
                KitchenIngredientId = ingredient.Id,
                QuantityPerPortion = defaultRecipe.QuantityPerPortion
            });
            existingRecipeKeySet.Add(recipeKey);
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedKitchenSubscriptionPackagesAsync(ApplicationDbContext dbContext)
    {
        foreach (var defaultPackage in KitchenSubscriptionPackageSeed.Defaults)
        {
            var package = await dbContext.KitchenSubscriptionPackages
                .FirstOrDefaultAsync(existing => existing.Plan == defaultPackage.Plan);

            if (package is null)
            {
                dbContext.KitchenSubscriptionPackages.Add(defaultPackage);
                continue;
            }

            if (!LegacyKitchenSubscriptionPackageTexts.TryGetValue(package.Plan, out var legacyTexts))
            {
                continue;
            }

            var updatedText = false;

            if (package.Name == legacyTexts.Name)
            {
                package.Name = defaultPackage.Name;
                updatedText = true;
            }

            if (package.Description == legacyTexts.Description)
            {
                package.Description = defaultPackage.Description;
                updatedText = true;
            }

            if (updatedText)
            {
                package.UpdatedAtUtc = DateTime.UtcNow;
            }

            var subscriptionsWithLegacySnapshot = await dbContext.KitchenSubscriptions
                .Where(subscription =>
                    subscription.Plan == defaultPackage.Plan &&
                    subscription.PackageNameSnapshot == legacyTexts.Name)
                .ToListAsync();

            foreach (var subscription in subscriptionsWithLegacySnapshot)
            {
                subscription.PackageNameSnapshot = defaultPackage.Name;
                subscription.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedKitchenMealSlotPricesAsync(ApplicationDbContext dbContext)
    {
        foreach (var defaultPrice in KitchenMealSlotPriceSeed.Defaults)
        {
            var exists = await dbContext.KitchenMealSlotPrices
                .AnyAsync(price => price.MealSlot == defaultPrice.MealSlot);

            if (exists)
            {
                continue;
            }

            dbContext.KitchenMealSlotPrices.Add(defaultPrice);
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedShopProductsAsync(ApplicationDbContext dbContext)
    {
        var existingSkus = await dbContext.ShopProducts
            .Select(product => product.Sku)
            .ToListAsync();
        var existingSkuSet = existingSkus.ToHashSet(StringComparer.OrdinalIgnoreCase);

        dbContext.ShopProducts.AddRange(ShopProductSeed.Defaults
            .Where(product => !existingSkuSet.Contains(product.Sku)));
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
