using AutoMapper;
using MedicalWarehouse_BusinessObject.Contract;
using MedicalWarehouse_BusinessObject.Entity;
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
    public class ShipmentRepository : BaseRepository<Shipment>, IShipmentRepository
    {

        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        public ShipmentRepository(ApplicationDbContext context, IMapper mapper) : base(context)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<ShipmentReponseModel>> GetAll()
        {
            var shipments = await _context.Shipments
                .Include(s => s.ShipmentDetails)
                .ThenInclude(sd => sd.Medical)
                .Where(s => s.IsDeleted == false)
                .Where(s => s.ShipmentDetails.Any(sd => sd.Quantity > 0))
                .AsNoTracking()
                .ToListAsync();
            return _mapper.Map<List<ShipmentReponseModel>>(shipments);
        }

        public async Task<Shipment> GetShipmentById(Guid id)
        {
            var shipment = await _context.Shipments
                .Include(s => s.ShipmentDetails)
                .ThenInclude(sd => sd.Medical)
                .Where(s => s.IsDeleted == false)
                .FirstOrDefaultAsync(s => s.Id.Equals(id));
            return shipment;
        }

        public async Task<IQueryable<Shipment>> GetShipmentBySupplierID(Guid supplierID)
        {
            try
            {
                return _context.Shipments
                .Where(p => p.SupplierId == supplierID && p.IsDeleted == false);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<List<ShipmentDetail>> GetShipmentDetailsByMedicalId(Guid medicalId)
        {
            var shipmentDetails = await _context.ShipmentDetails
                  .Where(sd => sd.MedicalId == medicalId)
                  .Include(sd => sd.Shipment)
                  .Include(sd => sd.Medical)
                  .ToListAsync();

            return shipmentDetails;
        }
    }
}
