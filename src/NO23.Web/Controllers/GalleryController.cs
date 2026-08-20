using Microsoft.AspNetCore.Mvc;

namespace NO23.Web.Controllers;

public class GalleryController : Controller
{
    public IActionResult Index() => View();
}
