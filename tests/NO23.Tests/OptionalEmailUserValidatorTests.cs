using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Infrastructure.Validation;

namespace NO23.Tests;

public class OptionalEmailUserValidatorTests
{
    [Fact]
    public async Task CreateAsync_AllowsUserWithoutEmail()
    {
        await using var provider = CreateServices();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        var result = await userManager.CreateAsync(
            new ApplicationUser { UserName = "member-without-email" },
            "Test123!");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateEmailWhenEmailIsProvided()
    {
        await using var provider = CreateServices();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var firstResult = await userManager.CreateAsync(
            new ApplicationUser
            {
                UserName = "first-user",
                Email = "user@no23.test"
            },
            "Test123!");

        var secondResult = await userManager.CreateAsync(
            new ApplicationUser
            {
                UserName = "second-user",
                Email = "user@no23.test"
            },
            "Test123!");

        Assert.True(firstResult.Succeeded);
        Assert.False(secondResult.Succeeded);
        Assert.Contains(secondResult.Errors, error =>
            error.Code == nameof(IdentityErrorDescriber.DuplicateEmail));
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
            .AddErrorDescriber<TurkishIdentityErrorDescriber>()
            .AddUserValidator<OptionalEmailUserValidator>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        return services.BuildServiceProvider();
    }
}
