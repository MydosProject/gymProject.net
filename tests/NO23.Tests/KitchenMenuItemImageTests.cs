using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Areas.Admin.Controllers;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Admin;

namespace NO23.Tests;

public class KitchenMenuItemImageTests
{
    [Fact]
    public async Task Create_SavesTrimmedImageUrl_AndEditShowsIt()
    {
        await using var db = Db();
        var controller = new KitchenMenuItemsController(db);

        var result = await controller.Create(Form(" https://cdn.example.com/bowl.jpg "));

        Assert.IsType<RedirectToActionResult>(result);
        var item = await db.KitchenMenuItems.SingleAsync();
        Assert.Equal("https://cdn.example.com/bowl.jpg", item.ImageUrl);

        var edit = Assert.IsType<ViewResult>(await controller.Edit(item.Id));
        Assert.Equal("https://cdn.example.com/bowl.jpg", Assert.IsType<KitchenMenuItemFormViewModel>(edit.Model).ImageUrl);
    }

    [Fact]
    public async Task Edit_ChangesOrClearsImageUrl()
    {
        await using var db = Db();
        var controller = new KitchenMenuItemsController(db);
        await controller.Create(Form("https://cdn.example.com/old.jpg"));
        var id = (await db.KitchenMenuItems.SingleAsync()).Id;

        var update = Form("/images/kitchen/new.webp");
        update.Id = id;
        await controller.Edit(id, update);
        Assert.Equal("/images/kitchen/new.webp", (await db.KitchenMenuItems.SingleAsync()).ImageUrl);

        var clear = Form("   ");
        clear.Id = id;
        await controller.Edit(id, clear);
        Assert.Null((await db.KitchenMenuItems.SingleAsync()).ImageUrl);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("https://cdn.example.com/a.jpg", true)]
    [InlineData("http://cdn.example.com/a.jpg", true)]
    [InlineData("/images/a.jpg", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("//evil.example.com/a.jpg", false)]
    [InlineData("ftp://files.example.com/a.jpg", false)]
    [InlineData("bowl.jpg", false)]
    public void ImageUrl_AllowsOnlyWebLinksAndSitePaths(string? url, bool valid)
    {
        var model = Form(url);
        var errors = new List<ValidationResult>();

        var ok = Validator.TryValidateObject(model, new ValidationContext(model), errors, validateAllProperties: true);

        Assert.Equal(valid, ok);
        if (!valid)
        {
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(KitchenMenuItemFormViewModel.ImageUrl)));
        }
    }

    private static KitchenMenuItemFormViewModel Form(string? imageUrl) => new()
    {
        Name = "Tavuklu bowl",
        Category = MenuItemCategory.MainMeal,
        Calories = 520,
        UnitPrice = 320,
        ProteinGrams = 42,
        CarbohydrateGrams = 48,
        FatGrams = 14,
        Ingredients = "Tavuk, pirinç, sebze",
        DisplayOrder = 1,
        ImageUrl = imageUrl
    };

    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
