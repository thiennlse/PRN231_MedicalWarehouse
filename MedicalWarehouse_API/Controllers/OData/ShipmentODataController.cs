using FluentValidation;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Net;

namespace MedicalWarehouse_API.Controllers.OData
{
    [Controller]
    [Route("odata/shipment")]
    public class ShipmentODataController : ODataController
    {
        private readonly IShipmentService _shipmentService;

        public ShipmentODataController(IShipmentService shipmentService)
        {
            _shipmentService = shipmentService;
        }

        [HttpGet]
        [EnableQuery]
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
    }
}
