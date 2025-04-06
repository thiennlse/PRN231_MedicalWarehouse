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
    public class AuthRepository : IAuthRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public AuthRepository(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task Add(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(string id)
        {
            var user = await GetById(id);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        public async Task<List<UserResponseModel>> GetAll()
        {
            var listUser = await _context.Users.ToListAsync();
            return _mapper.Map<List<UserResponseModel>>(listUser);
        }

        public async Task<User> GetById(string id)
        {
            var user = await _context.Users.FindAsync(id);
            return user;
        }

        public async Task Update(User user)
        {
            _context.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
