using Azure.Core;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Services.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using MedicalWarehouse_BusinessObject.Entity;


namespace MedicalWarehouse_API.Controllers
{
    [Controller]
    [Route("medical")]
    public class MedicalController : ControllerBase
    {
        private readonly MedicalService _medicalService;

        public MedicalController(MedicalService medicalService)
        {
            _medicalService = medicalService;
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "ADMIN,STAFF")]
        [Route("create")]
        public async Task<IActionResult> CreateMedical([FromBody] MedicalRequestModel request)
        {
            try
            {
                var medical = await _medicalService.CreateMedicalAsync(request);
                if (medical == null)
                {
                    return Ok(new BaseResponse<MedicalResponseModel>
                    {
                        Success = false,
                        Message = "Error when create a Medical"
                    });
                }

                return Ok(new BaseResponse<MedicalResponseModel>
                {
                    Success = true,
                    Result = medical,
                    Message = "Create new medical successfully"
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
        public async Task<IActionResult> GetAll()
        {
            try
            {
                List<MedicalResponseModel> medicals = await _medicalService.GetAllMedicals();
                if (!medicals.Any())
                {
                    return Ok(new BaseResponse<MedicalResponseModel>
                    {
                        Success = false,
                        Message = "No data found"
                    });
                }
                return Ok(new BaseResponse<MedicalResponseModel>
                {
                    Success = true,
                    Results = medicals,
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMedicalById(Guid id)
        {
            try
            {
                var medical = await _medicalService.GetById(id);
                if (medical == null)
                {
                    return Ok(new BaseResponse<MedicalResponseModel>
                    {
                        Success = false,
                        Message = "No medical found"
                    });
                }
                return Ok(new BaseResponse<MedicalResponseModel>
                {
                    Success = true,
                    Result = medical,
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

        [HttpGet("shipment/{id}")]
        public async Task<IActionResult> GetShipmentsByMedicalId(Guid id)
        {
            try
            {
                var medical = await _medicalService.GetShipmentDetailsByMedicalId(id);
                if (medical == null)
                {
                    return Ok(new BaseResponse<ShipmentReponseModel>
                    {
                        Success = false,
                        Message = "No medical found"
                    });
                }
                return Ok(new BaseResponse<ShipmentReponseModel>
                {
                    Success = true,
                    Results = medical,
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

        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "ADMIN")]
        public async Task<IActionResult> UpdateMedical(Guid id, [FromBody] MedicalRequestModel request)
        {
            try
            {
                var medical = await _medicalService.UpdateAsync(id, request);

                return Ok(new BaseResponse<MedicalResponseModel>
                {
                    Success = true,
                    Message = "Medical data has been updated",
                    Result = medical
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

        [HttpDelete("delete/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "ADMIN")]
        public async Task<IActionResult> DeleteMedical(Guid id)
        {
            try
            {
                await _medicalService.UpdateDeleteAsync(id);

                return Ok(new BaseResponse<MedicalResponseModel>
                {
                    Success = true,
                    Message = "Medical has been deleted successfully"
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
