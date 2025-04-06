using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Services;
using MedicalWarehouse_Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;
using System.Net;

namespace MedicalWarehouse_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckOutController : Controller
    {
        private readonly PayOsService _payOsService;
        private readonly IOrderService _orderService;

        public CheckOutController(PayOsService payOsService, IOrderService orderService)
        {
            _payOsService = payOsService;
            _orderService = orderService;
        }

        [HttpPost("create-payment-link")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> Checkout([FromBody] CheckOutRequestModel paymentRequest)
        {
            try
            {
                var result = await _orderService.CreatePaymentUrlAsync(paymentRequest);
                if (result != null)
                {
                    return Ok(new BaseResponse<string>
                    {
                        Success = true,
                        Result = result,
                        Message = "Create checkout url successfully"
                    });
                }
                return Ok(new BaseResponse<string>
                {
                    Success = false,
                    Message = "Create checkout url failed"
                });
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
