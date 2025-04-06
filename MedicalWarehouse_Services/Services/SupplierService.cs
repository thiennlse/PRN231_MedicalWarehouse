using AutoMapper;
using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Repository.Interface;
using MedicalWarehouse_Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace MedicalWarehouse_Services.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _supplierRepository;
        private readonly IShipmentRepository _shipmentRepository;
        private readonly IShipmentDetailRepository _shipmentDetailRepository;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IMapper _mapper;

        public SupplierService(ISupplierRepository supplierRepository, IShipmentRepository shipmentRepository, IShipmentDetailRepository shipmentDetailRepository, IHttpContextAccessor httpContextAccessor, IMapper mapper)
        {
            _supplierRepository = supplierRepository;
            _shipmentRepository = shipmentRepository;
            _shipmentDetailRepository = shipmentDetailRepository;
            _httpContext = httpContextAccessor;
            _mapper = mapper;
        }

        public async Task<BaseResponse<SupplierResponse>> GetAllSuppliersAsync()
        {
            try
            {
                var suppliers = await _supplierRepository.GetAllAsync();
                var result = _mapper.Map<List<SupplierResponse>>(suppliers);

                return new BaseResponse<SupplierResponse>
                {
                    Success = true,
                    Message = "Lấy danh sách nhà cung cấp thành công",
                    Results = result
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<SupplierResponse>
                {
                    Success = false,
                    Message = $"Lỗi khi lấy danh sách nhà cung cấp: {ex.Message}"
                };
            }
        }

        public async Task<BaseResponse<SupplierResponse>> GetSupplierByIdAsync(Guid id)
        {
            try
            {
                var supplier = await _supplierRepository.GetByIdAsync(id);
                if (supplier == null)
                {
                    return new BaseResponse<SupplierResponse>
                    {
                        Success = false,
                        Message = "Không tìm thấy nhà cung cấp"
                    };
                }

                return new BaseResponse<SupplierResponse>
                {
                    Success = true,
                    Result = _mapper.Map<SupplierResponse>(supplier),
                    Message = "Lấy thông tin nhà cung cấp thành công"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<SupplierResponse>
                {
                    Success = false,
                    Message = $"Lỗi khi lấy thông tin nhà cung cấp: {ex.Message}"
                };
            }
        }

        public async Task<BaseResponse<SupplierResponse>> CreateSupplierAsync(SupplierRequestModel request)
        {
            try
            {
                var currentUserName = _httpContext.HttpContext.User.FindFirst("name")?.Value;
                if (await _supplierRepository.CheckPhoneNumberExistAsync(request.PhoneNumber))
                {
                    return new BaseResponse<SupplierResponse>
                    {
                        Success = false,
                        Message = "Số điện thoại đã tồn tại"
                    };
                }

                if (await _supplierRepository.CheckEmailExistAsync(request.ContactEmail))
                {
                    return new BaseResponse<SupplierResponse>
                    {
                        Success = false,
                        Message = "Email đã tồn tại"
                    };
                }
                var supplier = new Supplier
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    PhoneNumber = request.PhoneNumber,
                    ContactEmail = request.ContactEmail,
                    CreateBy = currentUserName,
                    CreatedDate = DateTime.UtcNow.ToUniversalTime(),
                    IsDeleted = false
                };

                await _supplierRepository.AddAsync(supplier);

                return new BaseResponse<SupplierResponse>
                {
                    Success = true,
                    Result = _mapper.Map<SupplierResponse>(supplier),
                    Message = "Tạo nhà cung cấp thành công"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<SupplierResponse>
                {
                    Success = false,
                    Message = $"Lỗi khi tạo nhà cung cấp: {ex.Message}"
                };
            }
        }

        public async Task<BaseResponse<SupplierResponse>> UpdateSupplierAsync(SupplierRequestModel supplier, Guid supplierId)
        {
            try
            {
                var currentUserName = _httpContext.HttpContext.User.FindFirst("name")?.Value;
                var existingSupplier = await _supplierRepository.GetByIdAsync(supplierId);
                if (existingSupplier == null)
                {
                    return new BaseResponse<SupplierResponse>
                    {
                        Success = false,
                        Message = "Không tìm thấy nhà cung cấp"
                    };
                }

                if (existingSupplier.PhoneNumber != supplier.PhoneNumber &&
                    await _supplierRepository.CheckPhoneNumberExistAsync(supplier.PhoneNumber))
                {
                    return new BaseResponse<SupplierResponse>
                    {
                        Success = false,
                        Message = "Số điện thoại đã tồn tại"
                    };
                }

                if (existingSupplier.ContactEmail != supplier.ContactEmail &&
                    await _supplierRepository.CheckEmailExistAsync(supplier.ContactEmail))
                {
                    return new BaseResponse<SupplierResponse>
                    {
                        Success = false,
                        Message = "Email đã tồn tại"
                    };
                }
                existingSupplier.Name = supplier.Name;
                existingSupplier.ContactEmail = supplier.ContactEmail;
                existingSupplier.PhoneNumber = supplier.PhoneNumber;
                existingSupplier.UpdateBy = currentUserName;
                existingSupplier.UpdatedDate = DateTime.UtcNow.ToUniversalTime();
                await _supplierRepository.UpdateAsync(existingSupplier);
                return new BaseResponse<SupplierResponse>
                {
                    Success = true,
                    Result = _mapper.Map<SupplierResponse>(existingSupplier),
                    Message = "Cập nhật nhà cung cấp thành công"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<SupplierResponse>
                {
                    Success = false,
                    Message = $"Lỗi khi cập nhật nhà cung cấp: {ex.Message}"
                };
            }
        }

        public async Task<BaseResponse<SupplierResponse>> DeleteSupplierAsync(Guid id)
        {
            try
            {
                var currentUserName = _httpContext.HttpContext.User.FindFirst("name")?.Value;
                var result = await _supplierRepository.GetByIdAsync(id);
                if (result == null)
                {
                    return new BaseResponse<SupplierResponse>
                    {
                        Success = false,
                        Message = "Không tìm thấy nhà cung cấp"
                    };
                }
                result.UpdateBy = currentUserName;
                result.UpdatedDate = DateTime.UtcNow.ToUniversalTime();
                result.IsDeleted = true;
                await _supplierRepository.UpdateAsync(result);

                return new BaseResponse<SupplierResponse>
                {
                    Success = true,
                    Message = "Xóa nhà cung cấp thành công"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<SupplierResponse>
                {
                    Success = false,
                    Message = $"Lỗi khi xóa nhà cung cấp: {ex.Message}"
                };
            }
        }

        public async Task<List<ShipmentReponseModel>> GetShipmentBySupplierIdAsync(Guid id)
        {
            try
            {
                var result = new List<ShipmentReponseModel>();
                var query = await _shipmentRepository.GetShipmentBySupplierID(id);
                if (query != null)
                {
                    result = _mapper.Map<List<ShipmentReponseModel>>(query);
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách lô hàng: {ex.Message}");
            }
        }

        private string GetUserName()
        {
            var token = _httpContext?.HttpContext?.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "name");
            return nameClaim?.Value;
        }
    }
}