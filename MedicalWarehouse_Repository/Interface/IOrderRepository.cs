using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_Repository.Interface
{
    public interface IOrderRepository : IBaseRepository<Order>
    {
        Task<Order> GetById(Guid id);
        Task<List<OrderResponseModel>> GetAll();
    }
}
