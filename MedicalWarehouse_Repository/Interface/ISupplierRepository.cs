using MedicalWarehouse_BusinessObject.Entity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MedicalWarehouse_Repository.Interface
{
    public interface ISupplierRepository : IBaseRepository<Supplier>
    {
        Task<IEnumerable<Supplier>> GetAllAsync();
        Task<Supplier> GetByIdAsync(Guid id);
        Task<bool> CheckPhoneNumberExistAsync(string phoneNumber);
        Task<bool> CheckEmailExistAsync(string email);
    }
}
