using AutoMapper;
using Azure.Core;
using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Repository.Interface;
using MedicalWarehouse_Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_Services.Services
{
    public class AreaService : IAreaService
    {
        private readonly IAreaRepository _repo;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;

        public AreaService(IAreaRepository repo, IHttpContextAccessor contextAccessor, UserManager<User> userManager, IMapper mapper)
        {
            _repo = repo;
            _contextAccessor = contextAccessor;
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<AreaResponseModel> CreateAreaAsync(AreaRequestModel request)
        {
            var currentUserName = _contextAccessor.HttpContext.User.FindFirst("name")?.Value;

            var area = new Area
            {
                Name = request.AreaName,
                CreateBy = currentUserName,
                CreatedDate = DateTime.Now.ToUniversalTime(),
                IsDeleted = false,
            };

            await _repo.AddAsync(area);

            return _mapper.Map<AreaResponseModel>(area);
        }

        public async Task DeleteAreaAsync(Guid areaId)
        {
            var currentUserName = _contextAccessor.HttpContext.User.FindFirst("name")?.Value;

            var isAreaExist = await _repo.GetAreaById(areaId);
            if (isAreaExist == null)
            {
                throw new Exception("Không tìm thấy khu vực");
            }
            isAreaExist.UpdateBy = currentUserName;
            isAreaExist.UpdatedDate = DateTime.Now.ToUniversalTime();

            await _repo.UpdateAsync(isAreaExist);
            await _repo.DeleteAsync(areaId);
        }

        public async Task<List<AreaResponseModel>> GetAllArea()
        {
            var listArea = await _repo.GetAll();
            return _mapper.Map<List<AreaResponseModel>>(listArea);
        }

        public async Task<AreaResponseModel> GetAreaByIdAsync(Guid areaId)
        {
            var area = await _repo.GetAreaById(areaId);
            return _mapper.Map<AreaResponseModel>(area);
        }

        public async Task<AreaResponseModel> UpdateAreaAsync(AreaRequestModel request, Guid areaId)
        {
            var currentUserName = _contextAccessor.HttpContext.User.FindFirst("name")?.Value;

            var isAreaExist = await _repo.GetAreaById(areaId);

            if (isAreaExist == null)
            {
                throw new Exception("Không tìm thấy khu vực");
            }

            isAreaExist.UpdateBy = currentUserName;
            isAreaExist.UpdatedDate = DateTime.Now.ToUniversalTime();
            isAreaExist.Name = request.AreaName;

            await _repo.UpdateAsync(isAreaExist);
            return _mapper.Map<AreaResponseModel>(isAreaExist);
        }
    }
}