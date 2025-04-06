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
    public class SupplierRepository : BaseRepository<Supplier>, ISupplierRepository
    {
        private readonly ApplicationDbContext _context;

        public SupplierRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Supplier>> GetAllAsync()
        {
            return await _context.Suppliers
                .Include(s => s.Shipments)
                .ThenInclude(s => s.ShipmentDetails)
                .ThenInclude(sd => sd.Medical)
                .Where(s => s.IsDeleted == false)
                .ToListAsync();
        }

        public async Task<Supplier?> GetByIdAsync(Guid id)
        {
            return await _context.Suppliers.Include(s => s.Shipments).FirstOrDefaultAsync(s => s.Id == id && s.IsDeleted == false);
        }

        public async Task<bool> CheckPhoneNumberExistAsync(string phoneNumber)
        {
            return await _context.Suppliers
                .AnyAsync(s => s.PhoneNumber == phoneNumber && s.IsDeleted == false);
        }

        public async Task<bool> CheckEmailExistAsync(string email)
        {
            return await _context.Suppliers
                .AnyAsync(s => s.ContactEmail == email && s.IsDeleted == false);
        }
    }
}
