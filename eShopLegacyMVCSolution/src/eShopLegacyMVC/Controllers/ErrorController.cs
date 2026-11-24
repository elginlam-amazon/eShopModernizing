using Microsoft.AspNetCore.Mvc;

namespace eShopLegacyMVC.Controllers
{
    /// <summary>
    /// Target of UseExceptionHandler("/Error"). Replaces the MVC 5 global HandleErrorAttribute,
    /// which rendered Views/Shared/Error.cshtml for unhandled exceptions.
    /// </summary>
    public class ErrorController : Controller
    {
        [Route("Error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public ActionResult Index()
        {
            return View("Error");
        }
    }
}
