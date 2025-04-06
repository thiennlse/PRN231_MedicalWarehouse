﻿using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_Repository.Interface
{
    public interface IAuthRepository
    {
        Task<User> GetById(string id);
        Task<List<UserResponseModel>> GetAll();
        Task Add(User user);
        Task Update(User user);
        Task Delete(string id);
    }
}
