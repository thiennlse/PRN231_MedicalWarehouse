using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_Repository.Interface
{
    public interface IShipmentRepository : IBaseRepository<Shipment>
    {
        Task<List<ShipmentDetail>> GetShipmentDetailsByMedicalId(Guid medicalId);
        Task<List<ShipmentReponseModel>> GetAll();
        Task<Shipment> GetShipmentById(Guid id);
        Task<IQueryable<Shipment>> GetShipmentBySupplierID(Guid supplierID);
    }
}
