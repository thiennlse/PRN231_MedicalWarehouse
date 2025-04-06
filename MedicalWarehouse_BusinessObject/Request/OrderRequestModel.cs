using MedicalWarehouse_BusinessObject.Enums;
using System.Text.Json.Serialization;

namespace MedicalWarehouse_BusinessObject.Request
{
    public class OrderRequestModel
    {
        public OrderType Type { get; set; }
        public OrderStatus Status { get; set; }
        [JsonPropertyName("orderDetails")]
        public List<OrderDetailRequestModel> OrderDetail { get; set; } = new List<OrderDetailRequestModel>();

    }
}

