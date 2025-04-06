using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Net;

namespace MedicalWarehouse_API.Controllers.OData
{
    [Controller]
    [Route("odata/order")]
    public class OrderODataController : ODataController
    {
        private readonly IOrderService _orderService;

        public OrderODataController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var listOrder = await _orderService.GetAllOrder();
                if (listOrder == null)
                {
                    return Ok(new BaseResponse<OrderResponseModel>
                    {
                        Success = false,
                        Message = "No orders found"
                    });
                }
                return Ok(new BaseResponse<OrderResponseModel>
                {
                    Success = true,
                    Results = listOrder,
                    Message = "Retrieved data successfully"
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
