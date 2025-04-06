using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Repository.Interface;
using MedicalWarehouse_Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Net.payOS.Types;
using MedicalWarehouse_BusinessObject.Enums;

namespace MedicalWarehouse_Services.Services
{
    public class OrderService : IOrderService
    {
        private readonly IShipmentRepository _shipmentRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IMedicalRepository _medicalRepo;
        private readonly IMapper _mapper;
        private readonly PayOsService _payOsService;

        public OrderService(IOrderRepository repo, IHttpContextAccessor contextAccessor, IShipmentRepository shipmentRepo, IMedicalRepository medicalRepository, IMapper mapper, PayOsService payOsService)
        {
            _shipmentRepo = shipmentRepo;
            _orderRepo = repo;
            _contextAccessor = contextAccessor;
            _medicalRepo = medicalRepository;
            _mapper = mapper;
            _payOsService = payOsService;
        }

        public async Task<OrderResponseModel> UpdateOrderAsync(OrderRequestModel request, Guid orderId)
        {
            var currentUser = _contextAccessor.HttpContext?.User;
            var currentUserName = currentUser?.FindFirst("name")?.Value ?? "Không xác định";

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "Yêu cầu không được để trống");
            }

            var order = await _orderRepo.GetById(orderId);
            if (order == null)
            {
                throw new Exception("Không tìm thấy đơn hàng");
            }

            var invalidMedicalIds = new List<Guid>();
            var updatedOrderDetails = new List<OrderDetail>();

            foreach (var detail in request.OrderDetail)
            {
                if (!await _medicalRepo.ExistAsync(detail.MedicalId))
                {
                    invalidMedicalIds.Add(detail.MedicalId);
                }
            }

            if (invalidMedicalIds.Any())
            {
                throw new Exception($"Các ID sản phẩm y tế sau không tồn tại: {string.Join(", ", invalidMedicalIds)}");
            }

            foreach (var detail in request.OrderDetail)
            {
                var shipmentDetails = await _shipmentRepo.GetShipmentDetailsByMedicalId(detail.MedicalId);
                if (shipmentDetails == null || !shipmentDetails.Any())
                {
                    invalidMedicalIds.Add(detail.MedicalId);
                    continue;
                }

                var medical = await _medicalRepo.GetMedicalById(detail.MedicalId);
                if (medical == null) continue;

                double totalCost = 0;
                var usedShipments = new List<Shipment>();
                var allocatedQuantity = detail.Quantity;

                if (request.Type != OrderType.Import)
                {
                    foreach (var shipmentDetail in shipmentDetails.OrderBy(sd => sd.ExpiredDate))
                    {
                        if (allocatedQuantity <= 0) break;

                        var usedQuantity = Math.Min(shipmentDetail.Quantity, allocatedQuantity);
                        allocatedQuantity -= usedQuantity;
                        shipmentDetail.Quantity -= usedQuantity;

                        if (!usedShipments.Contains(shipmentDetail.Shipment))
                        {
                            usedShipments.Add(shipmentDetail.Shipment);
                        }

                        totalCost += medical.Price * usedQuantity;
                    }

                    if (allocatedQuantity > 0)
                    {
                        throw new Exception($"Không đủ hàng trong kho cho sản phẩm y tế với ID {detail.MedicalId}.");
                    }
                }
                else
                {
                    totalCost = medical.Price * detail.Quantity;
                    usedShipments = shipmentDetails.Any() ? new List<Shipment> { shipmentDetails.First().Shipment } : new List<Shipment>();
                }

                var existingOrderDetail = order.OrderDetails
                    .FirstOrDefault(od => od.Shipments.SelectMany(s => s.ShipmentDetails)
                    .Any(sd => sd.MedicalId == detail.MedicalId));

                if (existingOrderDetail != null)
                {
                    existingOrderDetail.Quantity = detail.Quantity;
                    existingOrderDetail.TotalCost = totalCost;
                    existingOrderDetail.UpdateBy = currentUserName;
                    existingOrderDetail.UpdatedDate = DateTime.UtcNow;

                    foreach (var shipment in usedShipments)
                    {
                        if (!existingOrderDetail.Shipments.Contains(shipment))
                        {
                            existingOrderDetail.Shipments.Add(shipment);
                        }
                    }
                }
                else
                {
                    var newOrderDetail = new OrderDetail
                    {
                        Id = Guid.NewGuid(),
                        CreateBy = currentUserName,
                        CreatedDate = DateTime.UtcNow,
                        Quantity = detail.Quantity,
                        TotalCost = totalCost,
                        Shipments = usedShipments
                    };

                    order.OrderDetails.Add(newOrderDetail);
                    updatedOrderDetails.Add(newOrderDetail);
                }
            }

