using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_Services.Interface
{
    public interface IShipmentService
    {
        Task<List<ShipmentReponseModel>> GetAll();
        Task<ShipmentReponseModel> GetById(Guid id);
        Task<ShipmentReponseModel> Add(ShipmentRequestModel model);
        Task<ShipmentReponseModel> Update(Guid shipmentId, ShipmentRequestModel model);
        Task DeleteById(Guid shipmentId);
    }
}
