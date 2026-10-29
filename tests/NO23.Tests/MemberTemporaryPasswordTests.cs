using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Infrastructure.Validation;
using NO23.Web.ViewModels.Admin;

namespace NO23.Tests;

public class MemberTemporaryPasswordTests
{
    [Theory]
    [InlineData("Öztürk23+")]
    [InlineData("Üyelik23+")]
    [InlineData("İstanbul23+")]
    [InlineData("Çalışma23+")]
    [InlineData("Test123+")]
    public async Task OneUppercaseIncludingTurkishLetters_IsAccepted(string password)
    {
        await using var provider = Services();
        var manager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var result = await manager.CreateAsync(new() { UserName = "member@test.local" }, password);
        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(item => item.Description)));
    }

    [Theory]
    [InlineData("öztürk23+", "PasswordRequiresUpper")]
    [InlineData("Öztürk23", "PasswordRequiresNonAlphanumeric")]
    [InlineData("Öztürk++", "PasswordRequiresDigit")]
    [InlineData("ÖZTÜRK23+", "PasswordRequiresLower")]
    [InlineData("Öz1+", "PasswordTooShort")]
    public async Task RemainingPasswordRequirements_ArePreserved(string password, string error)
    {
        await using var provider = Services();
        var manager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var result = await manager.CreateAsync(new() { UserName = "member@test.local" }, password);
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, item => item.Code == error);
    }

    [Fact]
    public async Task AdminCanRenewTemporaryPasswordRepeatedlyAndOldPasswordsStopWorking()
    {
        await using var provider = Services();
        var db = provider.GetRequiredService<ApplicationDbContext>();
        var manager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "member@test.local", FirstName = "Test" };
        Assert.True((await manager.CreateAsync(user, "Öztürk23+")).Succeeded);
        var member = new MemberProfile { ApplicationUser = user, MembershipPackage = new() { Name = "Test", Audience = "Test", Description = "Test" } };
        db.MemberProfiles.Add(member);
        await db.SaveChangesAsync();
        var controller = Controller(db, manager);
        var previous = "Öztürk23+";
        foreach (var password in new[] { "Üyelik24+", "İstanbul25+" })
        {
            var stamp = await manager.GetSecurityStampAsync(user);
            var result = await controller.ResetPassword(member.Id, new() { Id = member.Id, Password = password, ConfirmPassword = password });
            Assert.IsType<RedirectToActionResult>(result);
            Assert.False(await manager.CheckPasswordAsync(user, previous));
            Assert.True(await manager.CheckPasswordAsync(user, password));
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
            Assert.DoesNotContain(password, controller.TempData.Values.OfType<string>());
            previous = password;
        }
    }

    [Fact]
    public async Task InvalidReset_DoesNotReplaceExistingPassword()
    {
        await using var provider = Services();
        var db = provider.GetRequiredService<ApplicationDbContext>();
        var manager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "member@test.local" };
        Assert.True((await manager.CreateAsync(user, "Öztürk23+")).Succeeded);
        var member = new MemberProfile { ApplicationUser = user, MembershipPackage = new() { Name = "Test", Audience = "Test", Description = "Test" } };
        db.MemberProfiles.Add(member);
        await db.SaveChangesAsync();
        var controller = Controller(db, manager);
        Assert.IsType<ViewResult>(await controller.ResetPassword(member.Id, new() { Id = member.Id, Password = "öztürk23+", ConfirmPassword = "öztürk23+" }));
        Assert.True(await manager.CheckPasswordAsync(user, "Öztürk23+"));
        Assert.False(controller.ModelState.IsValid);
        Assert.IsType<BadRequestResult>(await controller.ResetPassword(member.Id, new() { Id = member.Id + 1 }));
        Assert.IsType<NotFoundResult>(await controller.ResetPassword(int.MaxValue));
    }

    private static ServiceProvider Services()
    {
        var services = new ServiceCollection();
        services.AddLogging(); services.AddDataProtection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<ApplicationUser>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders().AddErrorDescriber<TurkishIdentityErrorDescriber>();
        services.RemoveAll<IPasswordValidator<ApplicationUser>>();
        services.AddScoped<IPasswordValidator<ApplicationUser>, UnicodePasswordValidator>();
        return services.BuildServiceProvider();
    }
    private static NO23.Web.Areas.Admin.Controllers.MembersController Controller(ApplicationDbContext db, UserManager<ApplicationUser> manager)
    {
        var context = new DefaultHttpContext();
        var controller = new NO23.Web.Areas.Admin.Controllers.MembersController(db, manager, TestMembershipModule.CreateDirectory(db)) { ControllerContext = new() { HttpContext = context } };
        controller.Url = new UrlHelper(new ActionContext(context, new RouteData(), new ActionDescriptor()));
        controller.TempData = new TempDataDictionary(context, new MemoryTempData());
        return controller;
    }
    private sealed class MemoryTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
