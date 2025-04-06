using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Enums;
using MedicalWarehouse_BusinessObject.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_BusinessObject.Response
{
    public class OrderResponseModel
    {
        public Guid OrderId { get; set; }
        public string Name { get; set; }
        public string UserId { get; set; }
        public OrderType Type { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public ICollection<OrderDetailsResponseModel> OrderDetails { get; set; } = new List<OrderDetailsResponseModel>();
        


    }
}
