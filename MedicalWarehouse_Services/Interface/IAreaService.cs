using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_Services.Interface
{
    public interface IAreaService
    {
        Task<List<AreaResponseModel>> GetAllArea();
        Task<AreaResponseModel> GetAreaByIdAsync(Guid areaId);
        Task<AreaResponseModel> CreateAreaAsync(AreaRequestModel request);
        Task<AreaResponseModel> UpdateAreaAsync(AreaRequestModel request, Guid areaId);
        Task DeleteAreaAsync(Guid areaId); 
    }
}
