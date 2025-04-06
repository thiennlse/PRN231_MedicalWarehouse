using FluentValidation;
using FluentValidation.Results;
using MedicalWarehouse_BusinessObject.Enums;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using System.Net;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace MedicalWarehouse_API.Controllers
{
    [Controller]
    [Route("shipment")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "ADMIN,STAFF")]
    public class ShipmentController : Controller
    {
        private readonly IShipmentService _shipmentService;
        private readonly IValidator<ShipmentRequestModel> _shipmentValidator;

        public ShipmentController(IShipmentService shipmentService, IValidator<ShipmentRequestModel> shipmentValidator)
        {
            _shipmentService = shipmentService;
            _shipmentValidator = shipmentValidator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var shipmentList = await _shipmentService.GetAll();
                if (!shipmentList.Any())
                {
                    return Ok(new BaseResponse<ShipmentReponseModel>
                    {
                        Success = false,
                        Message = "No Shipment found"
                    });
                }
                return Ok(new BaseResponse<ShipmentReponseModel>
                {
                    Success = true,
                    Results = shipmentList,
                    Message = "Get all shipments successful"
                });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new BaseResponse<ShipmentReponseModel>
                {
                    Success = false,
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
                var shipment = await _shipmentService.GetById(id);
                if (shipment == null)
                {
                    return Ok(new BaseResponse<ShipmentReponseModel>
                    {
                        Success = false,
                        Message = "No Shipment found"
                    });
                }
                return Ok(new BaseResponse<ShipmentReponseModel>
                {
                    Success = true,
                    Result = shipment,
                    Message = "Get all shipments successful"
                });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new BaseResponse<ShipmentReponseModel>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "ADMIN,STAFF")]
        [Route("create")]
        public async Task<IActionResult> CreateShipment([FromBody] ShipmentRequestModel model)
        {
            try
            {
                ValidationResult validationResult = await _shipmentValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    var error = validationResult.Errors.Select(e => (object)new
                    {
                        e.PropertyName,
                        e.ErrorMessage
                    }).ToList();

                    return BadRequest(new BaseResponse<object>
                    {
                        Success = false,
                        Results = error
                    });
                }
                var shipment = await _shipmentService.Add(model);
                if (model == null)
                {
                    return Ok(new BaseResponse<ShipmentReponseModel>
                    {
                        Success = false,
                        Message = "Error when create a Shipment"
                    });
                }
                return Ok(new BaseResponse<ShipmentReponseModel>
                {
                    Success = true,
                    Result = shipment,
                    Message = "Created successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new BaseResponse<ShipmentReponseModel>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpPut]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "ADMIN,STAFF")]
        [Route("{id}")]
        public async Task<IActionResult> UpdateShipment(Guid id, [FromBody] ShipmentRequestModel model)
        {
            try
            {
                ValidationResult validationResult = await _shipmentValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    var error = validationResult.Errors.Select(e => (object)new
                    {
                        e.PropertyName,
                        e.ErrorMessage
                    }).ToList();

                    return BadRequest(new BaseResponse<object>
                    {
                        Success = false,
                        Results = error
                    });
                }
                var shipment = await _shipmentService.GetById(id);
                if (shipment == null)
                {
                    return Ok(new BaseResponse<ShipmentReponseModel>
                    {
                        Success = false,
                        Message = "No Shipment found"
                    });
                }

                var result = await _shipmentService.Update(id, model);
                return Ok(new BaseResponse<ShipmentReponseModel>
                {
                    Success = true,
                    Result = result,
                    Message = "Updated successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new BaseResponse<ShipmentReponseModel>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpDelete]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "ADMIN,STAFF")]
        [Route("{id}")]
        public async Task<IActionResult> DeleteShipment(Guid id)
        {
            try
            {
                var shipment = await _shipmentService.GetById(id);
                if (shipment == null)
                {
                    return Ok(new BaseResponse<ShipmentReponseModel>
                    {
                        Success = false,
                        Message = "No Shipment found"
                    });
                }

                await _shipmentService.DeleteById(id);
                return Ok(new BaseResponse<ShipmentReponseModel>
                {
                    Success = true,
                    Message = "Deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new BaseResponse<ShipmentReponseModel>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }
    }
}
