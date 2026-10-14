using Microsoft.AspNetCore.Mvc;

namespace eShopLegacyMVC.Controllers
{
    /// <summary>
    /// Renders Views/Shared/Error.cshtml for unhandled exceptions outside Development
    /// (replaces the global MVC 5 HandleErrorAttribute filter).
    /// </summary>
    public class ErrorController : Controller
    {
        [Route("Error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index()
        {
            return View("Error");
        }
    }
}
