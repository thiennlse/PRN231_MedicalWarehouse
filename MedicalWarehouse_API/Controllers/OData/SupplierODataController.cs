using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Net;

namespace MedicalWarehouse_API.Controllers.OData
{
    [ApiController]
    [Route("odata/supplier")]
    public class SupplierODataController : ODataController
    {
        private readonly ISupplierService _supplierService;

        public SupplierODataController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> GetAllSupplier()
        {
            try
            {
                var response = await _supplierService.GetAllSuppliersAsync();
                if (!response.Success)
                {
                    return Ok(new BaseResponse<object>
                    {
                        Success = false,
                        Message = "No suppliers found"
                    });
                }
                return Ok(response.Results);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new BaseResponse<object>
                {
                    Success = false,
                    Result = null,
                    Message = ex.Message
                });
            }
        }
    }
}
