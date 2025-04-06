using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_Repository.Interface
{
    public interface IDashboardRepository : IBaseRepository<Order>
    {
        Task<IQueryable<Order>> GetImportOrdersInWeekAsync(DateTime weekStart, DateTime weekEnd, OrderType type);
        Task<IQueryable<Order>> GetOrdersInYearAsync(DateTime startOfYear, DateTime endOfYear);
        Task<IQueryable<Order>> GetUpComingOrderAsync(DateTime date);
    }
}
