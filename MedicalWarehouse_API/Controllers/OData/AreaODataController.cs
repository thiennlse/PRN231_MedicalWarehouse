using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Net;

namespace MedicalWarehouse_API.Controllers.OData
{
    [Controller]
    [Route("odata/area")]
    public class AreaODataController : ODataController
    {
        private readonly IAreaService _areaService;

        public AreaODataController(IAreaService areaService)
        {
            _areaService = areaService;
        }

        [HttpGet]
        [EnableQuery]
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
    }
}
