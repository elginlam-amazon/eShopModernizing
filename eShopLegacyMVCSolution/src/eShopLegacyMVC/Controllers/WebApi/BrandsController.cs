using eShopLegacy.Utilities;
using eShopLegacyMVC.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace eShopLegacyMVC.Controllers.WebApi
{
    [ApiController]
    [Route("api/brands")]
    public class BrandsController : ControllerBase
    {
        private readonly ICatalogService _service;

        public BrandsController(ICatalogService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var brands = _service.GetCatalogBrands();
            return Ok(brands);
        }

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var brands = _service.GetCatalogBrands();
            var brand = brands.FirstOrDefault(x => x.Id == id);
            if (brand == null) return NotFound();

            return Ok(brand);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var brandToDelete = _service.GetCatalogBrands().FirstOrDefault(x => x.Id == id);
            if (brandToDelete == null)
            {
                return NotFound();
            }

            // demo only - don't actually delete
            return Ok();
        }
    }
}
