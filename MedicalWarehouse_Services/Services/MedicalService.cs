using AutoMapper;
using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Repository.Interface;
using MedicalWarehouse_Services.Interface;
using MedicalWarehouse_Validations.Medical;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MedicalWarehouse_Services.Services
{
    public class MedicalService : IMedicalService
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IMedicalRepository _medicalRepository;
        private readonly IMapper _mapper;
        private readonly CloudinaryService _cloudinaryService;
        private readonly UserManager<User> _userManager;
        private readonly IShipmentRepository _shipmentRepository;
        private readonly IShipmentDetailRepository _shipmentDetailRepository;

        public MedicalService(
            IMedicalRepository medicalRepository,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            UserManager<User> userManager,
            CloudinaryService cloudinaryService,
            IShipmentRepository shipmentRepository,
            IShipmentDetailRepository shipmentDetailRepository)
        {
            _contextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _medicalRepository = medicalRepository ?? throw new ArgumentNullException(nameof(medicalRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _cloudinaryService = cloudinaryService ?? throw new ArgumentNullException(nameof(cloudinaryService));
            _shipmentRepository = shipmentRepository ?? throw new ArgumentNullException(nameof(shipmentRepository));
            _shipmentDetailRepository = shipmentDetailRepository ?? throw new ArgumentNullException(nameof(shipmentDetailRepository));
        }

        public async Task<List<MedicalResponseModel>> GetAllMedicals()
        {
            var medicals = await _medicalRepository.GetAll() ?? throw new KeyNotFoundException("Không tìm thấy hồ sơ y tế.");
            var medicalIds = medicals.Select(m => m.Id).ToList();
            var shipmentDetails = await _shipmentDetailRepository.GetShipmentByMedicalId(medicalIds);

            return medicals.Select(medical =>
            {
                var response = _mapper.Map<MedicalResponseModel>(medical);
                response.Quantity = shipmentDetails
                    .Where(sd => sd.MedicalId == medical.Id)
                    .Sum(sd => sd.Quantity);
                return response;
            }).ToList();
        }

        public async Task<MedicalResponseModel> GetById(Guid medicalId)
        {
            var medical = await _medicalRepository.GetMedicalById(medicalId)
                ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ y tế với ID {medicalId}.");

            var response = _mapper.Map<MedicalResponseModel>(medical);
            var shipmentDetails = await _shipmentRepository.GetShipmentDetailsByMedicalId(medicalId);
            response.Quantity = shipmentDetails?.Sum(sd => sd.Quantity) ?? 0;

            return response;
        }

        public async Task<MedicalResponseModel> CreateMedicalAsync(MedicalRequestModel request)
        {
            await ValidateRequestAsync(request);
            var medical = _mapper.Map<Medical>(request);
            medical.CreatedDate = DateTime.UtcNow;
            medical.CreateBy = GetCurrentUserName();
            medical.IsDeleted = false;

            await _medicalRepository.AddAsync(medical);
            return _mapper.Map<MedicalResponseModel>(medical);
        }

        public async Task<MedicalResponseModel> UpdateAsync(Guid id, MedicalRequestModel request)
        {
            var medical = await _medicalRepository.GetMedicalById(id)
                ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ y tế với ID {id}.");

            await ValidateRequestAsync(request);
            _mapper.Map(request, medical);
            medical.UpdatedDate = DateTime.UtcNow;
            medical.UpdateBy = GetCurrentUserName();

            await _medicalRepository.UpdateAsync(medical);
            return _mapper.Map<MedicalResponseModel>(medical);
        }

        public async Task<bool> UpdateDeleteAsync(Guid id)
        {
            var medical = await _medicalRepository.GetMedicalById(id)
                ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ y tế với ID {id}.");

            medical.IsDeleted = true;
            medical.UpdatedDate = DateTime.UtcNow;
            medical.UpdateBy = GetCurrentUserName();

            await _medicalRepository.UpdateAsync(medical);
            return true;
        }

        public async Task<List<ShipmentReponseModel>> GetShipmentDetailsByMedicalId(Guid medicalId)
        {
            if (medicalId == Guid.Empty)
            {
                throw new ArgumentException("medicalId không hợp lệ.", nameof(medicalId));
            }

            var medical = await _medicalRepository.GetMedicalById(medicalId)
                ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ y tế với ID {medicalId}.");

            // Lấy danh sách ShipmentDetail
            var shipmentDetails = await _shipmentRepository.GetShipmentDetailsByMedicalId(medicalId)
                ?? throw new KeyNotFoundException("Không tìm thấy lô thuốc phù hợp.");
            
            // Nhóm các ShipmentDetail theo Shipment
            var shipmentGroups = shipmentDetails.GroupBy(sd => sd.Shipment.Id);
            
            // Tạo danh sách ShipmentResponseModel
            var result = new List<ShipmentReponseModel>();
            
            foreach (var group in shipmentGroups)
            {
                var firstDetail = group.First();
                var shipment = firstDetail.Shipment;
                
                // Map Shipment sang ShipmentResponseModel
                var shipmentResponse = _mapper.Map<ShipmentReponseModel>(shipment);
                
                // Tạo danh sách ShipmentDetailResponse
                shipmentResponse.ShipmentDetails = group.Select(sd => new ShipmentDetailResponse
                {
                    MedicalId = sd.MedicalId,
                    MedicalName = sd.Medical.Name,
                    TypeMedical = sd.Medical.TypeMedical,
                    MedicalDescription = sd.Medical.Description,
                    Price = sd.Medical.Price,
                    Quantity = sd.Quantity,
                    ExpiredDate = sd.ExpiredDate
                }).ToList();
                
                result.Add(shipmentResponse);
            }
            
            return result;
        }

        private async Task ValidateRequestAsync(MedicalRequestModel request)
        {
            var validator = new MedicalValidator();
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
            }
        }

        private string GetCurrentUserName()
        {
            return _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        }
    }
}