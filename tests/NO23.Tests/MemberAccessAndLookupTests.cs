using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.TrainerPanel;
using Measurements = NO23.Web.Areas.Trainer.Controllers.MeasurementsController;
using Members = NO23.Web.Areas.Trainer.Controllers.MembersController;

namespace NO23.Tests;

public class MemberAccessAndLookupTests
{
    [Fact]
    public async Task KitchenMetadata_HasNoSportsPrivilegesAndCannotBeEditedAsSportsLevel()
    {
        await using var db = Db();
        var seed = MembershipPackageSeed.Defaults.Single(x => x.Code == MembershipPackageCode.KitchenOnly);
        Assert.Equal(0, seed.WeeklyClassLimit);
        Assert.False(seed.IsActive);
        Assert.False(seed.IncludesPersonalTrainingSupport);
        Assert.False(seed.IncludesCommunityMembership);
        Assert.Empty(seed.Options);
        var package = new MembershipPackage { Code = MembershipPackageCode.KitchenOnly, Name = seed.Name };
        db.MembershipPackages.Add(package);
        await db.SaveChangesAsync();
        var controller = new NO23.Web.Areas.Admin.Controllers.MembershipPackagesController(db);
        Assert.IsType<NotFoundResult>(await controller.Edit(package.Id));
        Assert.IsType<BadRequestResult>(await controller.Create(new NO23.Web.ViewModels.Admin.MembershipPackageFormViewModel { Code = MembershipPackageCode.KitchenOnly }));
        Assert.IsType<BadRequestResult>(await controller.Edit(package.Id, new NO23.Web.ViewModels.Admin.MembershipPackageFormViewModel { Id = package.Id, Code = MembershipPackageCode.KitchenOnly }));
        var profile = new MemberProfile { MembershipPackage = package, RemainingClassCredits = 100 };
        Assert.False(MemberPackageEntitlement.HasUnlimitedClassAccess(profile));
        Assert.False(MemberPackageEntitlement.IsActive(profile, DateTime.UtcNow));
    }

    [Fact]
    public async Task Search_DistinguishesDuplicateNamesAndRestrictsTrainerResults()
    {
        await using var db = Db();
        var (coach, own, other, unassigned) = await Seed(db);
        var service = new MemberLookupService(db);
        var adminResults = await service.SearchAsync("ışıl yılmaz");
        Assert.Equal(3, adminResults.Count);
        Assert.Equal(3, adminResults.Select(x => x.Id).Distinct().Count());
        Assert.Equal(3, adminResults.Select(x => x.Phone).Distinct().Count());
        var assignedResults = await service.SearchAsync("IŞIL YILMAZ", coach.Id);
        Assert.Equal(own.Id, Assert.Single(assignedResults).Id);
        Assert.Empty(await service.SearchAsync("#" + other.Id, coach.Id));
        Assert.Empty(await service.SearchAsync("#" + unassigned.Id, coach.Id));
        Assert.Equal(own.Id, Assert.Single(await service.SearchAsync("0531 111 22 33", coach.Id)).Id);
        Assert.Equal("PRO", Assert.Single(assignedResults).Package);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("I")]
    public async Task Search_DoesNotReturnAllMembersWithoutUsefulQuery(string? query)
    {
        await using var db = Db();
        await Seed(db);
        Assert.Empty(await new MemberLookupService(db).SearchAsync(query));
        Assert.Empty(await new MemberLookupService(db).SearchAsync(new string('a', 81)));
    }

    [Fact]
    public async Task Search_LimitsResultsAndDoesNotExposeNationalIdentityNumbers()
    {
        await using var db = Db();
        var (coach, own, _, _) = await Seed(db);
        for (var index = 0; index < 25; index++)
            db.MemberProfiles.Add(new MemberProfile
            {
                ApplicationUser = new ApplicationUser { UserName = "lookup-" + index, FirstName = "Işıl", LastName = "Yılmaz" },
                MembershipPackage = own.MembershipPackage, AssignedTrainerId = coach.Id
            });
        await db.SaveChangesAsync();
        var results = await new MemberLookupService(db).SearchAsync("ışıl", coach.Id);
        Assert.Equal(20, results.Count);
        var json = System.Text.Json.JsonSerializer.Serialize(results);
        Assert.DoesNotContain("NationalIdentityNumber", json);
        Assert.DoesNotContain("10000000146", json);
    }

