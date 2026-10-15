using Microsoft.AspNetCore.Mvc;

namespace eShopLegacyMVC.Controllers
{
    /// <summary>
    /// Target of UseExceptionHandler("/Error"); replaces the MVC 5 global HandleErrorAttribute,
    /// which rendered Views/Shared/Error.cshtml for unhandled exceptions.
    /// </summary>
    public class ErrorController : Controller
    {
        [Route("Error")]
        public IActionResult Index()
        {
            return View("Error");
        }
    }
}
