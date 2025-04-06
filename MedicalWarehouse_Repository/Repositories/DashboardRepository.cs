using MedicalWarehouse_BusinessObject.Contract;
using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Enums;
using MedicalWarehouse_Repository.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_Repository.Repositories
{
    public class DashboardRepository : BaseRepository<Order>, IDashboardRepository
    {
        private readonly ApplicationDbContext _context;

        public DashboardRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IQueryable<Order>> GetImportOrdersInWeekAsync(DateTime weekStart, DateTime weekEnd, OrderType type)
        {
            try
            {
                return _context

                    .Orders.Include(p => p.OrderDetails)
                    .Where(p => p.OrderDate >= weekStart 
                        && p.OrderDate <= weekEnd 
                        && p.Type == type
                        && p.IsDeleted == false).OrderBy(p => p.OrderDate);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<IQueryable<Order>> GetOrdersInYearAsync(DateTime startOfYear, DateTime endOfYear)
        {
            try
            {
                return _context
                    .Orders
                    .Include(p => p.OrderDetails)
                    .Where(p => p.OrderDate >= startOfYear
                        && p.OrderDate <= endOfYear
                        && p.IsDeleted == false)
                    .OrderBy(p => p.OrderDate);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<IQueryable<Order>> GetUpComingOrderAsync(DateTime date)
        {
            try
            {
                return _context
                    .Orders
                    .Include(p => p.OrderDetails)
                    .Include(p => p.User)
                    .Where(p => p.OrderDate > date
                        && p.IsDeleted == false
                        && p.Type == OrderType.Import)
                    .OrderBy(p => p.OrderDate);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}