using Microsoft.AspNetCore.Mvc;

namespace ShopIt.Authentication.Presentation.Controllers;

public class HomeController : Controller
{
    [HttpGet("~/")]
    public IActionResult Index()
    {
        return View();
    }
}
