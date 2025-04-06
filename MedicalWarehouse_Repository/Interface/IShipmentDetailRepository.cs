using MedicalWarehouse_BusinessObject.Entity;

namespace MedicalWarehouse_Repository.Interface
{
    public interface IShipmentDetailRepository : IBaseRepository<ShipmentDetail>
    {
        Task<List<ShipmentDetail>> GetShipmentByMedicalId(List<Guid> ids);
    }
}
