using MedicalWarehouse_BusinessObject.Contract;
using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_Repository.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_Repository.Repositories
{
    public class ShipmentDetailRepository : BaseRepository<ShipmentDetail>, IShipmentDetailRepository
    {
        private readonly ApplicationDbContext _context;
        public ShipmentDetailRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<ShipmentDetail>> GetShipmentByMedicalId(List<Guid> ids)
        {
            return await _context.ShipmentDetails
                    .Where(sd => ids.Contains(sd.MedicalId))
                    .ToListAsync();
        }
    }
}
