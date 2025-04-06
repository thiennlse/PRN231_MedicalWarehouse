using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace MedicalWarehouse_API.Controllers
{
    [Controller]
    [Route("order")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
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

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    return Ok(new BaseResponse<OrderResponseModel>
                    {
                        Success = false,
                        Message = "Order not found"
                    });
                }
                return Ok(new BaseResponse<OrderResponseModel>
                {
                    Success = true,
                    Result = order,
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

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [Route("create")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequestModel request)
        {
            try
            {
                var order = await _orderService.CreateOrderAsync(request);
                if (order == null)
                {
                    return Ok(new BaseResponse<OrderResponseModel>
                    {
                        Success = false,
                        Message = "Error when creating an order"
                    });
                }
                return Ok(new BaseResponse<OrderResponseModel>
                {
                    Success = true,
                    Result = order,
                    Message = "Created new order successfully"
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

        [HttpPut]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "ADMIN,STAFF")]
        [Route("{id}")]
        public async Task<IActionResult> UpdateOrder(Guid id, [FromBody] OrderRequestModel request)
        {
            try
            {
                var order = await _orderService.UpdateOrderAsync(request, id);
                return Ok(new BaseResponse<OrderResponseModel>
                {
                    Success = true,
                    Message = "Order data has been updated",
                    Result = order
                });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Available quantity exceeded"))
                {
                    return BadRequest(new { success = false, message = ex.Message });
                }
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "ADMIN,STAFF")]
        [Route("{id}")]
        public async Task<IActionResult> DeleteOrder(Guid id)
        {
            try
            {
                await _orderService.DeleteOrderAsync(id);
                return Ok(new BaseResponse<OrderResponseModel>
                {
                    Success = true,
                    Message = "Order has been deleted successfully"
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
