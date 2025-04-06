using AutoMapper;
using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Enums;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Repository.Interface;
using MedicalWarehouse_Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MedicalWarehouse_Services.Services
{
    public class ShipmentService : IShipmentService
    {
        private readonly IShipmentRepository _repository;
        private readonly IMedicalRepository _medicalRepo;
        private readonly IOrderRepository _orderRepository;
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IAreaRepository _areaRepository;

        public ShipmentService(
            IShipmentRepository repository, 
            IAreaRepository areaRepository, 
            IMapper mapper, 
            UserManager<User> userManager, 
            IHttpContextAccessor contextAccessor, 
            IMedicalRepository medicalRepo,
            IOrderRepository orderRepository)
        {
            _repository = repository;
            _mapper = mapper;
            _userManager = userManager;
            _contextAccessor = contextAccessor;
            _medicalRepo = medicalRepo;
            _areaRepository = areaRepository;
            _orderRepository = orderRepository;
        }

        public async Task<ShipmentReponseModel> Add(ShipmentRequestModel model)
        {
            var currentUserName = _contextAccessor.HttpContext.User.FindFirst("name")?.Value;
            var area = await _areaRepository.GetAreaById(model.AreaId);

            if (area == null)
            {
                throw new Exception($"Khu vực với ID {model.AreaId} không tồn tại");
            }

            var shipmentDetails = new List<ShipmentDetail>();
            var invalidMedicalIds = new List<Guid>();
            var supplierIds = new HashSet<Guid>();

            foreach (var detail in model.ShipmentDetails)
            {
                var medical = await _medicalRepo.GetMedicalById(detail.MedicalId);
                if (medical == null)
                {
                    invalidMedicalIds.Add(detail.MedicalId);
                    continue;
                }

                // Collect supplier ID from each medical
                if (medical.SupplierId != Guid.Empty)
                {
                    supplierIds.Add(medical.SupplierId);
                }

                shipmentDetails.Add(new ShipmentDetail
                {
                    CreateBy = currentUserName,
                    CreatedDate = DateTime.UtcNow,
                    Name = GeneratedUniqueCode(),
                    MedicalId = detail.MedicalId,
                    Medical = medical,
                    ExpiredDate = detail.ExpiredDate.ToUniversalTime(),
                    Quantity = detail.Quantity,
                    IsDeleted = false
                });
            }

            if (!shipmentDetails.Any())
            {
                throw new Exception("Không tìm thấy ID sản phẩm y tế hợp lệ. Không thể tạo lô hàng.");
            }

            // Auto-determine supplier if not specified but can be derived from the medicines
            Guid supplierId = model.SupplierId;
            if (supplierId == Guid.Empty && supplierIds.Count == 1)
            {
                // If all medicines have the same supplier, use that
                supplierId = supplierIds.First();
            }
            else if (model.SupplierId == Guid.Empty && supplierIds.Count > 1)
            {
                // If medicines have different suppliers, this is ambiguous
                throw new Exception("Không thể tự động xác định nhà cung cấp vì các thuốc đến từ các nhà cung cấp khác nhau. Vui lòng chỉ định nhà cung cấp.");
            }
            else if (model.SupplierId == Guid.Empty)
            {
                throw new Exception("Không thể xác định nhà cung cấp. Vui lòng chỉ định nhà cung cấp.");
            }

            var shipment = new Shipment
            {
                CreateBy = currentUserName,
                CreatedDate = DateTime.UtcNow,
                Name = GeneratedUniqueCode(),
                AreaId = model.AreaId,
                Area = area,
                IsDeleted = false,
                SupplierId = supplierId,
                ShipDate = model.ShipDate.ToUniversalTime(),
                ShipmentDetails = shipmentDetails
            };

            if (invalidMedicalIds.Any())
            {
                throw new Exception($"Các ID sản phẩm y tế sau không tồn tại: {string.Join(", ", invalidMedicalIds)}");
            }

            await _repository.AddAsync(shipment);

            // Find and update related order status
            if (model.OrderId.HasValue)
            {
                var order = await _orderRepository.GetById(model.OrderId.Value);
                if (order != null)
                {
                    order.Status = OrderStatus.ACCEPTED;
                    order.UpdateBy = currentUserName;
                    order.UpdatedDate = DateTime.UtcNow.ToUniversalTime();
                    
                    // Update order
                    await _orderRepository.UpdateAsync(order);
                }
            }

            return _mapper.Map<ShipmentReponseModel>(shipment);
        }

        public async Task DeleteById(Guid shipmentId)
        {
            var currentUserName = _contextAccessor.HttpContext.User.FindFirst("name")?.Value;
            var shipment = await _repository.GetShipmentById(shipmentId);
            if (shipment == null)
            {
                throw new Exception("Không tìm thấy lô hàng!");
            }

            shipment.UpdateBy = currentUserName;
            shipment.UpdatedDate = DateTime.UtcNow.ToUniversalTime();

            await _repository.UpdateAsync(shipment);
            await _repository.DeleteAsync(shipmentId);
        }

        public async Task<List<ShipmentReponseModel>> GetAll()
        {
            var shipments = await _repository.GetAll();
            return shipments;
        }

        public async Task<ShipmentReponseModel> GetById(Guid id)
        {
            var shipment = await _repository.GetShipmentById(id);
            return _mapper.Map<ShipmentReponseModel>(shipment);
        }

        public async Task<ShipmentReponseModel> Update(Guid shipmentId, ShipmentRequestModel model)
        {
            var currentUserName = _contextAccessor.HttpContext.User.FindFirst("name")?.Value;
            var area = await _areaRepository.GetAreaById(model.AreaId);

            if (area == null)
            {
                throw new Exception($"Khu vực với ID {model.AreaId} không tồn tại");
            }

            var shipment = await _repository.GetShipmentById(shipmentId);

            if (shipment == null)
            {
                throw new Exception("Không tìm thấy lô hàng!");
            }

            var invalidMedicalIds = new List<Guid>();
            var medicalDict = new Dictionary<Guid, Medical>();

            foreach (var detail in model.ShipmentDetails)
            {
                var medical = await _medicalRepo.GetMedicalById(detail.MedicalId);
                if (medical == null)
                {
                    invalidMedicalIds.Add(detail.MedicalId);
                }
                else
                {
                    medicalDict[detail.MedicalId] = medical;
                }
            }

            if (invalidMedicalIds.Any())
            {
                throw new Exception($"Các ID sản phẩm y tế sau không tồn tại: {string.Join(", ", invalidMedicalIds)}");
            }

            shipment.UpdateBy = currentUserName;
            shipment.UpdatedDate = DateTime.UtcNow.ToUniversalTime();
            shipment.AreaId = model.AreaId;
            shipment.Area = area;
            shipment.SupplierId = model.SupplierId;
            shipment.ShipDate = model.ShipDate.ToUniversalTime();

            // Remove all existing ShipmentDetails
            shipment.ShipmentDetails.Clear();

            // Add new ShipmentDetails
            if (model.ShipmentDetails != null && model.ShipmentDetails.Any())
            {
                foreach (var detail in model.ShipmentDetails)
                {
                    shipment.ShipmentDetails.Add(new ShipmentDetail
                    {
                        ShipmentId = shipment.Id,
                        MedicalId = detail.MedicalId,
                        Medical = medicalDict[detail.MedicalId],
                        Quantity = detail.Quantity,
                        ExpiredDate = detail.ExpiredDate.ToUniversalTime(),
                        CreateBy = currentUserName,
                        CreatedDate = DateTime.UtcNow.ToUniversalTime(),
                        UpdateBy = currentUserName,
                        UpdatedDate = DateTime.UtcNow.ToUniversalTime()
                    });
                }
            }

            await _repository.UpdateAsync(shipment);
            return _mapper.Map<ShipmentReponseModel>(shipment);
        }

        private string GeneratedUniqueCode() => Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();
    }
}