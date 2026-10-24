using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Infrastructure;
using NO23.Web.Services;
using NO23.Web.ViewModels;
using NO23.Web.ViewModels.Admin;
using NO23.Web.ViewModels.TrainerPanel;

namespace NO23.Tests;

public class CalendarNavigationTests
{
    [Theory]
    [InlineData("day",1)]
    [InlineData("week",7)]
    public async Task AdminAndTrainer_FocusTheRequestedDayAndPreserveTheSameCreditSummary(string mode, int days)
    {
        await using var db = Db();
        var date = new DateTime(2026,10,21);
        var coach = new Trainer { ApplicationUserId = "coach", FirstName = "QA" };
        var member = new MemberProfile { ApplicationUser = new ApplicationUser { FirstName = "Deneme", LastName = "Üye" },
            MembershipPackage = new MembershipPackage { WeeklyClassLimit = 3 }, AssignedTrainer = coach, RemainingClassCredits = 10 };
        db.PersonalTrainingSessions.AddRange(
            new PersonalTrainingSession { MemberProfile = member, Trainer = coach, StartsAtUtc = ClubTime.ToUtc(date.AddHours(10)) },
            new PersonalTrainingSession { MemberProfile = member, Trainer = coach, StartsAtUtc = ClubTime.ToUtc(date.AddDays(1).AddHours(9)) });
        await db.SaveChangesAsync();
        var service = new PersonalTrainingCalendarService(db);
        var admin = new NO23.Web.Areas.Admin.Controllers.CalendarController(db, service, new ClassReservationService(db));
        var adminModel = Assert.IsType<WeeklyCalendarViewModel>(Assert.IsType<ViewResult>(await admin.Index(null,null, member.Id, view:mode,date:date)).Model);
        Assert.Equal(days, adminModel.DisplayDays.Count());
        Assert.Equal(date, adminModel.SelectedDate);
        Assert.Equal(10, adminModel.Credits!.Remaining);
        Assert.Equal(8, adminModel.Credits.Available);
        Assert.Equal(mode, Assert.IsType<CalendarNavigationViewModel>(admin.ViewData["CalendarNavigation"]).Mode);
        var trainer = new NO23.Web.Areas.Trainer.Controllers.CalendarController(db, service)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,"coach")],"test")) } } };
        var trainerModel = Assert.IsType<TrainerCalendarViewModel>(Assert.IsType<ViewResult>(await trainer.Index(null,member.Id,view:mode,date:date)).Model);
        Assert.Equal(days, trainerModel.VisibleDays.Count());
        Assert.Equal(mode == "day" ? 1 : 2, trainerModel.VisibleSessions.Count);
        Assert.Equal(adminModel.Credits.Remaining,trainerModel.Credits!.Remaining);
        Assert.Equal(adminModel.Credits.Available,trainerModel.Credits.Available);
        Assert.All(trainerModel.Sessions, x => Assert.Equal(10,x.RemainingCredits));
        var next = Assert.IsType<WeeklyCalendarViewModel>(Assert.IsType<ViewResult>(await admin.Index(date.AddDays(7),null,member.Id)).Model);
        Assert.Equal(adminModel.Credits.Remaining,next.Credits!.Remaining);
        Assert.Equal(adminModel.Credits.Available,next.Credits.Available);
        Assert.Equal(10, member.RemainingClassCredits);
        var foreign = new MemberProfile { ApplicationUser = new ApplicationUser { FirstName = "Foreign" }, MembershipPackage = member.MembershipPackage, RemainingClassCredits = 99 };
        db.MemberProfiles.Add(foreign); await db.SaveChangesAsync();
        var forbidden = Assert.IsType<TrainerCalendarViewModel>(Assert.IsType<ViewResult>(await trainer.Index(date,foreign.Id,view:mode)).Model);
        Assert.Null(forbidden.Credits);
        Assert.Empty(forbidden.Sessions);
    }

    [Theory]
    [InlineData("day",1)]
    [InlineData("week",7)]
    public void PreviousAndNextNavigation_MoveByTheSelectedView(string mode,int offset)
    {
        var date = new DateTime(2026,12,31);
        var model = new CalendarNavigationViewModel { Date = date, Mode = mode, MemberId = 5, IncludeCompleted = true };
        Assert.Equal(date.AddDays(-offset),model.Previous);
        Assert.Equal(date.AddDays(offset),model.Next);
        Assert.Contains("202",model.Title);
    }

    [Theory]
    [InlineData("day","2026-10-21",true)]
    [InlineData("week","2026-10-21",true)]
    [InlineData("evil","2026-10-21",false)]
    [InlineData("day","https://example.test",false)]
    [InlineData("day","9999-01-01",false)]
    public void PostRedirect_KeepsViewDateAndExistingFiltersOnlyForValidContext(string mode,string date,bool accepted)
    {
        var http = new DefaultHttpContext();
        http.Request.Method = "POST"; http.Request.ContentType = "application/x-www-form-urlencoded";
        http.Request.Form = new FormCollection(new Dictionary<string,StringValues> { ["CalendarView"]=mode,["CalendarDate"]=date });
        var route = new RouteData(); route.Values["controller"]="Calendar"; route.Values["area"]="Admin";
        var context = new ActionExecutedContext(new ActionContext(http,route,new ActionDescriptor()),[],new object());
        var redirect = new RedirectToActionResult("Index",null,new { memberId=7,trainerId=3,includeCompleted=true });
        context.Result = redirect;
        new CalendarViewContextAttribute().OnActionExecuted(context);
        Assert.Equal(accepted,redirect.RouteValues!.ContainsKey("view"));
        Assert.Equal(7,redirect.RouteValues["memberId"]);
        if(accepted) { Assert.Equal(mode,redirect.RouteValues["view"]); Assert.Equal(date,redirect.RouteValues["date"]); }
    }

    [Fact]
    public void SharedCalendarPostForms_ReceiveContextWithoutRemovingAntiforgeryContent()
    {
        var helper = new CalendarContextTagHelper { ViewContext = new ViewContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(),new ModelStateDictionary()) {
            ["CalendarNavigation"] = new CalendarNavigationViewModel { Date = new DateTime(2026,10,21),Mode="day" } } } };
        var output = new TagHelperOutput("form",new TagHelperAttributeList { {"method","post"} },(cache,encoder)=>Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        output.PostContent.AppendHtml("<input name=\"__RequestVerificationToken\" value=\"qa\" />");
        helper.Process(new TagHelperContext(new TagHelperAttributeList(),new Dictionary<object,object>(),"qa"),output);
        var html = output.PostContent.GetContent();
        Assert.Contains("CalendarView",html); Assert.Contains("CalendarDate",html); Assert.Contains("2026-10-21",html);
        Assert.Contains("__RequestVerificationToken",html);
    }

    [Theory]
    [InlineData("invalid",2026)]
    [InlineData("week",9999)]
    public async Task InvalidViewOrDate_IsRejectedBeforeCalendarQueries(string mode,int year)
    {
        await using var db=Db();
        var controller = new NO23.Web.Areas.Admin.Controllers.CalendarController(db,new(db),new(db));
        Assert.IsType<BadRequestResult>(await controller.Index(null,null,view:mode,date:new DateTime(year,1,1)));
    }

    private static ApplicationDbContext Db()=>new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
