using MedicalWarehouse_BusinessObject.Enums;

namespace MedicalWarehouse_BusinessObject.Entity
{
    public class Order : BaseEntity
    {
        public string UserId { get; set; }
        public User User { get; set; }
        public OrderType Type { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
       
    }
}
