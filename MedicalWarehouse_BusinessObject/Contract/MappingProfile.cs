using AutoMapper;
using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using System.Linq;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Area Mapping
        CreateMap<Area, AreaResponseModel>()
            .ForMember(dest => dest.AreaId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.AreaName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Shipment, opt => opt.MapFrom(src => src.Shipment ?? new List<Shipment>()));

        CreateMap<AreaRequestModel, Area>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.AreaName));

        // User Mapping
        CreateMap<User, UserResponseModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.UserName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role));

        // Medical Mapping
        CreateMap<Medical, MedicalResponseModel>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Unit, opt => opt.MapFrom(src => src.Unit))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
            .ForMember(dest => dest.TypeMedical, opt => opt.MapFrom(src => src.TypeMedical))
            .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src => src.Supplier != null ? src.Supplier.Name : string.Empty))
            .ForMember(dest => dest.SupplierId, opt => opt.MapFrom(src => src.SupplierId));

        CreateMap<MedicalRequestModel, Medical>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        // Shipment Mapping
        CreateMap<ShipmentDetail, ShipmentReponseModel>();

        CreateMap<Shipment, ShipmentReponseModel>()
            .ForMember(dest => dest.ShipmentId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.SupplierId, opt => opt.MapFrom(src => src.SupplierId))
            .ForMember(dest => dest.AreaId, opt => opt.MapFrom(src => src.AreaId))
            .ForMember(dest => dest.ShipmentDetails, opt => opt.MapFrom(src => src.ShipmentDetails)) // Không cần khởi tạo danh sách mới ở đây
            .ForMember(dest => dest.ShipDate, opt => opt.MapFrom(src => src.ShipDate));

        CreateMap<ShipmentRequestModel, Shipment>()
            .ForMember(dest => dest.SupplierId, opt => opt.MapFrom(src => src.SupplierId))
            .ForMember(dest => dest.AreaId, opt => opt.MapFrom(src => src.AreaId))
            .ForMember(dest => dest.ShipDate, opt => opt.MapFrom(src => src.ShipDate))
            .ForMember(dest => dest.ShipmentDetails, opt => opt.MapFrom(src => src.ShipmentDetails ?? new List<ShipmentDetailRequestModel>()));

        // ShipmentDetail Mapping
        CreateMap<ShipmentDetail, ShipmentDetailResponse>()
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
            .ForMember(dest => dest.ExpiredDate, opt => opt.MapFrom(src => src.ExpiredDate))
            .ForMember(dest => dest.MedicalName, opt => opt.MapFrom(src => src.Medical != null ? src.Medical.Name : string.Empty))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Medical != null ? src.Medical.Price : 0));

        // OrderDetail Mapping 
        CreateMap<OrderDetail, OrderDetailsResponseModel>()
            .ForMember(dest => dest.OrderQuantity, opt => opt.MapFrom(src => src.Quantity))
            .ForMember(dest => dest.TotalCost, opt => opt.MapFrom(src => src.TotalCost))
            .ForMember(dest => dest.MedicalId, opt => opt.MapFrom(src => src.MedicalId))
            .ForMember(dest => dest.ExpiredDate, opt => opt.MapFrom(src =>
                src.Shipments != null && src.Shipments.Any() ?
                src.Shipments.SelectMany(s => s.ShipmentDetails)
                    .Where(sd => sd.MedicalId == src.MedicalId)
                    .OrderBy(sd => sd.ExpiredDate)
                    .Select(sd => sd.ExpiredDate)
                    .FirstOrDefault() :
                DateTime.Now.AddMonths(6) // Nếu không có shipment, mặc định 6 tháng từ hiện tại
            ));

        // Order Mapping
        CreateMap<Order, OrderResponseModel>()
            .ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(src => src.OrderDate))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.OrderDetails, opt => opt.MapFrom(src => src.OrderDetails));

        CreateMap<OrderRequestModel, Order>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.OrderDetails, opt => opt.Ignore()); // Handle manually

        // Supplier Mapping
        CreateMap<Supplier, SupplierResponse>()
            .ForMember(dest => dest.Shipment, opt => opt.MapFrom(src => src.Shipments))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));

        CreateMap<SupplierRequestModel, Supplier>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.ContactEmail, opt => opt.MapFrom(src => src.ContactEmail));
    }
}