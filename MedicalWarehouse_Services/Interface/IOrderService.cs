using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_Services.Interface
{
    public interface IOrderService
    {
        Task<List<OrderResponseModel>> GetAllOrder();
        Task<OrderResponseModel> GetOrderByIdAsync(Guid Id);
        Task<OrderResponseModel> CreateOrderAsync(OrderRequestModel request);
        Task<OrderResponseModel> UpdateOrderAsync(OrderRequestModel request, Guid Id);
        Task DeleteOrderAsync(Guid Id);
        Task<string> CreatePaymentUrlAsync(CheckOutRequestModel paymentRequest);
    }
}