            if (request.Type != OrderType.Import && invalidMedicalIds.Any())
            {
                throw new Exception($"Các ID sản phẩm y tế sau đã hết hàng: {string.Join(", ", invalidMedicalIds)}");
            }

            order.UpdateBy = currentUserName;
            order.UpdatedDate = DateTime.UtcNow;
            order.Type = request.Type;
            order.Status = request.Status;

            await _orderRepo.UpdateAsync(order);

            return _mapper.Map<OrderResponseModel>(order);
        }


        public async Task<OrderResponseModel> CreateOrderAsync(OrderRequestModel request)
        {
            // Lấy thông tin người dùng hiện tại
            var currentUser = _contextAccessor.HttpContext?.User;
            var currentUserName = currentUser?.FindFirst("name")?.Value ?? "Không xác định";
            var currUserId = currentUser?.FindFirst("uid")?.Value ?? "Không xác định";

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "Yêu cầu không được để trống");
            }

            // Kiểm tra tất cả Medical IDs có tồn tại không
            var invalidMedicalIds = new List<Guid>();
            foreach (var detail in request.OrderDetail)
            {
                if (!await _medicalRepo.ExistAsync(detail.MedicalId))
                {
                    invalidMedicalIds.Add(detail.MedicalId);
                }
            }

            if (invalidMedicalIds.Any())
            {
                throw new Exception($"Các ID sản phẩm y tế sau không tồn tại: {string.Join(", ", invalidMedicalIds)}");
            }

            // Tạo đơn hàng mới
            var newOrder = new Order
            {
                Id = Guid.NewGuid(),
                Name = GenerateUniqueCode(),
                UserId = currUserId,
                Type = request.Type,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.ORDERED,
                CreateBy = currentUserName,
                CreatedDate = DateTime.UtcNow,
                OrderDetails = new List<OrderDetail>()
            };

            // Xử lý chi tiết đơn hàng
            foreach (var detailRequest in request.OrderDetail)
            {
                var medical = await _medicalRepo.GetMedicalById(detailRequest.MedicalId);
                if (medical == null)
                {
                    throw new Exception($"Không tìm thấy sản phẩm y tế với ID {detailRequest.MedicalId}");
                }

                var shipmentDetails = await _shipmentRepo.GetShipmentDetailsByMedicalId(detailRequest.MedicalId);
                if ((shipmentDetails == null || !shipmentDetails.Any()) && request.Type != OrderType.Import)
                {
                    throw new Exception($"Không có lô hàng nào cho sản phẩm y tế với ID {detailRequest.MedicalId}");
                }

                var newOrderDetail = new OrderDetail
                {
                    Id = Guid.NewGuid(),
                    Name = GenerateUniqueCode(),
                    CreateBy = currentUserName,
                    CreatedDate = DateTime.UtcNow,
                    OrderId = newOrder.Id,
                    Orders = newOrder,
                    MedicalId = detailRequest.MedicalId,
                    Quantity = detailRequest.Quantity,
                    TotalCost = 0, // Sẽ được cập nhật sau
                    Shipments = new List<Shipment>()
                };

                // Xử lý phân bổ số lượng từ các lô hàng - chỉ khi không phải là đơn nhập hàng
                if (request.Type != OrderType.Import)
                {
                    var allocatedQuantity = detailRequest.Quantity;
                    double totalCost = 0;

                    // Sắp xếp shipmentDetails theo ngày hết hạn (FIFO)
                    foreach (var shipmentDetail in shipmentDetails.OrderBy(sd => sd.ExpiredDate))
                    {
                        if (allocatedQuantity <= 0) break;

                        var usedQuantity = Math.Min(shipmentDetail.Quantity, allocatedQuantity);
                        allocatedQuantity -= usedQuantity;

                        // Cập nhật số lượng trong kho
                        shipmentDetail.Quantity -= usedQuantity;

                        // Thêm shipment vào danh sách shipments của orderDetail
                        if (!newOrderDetail.Shipments.Contains(shipmentDetail.Shipment))
                        {
                            newOrderDetail.Shipments.Add(shipmentDetail.Shipment);
                        }

                        // Tính tổng chi phí
                        totalCost += medical.Price * usedQuantity;
                    }

                    if (allocatedQuantity > 0)
                    {
                        throw new Exception($"Không đủ hàng trong kho cho sản phẩm y tế với ID {detailRequest.MedicalId}");
                    }

                    newOrderDetail.TotalCost = totalCost;
                }
                else
                {
                    // Đối với đơn nhập hàng, tính toán chi phí dựa trên giá và số lượng
                    newOrderDetail.TotalCost = medical.Price * detailRequest.Quantity;
                }

                newOrder.OrderDetails.Add(newOrderDetail);
            }
            await _orderRepo.AddAsync(newOrder);

            return _mapper.Map<OrderResponseModel>(newOrder);
        }

        public async Task<OrderResponseModel> GetOrderByIdAsync(Guid orderId)
        {
            var order = await _orderRepo.GetById(orderId);
            if (order == null) return null;
            return _mapper.Map<OrderResponseModel>(order);
        }

        public async Task DeleteOrderAsync(Guid orderId)
        {
            var currentUserName = _contextAccessor.HttpContext.User.FindFirst("name")?.Value;

            var order = await _orderRepo.GetById(orderId);
            if (order == null)
            {
                throw new Exception("Không tìm thấy đơn hàng");
            }
            order.UpdateBy = currentUserName;
            order.UpdatedDate = DateTime.UtcNow.ToUniversalTime();
            order.IsDeleted = true;
            await _orderRepo.UpdateAsync(order);
        }

        public async Task<List<OrderResponseModel>> GetAllOrder()
        {
            return await _orderRepo.GetAll();
        }

        private string GenerateUniqueCode() => Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();

        public async Task<string> CreatePaymentUrlAsync(CheckOutRequestModel paymentRequest)
        {
            try
            {
                List<ItemData> items = new List<ItemData>();

                // Tránh tràn số khi tạo orderCode
                int orderCode = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % int.MaxValue);

                long totalAmount = 0; // Dùng long thay vì int

                foreach (var request in paymentRequest.OrderRequest)
                {
                    var medical = await _medicalRepo.GetMedicalById(request.MedicalId);

                    if (medical == null)
                    {
                        throw new Exception($"Product not found for MedicalId {request.MedicalId}");
                    }

                    // Lấy danh sách lô hàng từ ShipmentDetails
                    var shipmentDetails = await _shipmentRepo.GetShipmentDetailsByMedicalId(request.MedicalId);
                    if (shipmentDetails == null || !shipmentDetails.Any())
                    {
                        throw new Exception($"No shipment available for Medical ID {request.MedicalId}");
                    }

                    int allocatedQuantity = request.Quantity; // Số lượng cần đặt hàng
                    int availableStock = shipmentDetails.Sum(s => s.Quantity); // Tổng tồn kho

                    if (allocatedQuantity > availableStock)
                    {
                        throw new Exception($"Not enough stock for Medical ID {request.MedicalId}. Available: {availableStock}");
                    }

                    long totalItemCost = (long)(medical.Price * allocatedQuantity);
                    totalAmount += totalItemCost;

                    items.Add(new ItemData(medical.Name, allocatedQuantity, (int)totalItemCost));
                }

                if (totalAmount > int.MaxValue)
                {
                    throw new Exception("Total amount exceeds the maximum allowed value.");
                }

                var paymentData = new PaymentData(
                    orderCode,
                    (int)totalAmount,
                    "payment order",
                    items,
                    paymentRequest.CancelUrl,
                    paymentRequest.ReturnUrl
                );

                CreatePaymentResult paymentResult = await _payOsService.createPaymentLink(paymentData);
                return paymentResult.checkoutUrl;
            }
            catch (Exception ex)
            {
                throw new Exception($"Payment creation failed: {ex.Message}");
            }
        }


    }
}