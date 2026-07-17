using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Models;
using NO23.Web.ViewModels.Home;

namespace NO23.Web.Controllers;

public class HomeController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var packages = await dbContext.MembershipPackages
            .AsNoTracking()
            .Where(package => package.IsActive)
            .OrderBy(package => package.DisplayOrder)
            .ToListAsync();

        var packageViewModels = packages
            .Select(package => new MembershipPackageSummaryViewModel
            {
                Code = package.Code.ToString().ToUpperInvariant(),
                Name = package.Name,
                Audience = package.Audience,
                DisplayOrder = package.DisplayOrder,
                Features = BuildPackageFeatures(package)
            })
            .ToList();

        return View(new HomeIndexViewModel
        {
            MembershipPackages = packageViewModels
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private static string[] BuildPackageFeatures(MembershipPackage package)
    {
        var features = new List<string>();

        features.Add(package.WeeklyClassLimit.HasValue
            ? $"Haftada {package.WeeklyClassLimit.Value} ders"
            : "Sınırsız grup dersleri");

        if (package.IncludesMeasurement)
        {
            features.Add("Başlangıç ölçümü");
        }

        if (package.IncludesBodyAnalysis)
        {
            features.Add("Vücut analizi");
        }

        if (package.IncludesNutritionSupport)
        {
            features.Add("Beslenme önerisi");
        }

        if (package.IncludesDetailedTracking)
        {
            features.Add("Detaylı gelişim takibi");
        }

        if (package.IncludesMonthlyAnalysis)
        {
            features.Add("Aylık performans analizi");
        }

        if (package.IncludesPriorityReservation)
        {
            features.Add("Öncelikli rezervasyon");
        }

        if (package.IncludesPersonalTrainingSupport)
        {
            features.Add("Personal Training desteği");
        }

        if (package.IncludesKitchenBenefits)
        {
            features.Add("NO23 Kitchen avantajları");
        }

        if (package.IncludesPrivateEvents)
        {
            features.Add("Özel etkinlik davetleri");
        }

        if (package.IncludesCommunityMembership)
        {
            features.Add("Community üyeliği");
        }

        return [.. features];
    }
}
