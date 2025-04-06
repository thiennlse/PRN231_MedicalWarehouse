using MedicalWarehouse_BusinessObject.Enums;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Services.Interface;
using MedicalWarehouse_Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Identity.Client;
using System.Net;

namespace MedicalWarehouse_API.Controllers
{
    [Route("[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "ADMIN,STAFF")]
    [ApiController]
    public class DashboardController : ODataController
    {
        private readonly IDashboardService _service;

        public DashboardController(IDashboardService service)
        {
            _service = service;
        }

        [HttpGet("get")]
        //[Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetColumnChartAsync(DateTime date, OrderType type)
        {
            var result = await _service.GetColumnChartAsync(date, type);
            return Ok(result);
        }
        [HttpGet("get-by-year")]
        //[Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetColumnChartAsync(int year)
        {
            var result = await _service.GetColumnChartAsync(year);
            return Ok(result);
        }
        [HttpGet("product-sale")]
        //[Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetAllSupplier(int year)
        {
            var result = await _service.GetProductSaleAsync(year);
            return Ok(result);
        }

        [HttpGet("up-coming")]
        //[Authorize(AuthenticationSchemes = "Bearer")]
        [EnableQuery]
        public async Task<IActionResult> GetAllUpComing()
        {
            var result = await _service.GetAllUpComing();
            return Ok(result);
        }
    }
}
