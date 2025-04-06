using AutoMapper;
using MedicalWarehouse_BusinessObject.Contract;
using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Repository.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_Repository.Repositories
{
    public class OrderRepository : BaseRepository<Order>, IOrderRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        public OrderRepository(ApplicationDbContext context, IMapper mapper) : base(context)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<List<OrderResponseModel>> GetAll()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Shipments)
                        .ThenInclude(s => s.ShipmentDetails)
                            .ThenInclude(sd => sd.Medical)
                .Where(o => o.IsDeleted == false || o.IsDeleted == null)
                .AsNoTracking()
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            return _mapper.Map<List<OrderResponseModel>>(orders);
        }

        public async Task<Order> GetById(Guid id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Shipments)
                        .ThenInclude(s => s.ShipmentDetails)
                            .ThenInclude(sd => sd.Medical)
                .Where(o => o.IsDeleted == false || o.IsDeleted == null)
                .FirstOrDefaultAsync(o => o.Id.Equals(id));

            return order;
        }
    }
}

