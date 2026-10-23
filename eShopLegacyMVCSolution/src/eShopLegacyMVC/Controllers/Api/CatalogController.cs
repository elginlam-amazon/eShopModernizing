using Microsoft.AspNetCore.Mvc;

namespace eShopLegacyMVC.Controllers.Api
{
    // In ASP.NET MVC 5 this type was never discovered as a controller (its name does not end with "Controller"),
    // so GET /api was never served by it. ASP.NET Core discovers any Controller-derived type, so [NonController]
    // keeps the original (unexposed) behaviour.
    [NonController]
    [Route("api")]
    public class CatalogController2 : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            return Json(new { Message = "Hello World!" });
        }
    }
}
