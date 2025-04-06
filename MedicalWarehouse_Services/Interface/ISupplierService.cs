using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using System;
using System.Threading.Tasks;

namespace MedicalWarehouse_Services.Interface
{
    public interface ISupplierService
    {
        Task<BaseResponse<SupplierResponse>> GetAllSuppliersAsync();
        Task<BaseResponse<SupplierResponse>> GetSupplierByIdAsync(Guid id);
        Task<BaseResponse<SupplierResponse>> CreateSupplierAsync(SupplierRequestModel supplier);
        Task<BaseResponse<SupplierResponse>> UpdateSupplierAsync(SupplierRequestModel supplier, Guid supplierId);
        Task<BaseResponse<SupplierResponse>> DeleteSupplierAsync(Guid id);
        Task<List<ShipmentReponseModel>> GetShipmentBySupplierIdAsync(Guid id);
    }
}
