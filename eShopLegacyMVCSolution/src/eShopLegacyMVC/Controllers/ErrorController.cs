using Microsoft.AspNetCore.Mvc;

namespace eShopLegacyMVC.Controllers
{
    /// <summary>
    /// Replaces the MVC 5 HandleErrorAttribute global filter: renders Views/Shared/Error.cshtml
    /// for unhandled exceptions (wired through UseExceptionHandler outside Development).
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
