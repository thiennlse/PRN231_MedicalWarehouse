using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace MedicalWarehouse_API.Controllers
{
    [ApiController]
    [Route("supplier")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "ADMIN,STAFF,PHARMACY")]
    public class SupplierController : Controller
    {
        private readonly ISupplierService _supplierService;

        public SupplierController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        [HttpGet]
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSupplierById(Guid id)
        {
            try
            {
                var response = await _supplierService.GetSupplierByIdAsync(id);
                if (!response.Success)
                {
                    return Ok(new BaseResponse<object>
                    {
                        Success = false,
                        Message = "No supplier found"
                    });
                }
                return Ok(response.Result);
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

        [HttpPost("create")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> CreateSupplier([FromBody] SupplierRequestModel supplier)
        {
            try
            {
                var response = await _supplierService.CreateSupplierAsync(supplier);

                return Ok(response);
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

        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> UpdateSupplier(Guid id, [FromBody] SupplierRequestModel supplier)
        {
            try
            {
                var response = await _supplierService.UpdateSupplierAsync(supplier, id);
                if (!response.Success)
                {
                    return BadRequest(response);
                }
                return Ok(response);
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

        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "ADMIN")]
        public async Task<IActionResult> DeleteSupplier(Guid id)
        {
            try
            {
                var response = await _supplierService.DeleteSupplierAsync(id);
                if (!response.Success)
                {
                    return BadRequest(response);
                }
                return Ok(response);
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