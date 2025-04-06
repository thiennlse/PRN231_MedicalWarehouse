using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Services.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Net;

namespace MedicalWarehouse_API.Controllers.OData
{
    [Controller]
    [Route("odata/medical")]
    public class MedicalODataController : ODataController
    {
        private readonly MedicalService _medicalService;

        public MedicalODataController(MedicalService medicalService)
        {
            _medicalService = medicalService;
        }

        [HttpGet]
        [EnableQuery]
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
    }
}
