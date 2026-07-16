using Microsoft.AspNetCore.Identity;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await SeedRolesAsync(roleManager);
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
}
