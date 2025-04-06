using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_Services.Interface
{
    public interface IMedicalService
    {
        Task<List<MedicalResponseModel>> GetAllMedicals();
        Task<MedicalResponseModel> GetById(Guid medicalId);
        Task<MedicalResponseModel> CreateMedicalAsync(MedicalRequestModel request);
        Task<MedicalResponseModel> UpdateAsync(Guid id, MedicalRequestModel request);
        Task<bool> UpdateDeleteAsync(Guid id);
        Task<List<ShipmentReponseModel>> GetShipmentDetailsByMedicalId(Guid medicalId);

    }
}
