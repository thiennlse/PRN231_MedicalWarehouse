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
    public class MedicalRepository : BaseRepository<Medical>, IMedicalRepository
    {
        private readonly ApplicationDbContext _context;

        public MedicalRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> ExistAsync(Guid medicalId)
        {
            return await _context.Medicals.AnyAsync(m => m.Id == medicalId);
        }

        public async Task<List<Medical>> GetAll()
        {
            var medicals = await _context.Medicals
                .Where(a => a.IsDeleted.Value == false)
                .Include(a => a.Supplier)
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();
            return medicals;
        }

        public async Task<Medical> GetMedicalById(Guid medicalId)
        {
            var medical = await _context.Medicals
                .Where(a => a.IsDeleted.Value == false)
                .Include(a => a.Supplier)
                .FirstOrDefaultAsync(a => a.Id.Equals(medicalId));
            return medical;
        }
    }
}
