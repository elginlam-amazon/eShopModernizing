using Microsoft.AspNetCore.Mvc;

namespace eShopLegacyMVC.Controllers
{
    /// <summary>
    /// Error page used by the exception handler middleware (replaces the global HandleErrorAttribute).
    /// </summary>
    public class ErrorController : Controller
    {
        [Route("Error")]
        public ActionResult Index()
        {
            return View("Error");
        }
    }
}
