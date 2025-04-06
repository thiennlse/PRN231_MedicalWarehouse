using FluentValidation;
using FluentValidation.Results;
using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Enums;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using System.Net;

namespace MedicalWarehouse_API.Controllers
{
    [Controller]
    [Route("area")]
    public class AreaController : Controller
    {
        private readonly IAreaService _areaService;
        private readonly IValidator<AreaRequestModel> _areaValidator;

        public AreaController(IAreaService areaService, IValidator<AreaRequestModel> areaValidator)
        {
            _areaService = areaService;
            _areaValidator = areaValidator;
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var listArea = await _areaService.GetAllArea();
                if (!listArea.Any())
                {
                    return Ok(new BaseResponse<AreaResponseModel>
                    {
                        Success = false,
                        Message = "No area found"
                    });
                }
                return Ok(new BaseResponse<AreaResponseModel>
                {
                    Success = true,
                    Results = listArea,
                    Message = "Retrived data successfully"
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
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "ADMIN,STAFF")]
        [Route("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var area = await _areaService.GetAreaByIdAsync(id);
                if (area == null)
                {
                    return Ok(new BaseResponse<AreaResponseModel>
                    {
                        Success = false,
                        Message = "No area found"
                    });
                }
                return Ok(new BaseResponse<AreaResponseModel>
                {
                    Success = true,
                    Result = area,
                    Message = "Retrived data successfully"
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
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "ADMIN,STAFF")]
        [Route("create")]
        public async Task<IActionResult> CreateArea([FromBody] AreaRequestModel request)
        {
            try
            {
                ValidationResult validationResult = await _areaValidator.ValidateAsync(request);
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
                var area = await _areaService.CreateAreaAsync(request);
                if (area == null)
                {
                    return Ok(new BaseResponse<AreaResponseModel>
                    {
                        Success = false,
                        Message = "Error when create a Area"
                    });
                }

                return Ok(new BaseResponse<AreaResponseModel>
                {
                    Success = true,
                    Result = area,
                    Message = "Create new area successfully"
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
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "ADMIN")]
        [Route("{id}")]
        public async Task<IActionResult> UpdateArea(Guid id, [FromBody] AreaRequestModel request)
        {
            try
            {
                ValidationResult validationResult = await _areaValidator.ValidateAsync(request);
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
                var area = await _areaService.UpdateAreaAsync(request, id);

                return Ok(new BaseResponse<AreaResponseModel>
                {
                    Success = true,
                    Message = "Area data has been updated",
                    Result = area
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

        [HttpDelete]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "ADMIN")]
        [Route("{id}")]
        public async Task<IActionResult> DeleteArea(Guid id)
        {
            try
            {
                await _areaService.DeleteAreaAsync(id);

                return Ok(new BaseResponse<AreaResponseModel>
                {
                    Success = true,
                    Message = "Area has been deleted successfully"
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
