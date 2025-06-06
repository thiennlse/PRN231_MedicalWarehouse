﻿using MedicalWarehouse_BusinessObject.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_Repository.Interface
{
    public interface IMedicalRepository : IBaseRepository<Medical>
    {
        Task<List<Medical>> GetAll();
        Task<Medical> GetMedicalById(Guid medicalId);
        Task<bool> ExistAsync(Guid medicalId);
    }
}
