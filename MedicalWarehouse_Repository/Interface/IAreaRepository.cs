﻿using MedicalWarehouse_BusinessObject.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_Repository.Interface
{
    public interface IAreaRepository : IBaseRepository<Area>
    {
        Task<List<Area>> GetAll();
        Task<Area> GetAreaById(Guid areaId);
    }
}
