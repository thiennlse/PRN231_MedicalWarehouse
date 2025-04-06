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
    public class AreaRepository : BaseRepository<Area>, IAreaRepository
    {
        private readonly ApplicationDbContext _context;

        public AreaRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Area>> GetAll()
        {
            var isAreaExist = await _context.Area
                .Include(a => a.Shipment)
                .ThenInclude(s => s.ShipmentDetails)
                .ThenInclude(sd => sd.Medical)
                .Where(a => a.IsDeleted.Value == false)
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();
            return isAreaExist;
        }

        public async Task<Area> GetAreaById(Guid areaId)
        {
            var isAreaExist = await _context.Area.Where(a => a.IsDeleted.Value == false)
                .FirstOrDefaultAsync(a => a.Id.Equals(areaId));
            return isAreaExist;
        }
    }
}
