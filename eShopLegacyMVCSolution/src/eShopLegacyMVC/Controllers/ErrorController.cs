using Microsoft.AspNetCore.Mvc;

namespace eShopLegacyMVC.Controllers
{
    /// <summary>
    /// Target of UseExceptionHandler; replaces the global HandleErrorAttribute that rendered Views/Shared/Error.cshtml.
    /// </summary>
    public class ErrorController : Controller
    {
        [Route("/Error")]
        public IActionResult Index()
        {
            return View("Error");
        }
    }
}
