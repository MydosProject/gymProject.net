using Microsoft.AspNetCore.Mvc;

namespace NO23.Web.Controllers;

public class AboutController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
