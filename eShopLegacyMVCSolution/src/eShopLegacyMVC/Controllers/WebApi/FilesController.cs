using eShopLegacy.Utilities;
using eShopLegacyMVC.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace eShopLegacyMVC.Controllers.WebApi
{
    // Previously routed by WebApiConfig convention "api/{controller}/{id}"
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private ICatalogService _service;

        public FilesController(ICatalogService service)
        {
            _service = service;
        }

        // GET api/<controller>
        [HttpGet]
        public IActionResult Get()
        {
            var brands = _service.GetCatalogBrands()
                .Select(b => new BrandDTO
                {
                    Id = b.Id,
                    Brand = b.Brand
                }).ToList();
            var serializer = new Serializing();

            // Serializing now emits JSON (BinaryFormatter was removed from .NET 9+)
            return File(serializer.SerializeBinary(brands), "application/json");
        }

        public class BrandDTO
        {
            public int Id { get; set; }
            public string Brand { get; set; }
        }
    }
}