    [Fact]
    public async Task TrainerListAndDetails_OnlyContainAssignedMembersAndExistingBalance()
    {
        await using var db = Db();
        var (coach, own, other, unassigned) = await Seed(db);
        var controller = Setup(new Members(db), coach.ApplicationUserId);
        var model = Assert.IsType<TrainerMembersViewModel>(Assert.IsType<ViewResult>(await controller.Index(null)).Model);
        Assert.Equal(1, model.TotalCount);
        Assert.Equal(own.Id, Assert.Single(model.Members).Id);
        var detail = Assert.IsType<MemberProfile>(Assert.IsType<ViewResult>(await controller.Details(own.Id)).Model);
        Assert.Equal(17, detail.RemainingClassCredits);
        Assert.IsType<NotFoundResult>(await controller.Details(other.Id));
        Assert.IsType<NotFoundResult>(await controller.Details(unassigned.Id));
        Assert.IsType<NotFoundResult>(await controller.Details(int.MaxValue));
        var filtered = Assert.IsType<TrainerMembersViewModel>(Assert.IsType<ViewResult>(await controller.Index("#" + other.Id)).Model);
        Assert.Empty(filtered.Members);
    }

    [Fact]
    public async Task TrainerMeasurements_SearchIndexAndSaveCannotReachAnotherTrainersMember()
    {
        await using var db = Db();
        var (coach, own, other, unassigned) = await Seed(db);
        var controller = Setup(new Measurements(db, new MemberProgressTrackingService(db)), coach.ApplicationUserId);
        var results = Assert.IsAssignableFrom<IReadOnlyList<MemberLookupResult>>(
            Assert.IsType<JsonResult>(await controller.SearchMembers("ışıl", default)).Value);
        Assert.Equal(own.Id, Assert.Single(results).Id);
        Assert.IsType<NotFoundResult>(await controller.Index(other.Id, null));
        Assert.IsType<NotFoundResult>(await controller.Index(unassigned.Id, null));
        Assert.IsType<NotFoundResult>(await controller.Save(other.Id,
            new NO23.Web.ViewModels.Member.MemberProgressEntryInputViewModel { EntryDate = DateOnly.FromDateTime(ClubTime.Now), BodyWeightKg = 80 }));
        Assert.Empty(await db.MemberProgressEntries.ToListAsync());
        var page = Assert.IsType<TrainerMeasurementsViewModel>(Assert.IsType<ViewResult>(await controller.Index(own.Id, null)).Model);
        Assert.Equal(own.Id, page.MemberId);
        Assert.Contains("0531", Assert.Single(page.Members).Text);
        var emptyPage = Assert.IsType<TrainerMeasurementsViewModel>(Assert.IsType<ViewResult>(await controller.Index(null, null)).Model);
        Assert.Empty(emptyPage.Members);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("unknown", true)]
    [InlineData("coach", false)]
    public async Task TrainerAccess_RequiresLinkedActiveTrainer(string? userId, bool active)
    {
        await using var db = Db();
        var (coach, own, _, _) = await Seed(db);
        coach.IsActive = active;
        await db.SaveChangesAsync();
        var members = Setup(new Members(db), userId);
        var measurements = Setup(new Measurements(db, new MemberProgressTrackingService(db)), userId);
        Assert.IsType<ForbidResult>(await members.Index(null));
        Assert.IsType<ForbidResult>(await members.Details(own.Id));
        Assert.IsType<ForbidResult>(await measurements.SearchMembers("ışıl", default));
    }

    [Fact]
    public void Controllers_RequireTrainerRole()
    {
        foreach (var controller in new[] { typeof(Members), typeof(Measurements) })
            Assert.Contains(controller.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(),
                attribute => attribute.Roles == ApplicationRoles.Trainer);
    }

    private static T Setup<T>(T controller, string? userId) where T : Controller
    {
        var identity = userId is null ? new ClaimsIdentity() : new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Role, ApplicationRoles.Trainer)], "test");
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };
        return controller;
    }

    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<(Trainer, MemberProfile, MemberProfile, MemberProfile)> Seed(ApplicationDbContext db)
    {
        var coach = new Trainer { ApplicationUserId = "coach", FirstName = "Deneme", LastName = "Eğitmen" };
        var otherCoach = new Trainer { ApplicationUserId = "other-coach", FirstName = "Diğer", LastName = "Eğitmen" };
        var package = new MembershipPackage { Code = MembershipPackageCode.Pro, Name = "PRO", WeeklyClassLimit = 4 };
        MemberProfile Member(string login, string phone, Trainer? trainer) => new()
        {
            ApplicationUser = new ApplicationUser { UserName = login, FirstName = "Işıl", LastName = "Yılmaz", PhoneNumber = phone },
            MembershipPackage = package, AssignedTrainer = trainer, RemainingClassCredits = 17,
            NationalIdentityNumber = "10000000146"
        };
        var own = Member("own", "0531 111 22 33", coach);
        var other = Member("other", "0532 111 22 33", otherCoach);
        var unassigned = Member("unassigned", "0533 111 22 33", null);
        db.MemberProfiles.AddRange(own, other, unassigned);
        await db.SaveChangesAsync();
        return (coach, own, other, unassigned);
    }
}
